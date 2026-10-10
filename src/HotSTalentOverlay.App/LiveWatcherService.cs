using System.Collections.Concurrent;
using System.Threading.Channels;
using HotSTalentOverlay.Core;

namespace HotSTalentOverlay.App;

public sealed class LiveWatcherService : BackgroundService
{
    private readonly Channel<string> _files = Channel.CreateBounded<string>(new BoundedChannelOptions(32) { FullMode = BoundedChannelFullMode.DropOldest });
    private readonly Channel<bool> _reload = Channel.CreateBounded<bool>(1);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _recent = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConfigStore _config;
    private readonly AppPaths _paths;
    private readonly StormReplaySnapshotReader _reader;
    private readonly RuntimeState _state;
    private readonly ILogger<LiveWatcherService> _logger;
    private FileSystemWatcher? _watcher;

    public LiveWatcherService(ConfigStore config, AppPaths paths, StormReplaySnapshotReader reader, RuntimeState state, ILogger<LiveWatcherService> logger)
    {
        _config = config; _paths = paths; _reader = reader; _state = state; _logger = logger;
    }

    public void RequestReload() => _reload.Writer.TryWrite(true);
    public void ProcessFile(string path) => Queue(path);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartWatcher();
        Task reloadTask = ReloadLoop(stoppingToken);
        try
        {
            await foreach (string path in _files.Reader.ReadAllAsync(stoppingToken))
                await ProcessAsync(path, stoppingToken);
        }
        finally
        {
            _watcher?.Dispose();
            await reloadTask;
        }
    }

    private async Task ReloadLoop(CancellationToken cancellationToken)
    {
        await foreach (bool _ in _reload.Reader.ReadAllAsync(cancellationToken))
            StartWatcher();
    }

    private void StartWatcher()
    {
        _watcher?.Dispose();
        string path = CatalogRefreshService.EffectiveReplayPath(_config.Current);
        if (!Directory.Exists(path))
        {
            _state.Update(s => s with { WatcherRunning = false, ReplayPath = path, LastError = "No existe la carpeta de replays: " + path });
            return;
        }

        // A custom Saves/Rejoin path must also cover its account's sibling Replays folder.
        string watchRoot = path;
        for (DirectoryInfo? directory = new(path); directory is not null; directory = directory.Parent)
            if (System.Text.RegularExpressions.Regex.IsMatch(directory.Name, @"^[1-9][0-9]*-Hero-[0-9]+-[0-9]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            { watchRoot = directory.FullName; break; }
        _watcher = new FileSystemWatcher(watchRoot, "*.Storm*")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            InternalBufferSize = 32 * 1024,
        };
        _watcher.Created += (_, e) => Queue(e.FullPath);
        _watcher.Changed += (_, e) => Queue(e.FullPath);
        _watcher.Renamed += (_, e) => Queue(e.FullPath);
        _watcher.Error += (_, e) => { _logger.LogError(e.GetException(), "El watcher perdió eventos; reiniciando"); RequestReload(); };
        _watcher.EnableRaisingEvents = true;
        _state.Update(s => s with { WatcherRunning = true, ReplayPath = path, LastError = string.Empty });
        _logger.LogInformation("Watcher activo en {Path}", path);

        // Queue final replays first so restarting cannot resurrect a finished StormSave.
        foreach (FileInfo replay in Directory.EnumerateFiles(watchRoot, "*.StormReplay", SearchOption.AllDirectories)
            .Select(x => new FileInfo(x)).Where(x => x.LastWriteTimeUtc > DateTime.UtcNow.AddHours(-12))
            .OrderBy(x => x.LastWriteTimeUtc)) Queue(replay.FullName);
        FileInfo? latest = Directory.EnumerateFiles(watchRoot, "*.StormSave", SearchOption.AllDirectories)
            .Select(x => new FileInfo(x)).Where(x => x.LastWriteTimeUtc > DateTime.UtcNow.AddHours(-12))
            .OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();
        if (latest is not null) Queue(latest.FullName);
    }

    private void Queue(string path)
    {
        string extension = Path.GetExtension(path);
        if (!extension.Equals(".StormSave", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".StormReplay", StringComparison.OrdinalIgnoreCase)) return;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (_recent.TryGetValue(path, out DateTimeOffset previous) && now - previous < TimeSpan.FromMilliseconds(400)) return;
        _recent[path] = now;
        _files.Writer.TryWrite(path);
    }

    private async Task ProcessAsync(string path, CancellationToken cancellationToken)
    {
        string? copy = null;
        try
        {
            if (Path.GetExtension(path).Equals(".StormReplay", StringComparison.OrdinalIgnoreCase))
            {
                // Created can arrive before HotS finishes writing the MPQ archive.
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        copy = await ResilientFileCopy.CreateSnapshotAsync(path, _paths.Temp, cancellationToken);
                        string? matchId = _reader.ReadCompletedMatchId(copy);
                        if (matchId is null) throw new InvalidDataException("La replay aún no contiene resultados finales.");
                        _state.CompleteMatch(matchId);
                        _logger.LogInformation("Replay final detectada; partida {Match} terminada", matchId);
                        return;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && attempt < 4)
                    {
                        _logger.LogDebug(ex, "Esperando a que termine de escribirse la replay: {Path}", path);
                        if (copy is not null) { File.Delete(copy); copy = null; }
                        await Task.Delay(300 * (attempt + 1), cancellationToken);
                    }
                }
            }
            copy = await ResilientFileCopy.CreateSnapshotAsync(path, _paths.Temp, cancellationToken);
            ReplaySnapshot snapshot = _reader.Read(copy, _config.Current.BattleTag, path);
            _state.ApplyReplay(snapshot);
            _logger.LogInformation("Partida {Match}; jugador {Player}; héroe {Hero}; talentos {Count}", snapshot.MatchId, snapshot.Player, snapshot.HeroUnitId, snapshot.TalentIds.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StormSave aún no procesable: {Path}", path);
            _state.Update(s => s with { LastError = ex.Message });
        }
        finally
        {
            try { if (copy is not null) File.Delete(copy); } catch { }
        }
    }
}
