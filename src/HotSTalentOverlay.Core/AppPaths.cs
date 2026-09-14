namespace HotSTalentOverlay.Core;

public sealed class AppPaths
{
    public AppPaths(string? root = null)
    {
        Root = Path.GetFullPath(root ?? Environment.GetEnvironmentVariable("HOTSTALENTOVERLAY_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotSTalentOverlay"));
        Data = Path.Combine(Root, "data");
        TalentAssets = Path.Combine(Root, "assets", "talents");
        Logs = Path.Combine(Root, "logs");
        Temp = Path.Combine(Root, "temp");
        ConfigFile = Path.Combine(Root, "config.json");
        CatalogFile = Path.Combine(Data, "talents.json");

        Directory.CreateDirectory(Data);
        Directory.CreateDirectory(TalentAssets);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Temp);
    }

    public string Root { get; }
    public string Data { get; }
    public string TalentAssets { get; }
    public string Logs { get; }
    public string Temp { get; }
    public string ConfigFile { get; }
    public string CatalogFile { get; }
}
