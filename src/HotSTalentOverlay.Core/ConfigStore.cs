using System.Text.Json;
using System.Text.RegularExpressions;

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
        Validate(config);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            string temporary = _paths.ConfigFile + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(config, JsonOptions.Indented), cancellationToken);
            File.Move(temporary, _paths.ConfigFile, true);
            _current = Normalize(config);
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
            AppConfig config = File.Exists(_paths.ConfigFile)
                ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_paths.ConfigFile), JsonOptions.Default) ?? new AppConfig()
                : new AppConfig();
            return Normalize(config);
        }
        catch
        {
            return new AppConfig();
        }
    }

    private static AppConfig Normalize(AppConfig config) => config with
    {
        UiLanguage = config.UiLanguage is "en" or "es" ? config.UiLanguage : "es",
        OverlayStyle = config.OverlayStyle ?? new OverlayStyleConfig(),
    };

    private static void Validate(AppConfig config)
    {
        OverlayStyleConfig style = config.OverlayStyle ?? throw new ArgumentException("Falta la configuración visual.", nameof(config));
        if (config.UiLanguage is not ("en" or "es"))
            throw new ArgumentException("El idioma de interfaz debe ser 'en' o 'es'.", nameof(config));
        foreach (string color in new[] { style.AccentColor, style.BorderColor, style.BackgroundColor, style.TextColor })
            if (string.IsNullOrWhiteSpace(color) || !Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$"))
                throw new ArgumentException("Los colores deben usar el formato #RRGGBB.", nameof(config));
        if (style.BorderWidth is < 0 or > 8 || style.BorderRadius is < 0 or > 32 ||
            style.IconSize is < 40 or > 160 || style.Gap is < 0 or > 40 || style.AnimationSpeed is < 50 or > 200)
            throw new ArgumentOutOfRangeException(nameof(config), "La configuración visual está fuera del rango permitido.");
        if (style.BorderStyle is not ("solid" or "double" or "dashed" or "none") ||
            style.EntryAnimation is not ("none" or "fade" or "slide" or "pop" or "flip"))
            throw new ArgumentException("La animación o el estilo de borde no es válido.", nameof(config));
    }
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
    public static readonly JsonSerializerOptions Indented = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}
