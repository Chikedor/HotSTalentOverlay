using Microsoft.Win32;

namespace HotSTalentOverlay.Core;

public sealed class GameInstallationDetector
{
    private static readonly string[] RegistryKeys =
    [
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Heroes of the Storm",
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Heroes of the Storm",
    ];

    public DetectedGame? Detect(string? configuredPath = null)
    {
        IEnumerable<string> candidates = CandidatePaths(configuredPath).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (string candidate in candidates)
        {
            string buildInfo = Path.Combine(candidate, ".build.info");
            if (!File.Exists(buildInfo))
                continue;
            string? version = ParseVersion(File.ReadAllText(buildInfo));
            if (version is null)
                continue;
            int build = int.TryParse(version.Split('.').LastOrDefault(), out int value) ? value : 0;
            return new DetectedGame(candidate, version, build);
        }

        return null;
    }

    public static string? ParseVersion(string buildInfo)
    {
        string[] lines = buildInfo.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return null;
        string[] headings = lines[0].Split('|');
        string[] values = lines.FirstOrDefault(x => !x.StartsWith("Branch!", StringComparison.Ordinal))?.Split('|') ?? [];
        int index = Array.FindIndex(headings, x => x.StartsWith("Version!", StringComparison.Ordinal));
        return index >= 0 && index < values.Length && Version.TryParse(values[index], out _) ? values[index] : null;
    }

    private static IEnumerable<string> CandidatePaths(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            yield return configuredPath;

        if (OperatingSystem.IsWindows())
        {
            foreach (string keyName in RegistryKeys)
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyName);
                if (key?.GetValue("InstallLocation") is string path && !string.IsNullOrWhiteSpace(path))
                    yield return path;
            }
        }

        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Heroes of the Storm");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Heroes of the Storm");
        yield return @"C:\Games\Heroes of the Storm";
    }
}

