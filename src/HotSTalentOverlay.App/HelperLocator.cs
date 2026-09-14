namespace HotSTalentOverlay.App;

public static class HelperLocator
{
    public static string Locate()
    {
        string[] executables = OperatingSystem.IsWindows()
            ? ["HeroesDataParser.exe", "dotnet-heroes-data-parser.exe"]
            : ["HeroesDataParser", "dotnet-heroes-data-parser"];
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (string executable in executables)
            {
                foreach (string relative in new[] { Path.Combine("tools", executable), Path.Combine("tools", "win-x64", executable), Path.Combine(".tools", executable) })
                {
                    string candidate = Path.Combine(directory.FullName, relative);
                    if (File.Exists(candidate)) return candidate;
                }
            }
            directory = directory.Parent;
        }
        return Path.Combine(AppContext.BaseDirectory, "tools", executables[0]);
    }
}
