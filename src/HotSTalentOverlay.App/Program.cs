using System.Text.Json;
using HotSTalentOverlay.App;
using HotSTalentOverlay.Core;
using Microsoft.Extensions.FileProviders;

WebApplicationOptions options = new() { Args = args, ContentRootPath = AppContext.BaseDirectory };
WebApplicationBuilder builder = WebApplication.CreateBuilder(options);
string? stateDir = args.SkipWhile(x => x != "--state-dir").Skip(1).FirstOrDefault();
AppPaths paths = new(stateDir);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(x => x.TimestampFormat = "HH:mm:ss ");
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(paths.Logs, "latest.log")));
builder.Services.AddSingleton(paths);
builder.Services.Configure<HostOptions>(x => x.ShutdownTimeout = TimeSpan.FromSeconds(5));
builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<GameInstallationDetector>();
builder.Services.AddSingleton<TalentCatalog>();
builder.Services.AddSingleton<CatalogBuilder>();
builder.Services.AddSingleton<StormReplaySnapshotReader>();
builder.Services.AddSingleton<EventHub>();
builder.Services.AddSingleton<RuntimeState>();
builder.Services.AddHttpClient<UpdateService>(client => client.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddSingleton<CatalogRefreshService>();
builder.Services.AddHostedService(x => x.GetRequiredService<CatalogRefreshService>());
builder.Services.AddSingleton<LiveWatcherService>();
builder.Services.AddHostedService(x => x.GetRequiredService<LiveWatcherService>());

ConfigStore earlyConfig = new(paths);
int port = earlyConfig.Current.ObsPort;
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

WebApplication app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(paths.Root, "assets")),
    RequestPath = "/assets",
});

app.MapGet("/api/status", (RuntimeState state, ConfigStore config) => Results.Ok(state.Snapshot with
{
    BattleTag = config.Current.BattleTag,
    UiLanguage = config.Current.UiLanguage,
    OverlayStyle = config.Current.OverlayStyle,
}));
app.MapGet("/api/config", (ConfigStore config) => Results.Ok(config.Current));
// Read-only simulation: preview controls must never replace the match shown in OBS.
app.MapGet("/api/preview", (TalentCatalog catalog) =>
{
    TalentCatalogEntry[] sample = catalog.Document?.Talents
        .Where(x => x.Hero.Equals("Valla", StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x.Level).GroupBy(x => x.Level).Select(x => x.First()).ToArray() ?? [];
    return Results.Ok(new AppStatus
    {
        MatchId = "preview", HeroName = sample.FirstOrDefault()?.Hero ?? string.Empty,
        CatalogBuild = catalog.Document?.Build ?? 0,
        Talents = sample.Select(x => new DetectedTalent(x.Level, x.TalentTreeId, x.Name, "/" + x.Icon.Replace('\\', '/'), null)).ToArray(),
    });
});
app.MapGet("/api/update", (UpdateService updates, CancellationToken ct) => updates.CheckAsync(false, ct));
app.MapPost("/api/update/install", async (UpdateService updates, CancellationToken ct) =>
{
    try { await updates.InstallAsync(ct); return Results.Accepted(); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});
app.MapPost("/api/config", async (AppConfig config, ConfigStore store, RuntimeState state, LiveWatcherService watcher, CatalogRefreshService refresh, GameInstallationDetector detector, CancellationToken ct) =>
{
    try
    {
        AppConfig previous = store.Current;
        await store.SaveAsync(config, ct);
        DetectedGame? game = detector.Detect(config.HotsPath);
        state.Update(s => s with
        {
            BattleTag = config.BattleTag, UiLanguage = config.UiLanguage, OverlayStyle = config.OverlayStyle,
            ReplayPath = CatalogRefreshService.EffectiveReplayPath(config),
            HotsDetected = game is not null, HotsPath = game?.Path ?? config.HotsPath,
            GameVersion = game?.Version ?? string.Empty, GameBuild = game?.Build ?? 0, LastError = string.Empty,
        });
        watcher.RequestReload();
        if (!string.Equals(previous.HotsPath, config.HotsPath, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(previous.Locale, config.Locale, StringComparison.OrdinalIgnoreCase))
            refresh.Request();
        return Results.Ok(config);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});
app.MapPost("/api/catalog/regenerate", (CatalogRefreshService refresh) => refresh.Request() ? Results.Accepted() : Results.StatusCode(429));
app.MapPost("/api/match/reset", (RuntimeState state) => { state.ResetMatch(); return Results.NoContent(); });
app.MapPost("/api/demo", (RuntimeState state, TalentCatalog catalog) =>
{
    TalentCatalogEntry[] valla = catalog.Document?.Talents.Where(x => x.Hero.Equals("Valla", StringComparison.OrdinalIgnoreCase)).ToArray() ?? [];
    string[] ids = valla.OrderBy(x => x.Level).GroupBy(x => x.Level).Select(x => x.First().TalentTreeId).ToArray();
    if (ids.Length == 0) ids = catalog.Document?.Talents.GroupBy(x => x.Level).OrderBy(x => x.Key).Select(x => x.First().TalentTreeId).Take(7).ToArray() ?? [];
    state.ApplyReplay(new ReplaySnapshot("demo", catalog.Document?.Build ?? 0, "Demo", valla.FirstOrDefault()?.HeroUnitId ?? "HeroDemonHunter", "Valla", ids, ids.Select(_ => (double?)null).ToArray(), "Demo"));
    return Results.NoContent();
});
app.MapPost("/api/replay/test", async (HttpRequest request, LiveWatcherService watcher) =>
{
    using JsonDocument json = await JsonDocument.ParseAsync(request.Body);
    string? path = json.RootElement.TryGetProperty("path", out JsonElement value) ? value.GetString() : null;
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return Results.BadRequest(new { error = "Archivo no encontrado." });
    watcher.ProcessFile(path);
    return Results.Accepted();
});
app.MapGet("/events", async (HttpContext context, RuntimeState state) =>
{
    // Compatibility for tabs opened before 1.4.4: send one snapshot and release
    // the connection. EventSource reconnects after one second instead of holding
    // all six HTTP/1.1 browser connections and starving talent image requests.
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.ContentType = "text/event-stream";
    await context.Response.WriteAsync($"retry: 1000\ndata: {JsonSerializer.Serialize(state.Snapshot, JsonOptions.Default)}\n\n", context.RequestAborted);
});

bool openBrowserOnStart = !args.Contains("--background", StringComparer.OrdinalIgnoreCase) &&
                          !args.Contains("--no-browser", StringComparer.OrdinalIgnoreCase);
app.Lifetime.ApplicationStarted.Register(() => WindowsShell.Start(app.Lifetime, app.Services.GetRequiredService<UpdateService>(), port, openBrowserOnStart));
app.Run();

public partial class Program;
