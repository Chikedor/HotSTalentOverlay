using System.Reflection;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Player;
using HotSTalentOverlay.App;
using HotSTalentOverlay.Core;
using Xunit;

namespace HotSTalentOverlay.Tests;

public sealed class MatchLifecycleTests
{
    private static ReplaySnapshot Live(string id = "match", string hero = "HeroCrusader", string identity = "local")
        => new(id, 98348, "Player", hero, hero, ["Talent1"], [1], "Incomplete") { PlayerIdentity = identity };
    private static RuntimeState State() => new(new TalentCatalog(), new EventHub());

    [Fact]
    public void FinalReplayClearsMatchAndLateSavesCannotRestoreIt()
    {
        RuntimeState state = State();
        state.ApplyReplay(Live());
        state.CompleteMatch("match");
        state.ApplyReplay(Live());
        state.ApplyReplay(Live(hero: "HeroWhitemane", identity: "enemy"));
        state.RefreshCatalog();
        Assert.Empty(state.Snapshot.MatchId);
        Assert.Empty(state.Snapshot.HeroUnitId);
        Assert.Empty(state.Snapshot.Talents);
        state.ApplyReplay(Live("next"));
        Assert.Equal("next", state.Snapshot.MatchId);
        Assert.Single(state.Snapshot.Talents);
    }

    [Fact]
    public void UnrelatedFinalReplayDoesNotClearCurrentMatch()
    {
        RuntimeState state = State();
        state.ApplyReplay(Live());
        state.CompleteMatch("old");
        Assert.Equal("match", state.Snapshot.MatchId);
    }

    [Fact]
    public void CompletionBeforeFirstSnapshotAlsoBlocksResurrection()
    {
        RuntimeState state = State();
        state.CompleteMatch("match");
        state.ApplyReplay(Live());
        Assert.Empty(state.Snapshot.Talents);
    }

    [Fact]
    public void PlayerAndHeroCannotChangeWithinSameMatch()
    {
        RuntimeState state = State();
        state.ApplyReplay(Live());
        state.ApplyReplay(Live(identity: "enemy"));
        state.ApplyReplay(Live(hero: "HeroWhitemane"));
        Assert.Equal("HeroCrusader", state.Snapshot.HeroUnitId);
        state.ApplyReplay(Live("next", "HeroWhitemane", "enemy"));
        Assert.Equal("HeroWhitemane", state.Snapshot.HeroUnitId);
    }

    private static (StormReplay Replay, StormPlayer Local, StormPlayer Enemy) Players()
    {
        StormPlayer local = new() { Name = "Local", ToonHandle = new() { Region = 2, ProgramId = 0x4865726f, Realm = 1, Id = 123 } };
        StormPlayer enemy = new() { Name = "Enemy", ToonHandle = new() { Region = 2, ProgramId = 0x4865726f, Realm = 1, Id = 456 } };
        StormReplay replay = (StormReplay)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(StormReplay));
        replay.Owner = enemy;
        typeof(StormReplay).GetProperty("Players", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(replay, new StormPlayer?[] { local, enemy });
        return (replay, local, enemy);
    }

    [Fact]
    public void AccountIdentityWinsOverEnemyOwner()
    {
        var (replay, local, _) = Players();
        Assert.Same(local, StormReplaySnapshotReader.SelectLocalPlayer(replay,
            "C:/Accounts/1/2-Hero-1-123/Saves/Rejoin/live.StormSave", ""));
    }

    [Fact]
    public void ConfiguredPlayerWinsOverEnemyOwnerWithoutAccountPath()
    {
        var (replay, local, _) = Players();
        Assert.Same(local, StormReplaySnapshotReader.SelectLocalPlayer(replay, "live.StormSave", "Local#1234"));
    }

    [Fact]
    public void MissingConfiguredAccountDoesNotFallBackToEnemyOwner()
    {
        var (replay, _, _) = Players();
        Assert.Null(StormReplaySnapshotReader.SelectLocalPlayer(replay, "C:/2-Hero-1-999/live.StormSave", ""));
        Assert.Null(StormReplaySnapshotReader.SelectLocalPlayer(replay, "live.StormSave", "Missing#1234"));
    }

    [Fact]
    public void RealFinalReplayMatchesReaderIdentityWhenFixtureProvided()
    {
        string? path = Environment.GetEnvironmentVariable("HOTS_FINAL_REPLAY_FIXTURE");
        if (string.IsNullOrWhiteSpace(path)) return;
        StormReplaySnapshotReader reader = new();
        ReplaySnapshot snapshot = reader.Read(path);
        Assert.Equal(snapshot.MatchId, reader.ReadCompletedMatchId(path));
        Assert.NotEmpty(snapshot.PlayerIdentity);
    }
}
