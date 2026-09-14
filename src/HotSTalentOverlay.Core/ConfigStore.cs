using System.Text.Json;

namespace HotSTalentOverlay.Core;

public sealed class ConfigStore
{
    private readonly AppPaths _paths;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppConfig _current;

    public ConfigStore(AppPaths paths)
    {
        _paths = paths;
        _current = LoadFromDisk();
    }

    public AppConfig Current => _current;

    public async Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        if (config.ObsPort is < 1024 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(config), "El puerto debe estar entre 1024 y 65535.");
        if (!string.IsNullOrWhiteSpace(config.BattleTag) && !config.BattleTag.Contains('#'))
            throw new ArgumentException("BattleTag debe tener el formato Nombre#1234.", nameof(config));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            string temporary = _paths.ConfigFile + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(config, JsonOptions.Indented), cancellationToken);
            File.Move(temporary, _paths.ConfigFile, true);
            _current = config;
        }
        finally
        {
            _gate.Release();
        }
    }

    private AppConfig LoadFromDisk()
    {
        try
        {
            return File.Exists(_paths.ConfigFile)
                ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_paths.ConfigFile), JsonOptions.Default) ?? new AppConfig()
                : new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
    public static readonly JsonSerializerOptions Indented = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}

