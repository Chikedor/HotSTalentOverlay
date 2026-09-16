namespace HotSTalentOverlay.Core;

public static class ReleaseVersionParser
{
    public static Version? Parse(string? value)
    {
        string normalized = (value ?? string.Empty).Trim().TrimStart('v', 'V');
        int separator = normalized.IndexOfAny(['-', '+']);
        if (separator >= 0) normalized = normalized[..separator];
        return Version.TryParse(normalized, out Version? version) ? version : null;
    }
}
