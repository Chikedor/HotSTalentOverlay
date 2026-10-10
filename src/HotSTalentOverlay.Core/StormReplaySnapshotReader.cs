using System.Text.RegularExpressions;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Player;
using Heroes.StormReplayParser.TrackerEvent;

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
        string matchId = MatchId(replay);
        string heroUnitId = player.PlayerHero?.HeroUnitId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(heroUnitId) && !string.IsNullOrWhiteSpace(player.PlayerHero?.HeroId))
            heroUnitId = "Hero" + player.PlayerHero.HeroId;
        return new ReplaySnapshot(matchId, replay.ReplayBuild, DisplayName(player), heroUnitId,
            player.PlayerHero?.HeroName ?? string.Empty, ids, times, result.Status.ToString())
        { PlayerIdentity = player.ToonHandle?.ToString() ?? player.BattleTagName };
    }

    public string? ReadCompletedMatchId(string filePath)
    {
        StormReplayResult result = StormReplay.Parse(filePath, new ParseOptions
        {
            AllowPTR = true, ShouldParseTrackerEvents = true,
            ShouldParseGameEvents = false, ShouldParseMessageEvents = false,
        });
        if (result.Status == StormReplayParseStatus.Exception)
            throw new InvalidDataException("La replay final todavía no se puede leer.", result.Exception);
        return result.Replay.TrackerEvents.Any(e => e.TrackerEventType == StormTrackerEventType.ScoreResultEvent)
            ? MatchId(result.Replay) : null;
    }

    private static string MatchId(StormReplay replay) => replay.RandomValue != 0
        ? replay.RandomValue.ToString() : $"{replay.ReplayBuild}:{replay.Timestamp:O}";

    public static StormPlayer? SelectLocalPlayer(StormReplay replay, string filePath, string configuredBattleTag)
    {
        List<StormPlayer> players = replay.StormPlayers.ToList();
        Match toon = ToonHandlePattern().Match(filePath);
        if (toon.Success)
        {
            string expected = toon.Value;
            StormPlayer? byHandle = players.FirstOrDefault(x => string.Equals(x.ToonHandle?.ToString(), expected, StringComparison.OrdinalIgnoreCase));
            if (byHandle is not null)
                return byHandle;
            // An authoritative account path must never fall back to another player.
            return null;
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
            return null;
        }

        return replay.Owner is not null && players.Contains(replay.Owner) ? replay.Owner : null;
    }

    public static IReadOnlyList<int> TalentTiers => Tiers;
    private static string DisplayName(StormPlayer player) => !string.IsNullOrWhiteSpace(player.BattleTagName) ? player.BattleTagName.Split('#')[0] : player.Name;

    [GeneratedRegex(@"[1-9][0-9]*-Hero-[0-9]+-[0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex ToonHandlePattern();
}
