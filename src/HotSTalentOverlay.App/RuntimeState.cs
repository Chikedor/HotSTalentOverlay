using HotSTalentOverlay.Core;

namespace HotSTalentOverlay.App;

public sealed class RuntimeState
{
    private readonly object _gate = new();
    private readonly TalentCatalog _catalog;
    private readonly EventHub _events;
    private AppStatus _status = new();
    private ReplaySnapshot? _lastReplay;

    public RuntimeState(TalentCatalog catalog, EventHub events)
    {
        _catalog = catalog;
        _events = events;
    }

    public AppStatus Snapshot { get { lock (_gate) return _status; } }

    public void Update(Func<AppStatus, AppStatus> update)
    {
        lock (_gate)
            _status = update(_status) with { UpdatedAt = DateTimeOffset.UtcNow };
        _events.Publish(Snapshot);
    }

    public void ApplyReplay(ReplaySnapshot replay)
    {
        lock (_gate) _lastReplay = replay;
        List<DetectedTalent> talents = [];
        for (int index = 0; index < replay.TalentIds.Count; index++)
        {
            string id = replay.TalentIds[index];
            TalentCatalogEntry? entry = _catalog.Find(id, replay.HeroUnitId);
            int level = entry?.Level ?? StormReplaySnapshotReader.TalentTiers.ElementAtOrDefault(index);
            talents.Add(new DetectedTalent(level, id, entry?.Name ?? id,
                entry is null ? string.Empty : "/" + entry.Icon.Replace('\\', '/'),
                index < replay.TalentTimes.Count ? replay.TalentTimes[index] : null));
        }

        Update(status => status with
        {
            MatchId = replay.MatchId,
            Player = replay.Player,
            HeroUnitId = replay.HeroUnitId,
            HeroName = _catalog.HeroName(replay.HeroUnitId) ?? replay.HeroName,
            Talents = talents.OrderBy(x => x.Level).ToArray(),
            LastError = string.Empty,
        });
    }

    public void RefreshCatalog()
    {
        ReplaySnapshot? replay;
        lock (_gate) replay = _lastReplay;
        if (replay is not null)
            ApplyReplay(replay);
    }

    public void ResetMatch()
    {
        lock (_gate) _lastReplay = null;
        Update(status => status with
        {
            MatchId = string.Empty, Player = string.Empty, HeroUnitId = string.Empty, HeroName = string.Empty, Talents = [], LastError = string.Empty,
        });
    }
}
