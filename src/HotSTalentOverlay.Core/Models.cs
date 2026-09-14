namespace HotSTalentOverlay.Core;

public sealed record AppConfig
{
    public string BattleTag { get; init; } = string.Empty;
    public string HotsPath { get; init; } = string.Empty;
    public string ReplayPath { get; init; } = string.Empty;
    public int ObsPort { get; init; } = 3874;
    public string Locale { get; init; } = "esES";
    public string UiLanguage { get; init; } = "es";
    public OverlayStyleConfig OverlayStyle { get; init; } = new();
}

public sealed record OverlayStyleConfig
{
    public string AccentColor { get; init; } = "#b9a8ff";
    public string BorderColor { get; init; } = "#7869b3";
    public string BackgroundColor { get; init; } = "#11101c";
    public string TextColor { get; init; } = "#ffffff";
    public int BorderWidth { get; init; } = 2;
    public int BorderRadius { get; init; } = 10;
    public string BorderStyle { get; init; } = "solid";
    public int IconSize { get; init; } = 84;
    public int Gap { get; init; } = 10;
    public bool ShowHero { get; init; } = true;
    public bool ShowLevels { get; init; } = true;
    public bool ShowTalentNames { get; init; }
    public string EntryAnimation { get; init; } = "slide";
    public string IdleAnimation { get; init; } = "none";
    public int AnimationSpeed { get; init; } = 100;
}

public sealed record TalentCatalogEntry(
    string TalentTreeId,
    string HeroUnitId,
    string Hero,
    int Level,
    string Name,
    string Icon);

public sealed record CatalogDocument(
    string GameVersion,
    int Build,
    string GeneratedAtUtc,
    string Locale,
    int HeroCount,
    IReadOnlyList<TalentCatalogEntry> Talents);

public sealed record DetectedGame(string Path, string Version, int Build);

public sealed record DetectedTalent(
    int Level,
    string TalentTreeId,
    string Name,
    string IconUrl,
    double? TimestampSeconds);

public sealed record ReplaySnapshot(
    string MatchId,
    int Build,
    string Player,
    string HeroUnitId,
    string HeroName,
    IReadOnlyList<string> TalentIds,
    IReadOnlyList<double?> TalentTimes,
    string ParseStatus);

public sealed record AppStatus
{
    public bool HotsDetected { get; init; }
    public string HotsPath { get; init; } = string.Empty;
    public string GameVersion { get; init; } = string.Empty;
    public int GameBuild { get; init; }
    public bool CatalogReady { get; init; }
    public int CatalogBuild { get; init; }
    public int HeroCount { get; init; }
    public int TalentCount { get; init; }
    public string CatalogStatus { get; init; } = "Pendiente";
    public bool WatcherRunning { get; init; }
    public string ReplayPath { get; init; } = string.Empty;
    public string BattleTag { get; init; } = string.Empty;
    public string UiLanguage { get; init; } = "es";
    public OverlayStyleConfig OverlayStyle { get; init; } = new();
    public string MatchId { get; init; } = string.Empty;
    public string Player { get; init; } = string.Empty;
    public string HeroUnitId { get; init; } = string.Empty;
    public string HeroName { get; init; } = string.Empty;
    public IReadOnlyList<DetectedTalent> Talents { get; init; } = [];
    public string LastError { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
