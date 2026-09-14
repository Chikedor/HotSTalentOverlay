using System.Threading.Channels;
using HotSTalentOverlay.Core;

namespace HotSTalentOverlay.App;

public sealed class CatalogRefreshService : BackgroundService
{
    private readonly Channel<bool> _requests = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });
    private readonly GameInstallationDetector _detector;
    private readonly CatalogBuilder _builder;
    private readonly TalentCatalog _catalog;
    private readonly ConfigStore _config;
    private readonly RuntimeState _state;
    private readonly AppPaths _paths;
    private readonly ILogger<CatalogRefreshService> _logger;

    public CatalogRefreshService(GameInstallationDetector detector, CatalogBuilder builder, TalentCatalog catalog,
        ConfigStore config, RuntimeState state, AppPaths paths, ILogger<CatalogRefreshService> logger)
    {
        _detector = detector; _builder = builder; _catalog = catalog; _config = config; _state = state; _paths = paths; _logger = logger;
    }

    public bool Request(bool force = true) => _requests.Writer.TryWrite(force);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync(stoppingToken);
        await foreach (bool force in _requests.Reader.ReadAllAsync(stoppingToken))
            await RefreshAsync(force, stoppingToken);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        DetectedGame? game = _detector.Detect(_config.Current.HotsPath);
        _state.Update(s => s with
        {
            HotsDetected = game is not null,
            HotsPath = game?.Path ?? _config.Current.HotsPath,
            GameVersion = game?.Version ?? string.Empty,
            GameBuild = game?.Build ?? 0,
            BattleTag = _config.Current.BattleTag,
            ReplayPath = EffectiveReplayPath(_config.Current),
        });

        try
        {
            if (File.Exists(_paths.CatalogFile))
            {
                _catalog.Load(_paths.CatalogFile);
                CatalogDocument document = _catalog.Document!;
                _state.RefreshCatalog();
                SetReady(document);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "El catálogo guardado no es válido; se regenerará");
        }

        if (game is null)
        {
            _state.Update(s => s with { CatalogStatus = "HotS no detectado", LastError = "Selecciona la carpeta de Heroes of the Storm." });
            return;
        }
        await RefreshAsync(_catalog.Document?.Build != game.Build, cancellationToken);
    }

    private async Task RefreshAsync(bool force, CancellationToken cancellationToken)
    {
        DetectedGame? game = _detector.Detect(_config.Current.HotsPath);
        _state.Update(s => s with
        {
            HotsDetected = game is not null, HotsPath = game?.Path ?? _config.Current.HotsPath,
            GameVersion = game?.Version ?? string.Empty, GameBuild = game?.Build ?? 0,
        });
        if (game is null)
        {
            _state.Update(s => s with { CatalogStatus = "HotS no detectado", LastError = "Selecciona la carpeta de Heroes of the Storm." });
            return;
        }
        if (!force && _catalog.Document?.Build == game.Build) return;

        _state.Update(s => s with { CatalogStatus = "Extrayendo desde CASC…", LastError = string.Empty });
        _logger.LogInformation("Generando catálogo para la build {Build}", game.Build);
        try
        {
            CatalogDocument document = await _builder.GenerateAsync(game, _config.Current.Locale, HelperLocator.Locate(), cancellationToken: cancellationToken);
            _catalog.Set(document);
            _state.RefreshCatalog();
            SetReady(document);
            _logger.LogInformation("Catálogo: {Heroes} héroes / {Talents} talentos", document.HeroCount, document.Talents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo regenerar el catálogo");
            _state.Update(s => s with { CatalogStatus = "Error de extracción", LastError = ex.Message });
        }
    }

    private void SetReady(CatalogDocument document) => _state.Update(s => s with
    {
        CatalogReady = true, CatalogBuild = document.Build, HeroCount = document.HeroCount,
        TalentCount = document.Talents.Count, CatalogStatus = "Listo", LastError = string.Empty,
    });

    public static string EffectiveReplayPath(AppConfig config) => !string.IsNullOrWhiteSpace(config.ReplayPath)
        ? config.ReplayPath
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Heroes of the Storm", "Accounts");
}
