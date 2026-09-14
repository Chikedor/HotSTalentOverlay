using System.Text.RegularExpressions;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Player;

namespace HotSTalentOverlay.Core;

public sealed partial class StormReplaySnapshotReader
{
    private static readonly int[] Tiers = [1, 4, 7, 10, 13, 16, 20];

    public ReplaySnapshot Read(string filePath, string configuredBattleTag = "", string? identitySourcePath = null)
    {
        StormReplayResult result = StormReplay.Parse(filePath, new ParseOptions
        {
            AllowPTR = true,
            ShouldParseTrackerEvents = true,
            ShouldParseGameEvents = true,
            ShouldParseMessageEvents = false,
        });
        if (result.Status == StormReplayParseStatus.Exception)
        {
            if (result.Exception is not null)
                throw result.Exception;
            throw new InvalidDataException("El parser no pudo leer el StormSave.");
        }

        StormReplay replay = result.Replay;
        StormPlayer player = SelectLocalPlayer(replay, identitySourcePath ?? filePath, configuredBattleTag)
            ?? throw new InvalidDataException("No se pudo identificar al jugador local. Configura BattleTag en la interfaz.");
        string[] ids = player.Talents.Where(x => !string.IsNullOrWhiteSpace(x.TalentNameId)).Select(x => x.TalentNameId!).ToArray();
        double?[] times = player.Talents.Where(x => !string.IsNullOrWhiteSpace(x.TalentNameId)).Select(x => x.Timestamp?.TotalSeconds).ToArray();
        string matchId = replay.RandomValue != 0 ? replay.RandomValue.ToString() : $"{replay.ReplayBuild}:{replay.Timestamp:O}";
        string heroUnitId = player.PlayerHero?.HeroUnitId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(heroUnitId) && !string.IsNullOrWhiteSpace(player.PlayerHero?.HeroId))
            heroUnitId = "Hero" + player.PlayerHero.HeroId;
        return new ReplaySnapshot(matchId, replay.ReplayBuild, DisplayName(player), heroUnitId,
            player.PlayerHero?.HeroName ?? string.Empty, ids, times, result.Status.ToString());
    }

    public static StormPlayer? SelectLocalPlayer(StormReplay replay, string filePath, string configuredBattleTag)
    {
        List<StormPlayer> players = replay.StormPlayers.ToList();
        if (replay.Owner is not null && players.Contains(replay.Owner))
            return replay.Owner;

        Match toon = ToonHandlePattern().Match(filePath);
        if (toon.Success)
        {
            string expected = toon.Value;
            StormPlayer? byHandle = players.FirstOrDefault(x => string.Equals(x.ToonHandle?.ToString(), expected, StringComparison.OrdinalIgnoreCase));
            if (byHandle is not null)
                return byHandle;
        }

        if (!string.IsNullOrWhiteSpace(configuredBattleTag))
        {
            StormPlayer? exact = players.FirstOrDefault(x => string.Equals(x.BattleTagName, configuredBattleTag, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                return exact;
            string name = configuredBattleTag.Split('#')[0];
            StormPlayer[] byName = players.Where(x => string.Equals(DisplayName(x), name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (byName.Length == 1)
                return byName[0];
        }

        return null;
    }

    public static IReadOnlyList<int> TalentTiers => Tiers;
    private static string DisplayName(StormPlayer player) => !string.IsNullOrWhiteSpace(player.BattleTagName) ? player.BattleTagName.Split('#')[0] : player.Name;

    [GeneratedRegex(@"[1-9][0-9]*-Hero-[0-9]+-[0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex ToonHandlePattern();
}
