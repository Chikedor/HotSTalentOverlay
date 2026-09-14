using System.Text.Json;
using HotSTalentOverlay.Core;
using Xunit;

namespace HotSTalentOverlay.Tests;

public sealed class CoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "HotSTalentOverlayTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void BuildInfoParserReadsActiveVersion()
    {
        const string input = "Branch!STRING:0|Active!DEC:1|Version!STRING:0|Product!STRING:0\neu|1|2.55.17.98025|hero";
        Assert.Equal("2.55.17.98025", GameInstallationDetector.ParseVersion(input));
    }

    [Fact]
    public void BuildInfoParserRejectsMalformedData() => Assert.Null(GameInstallationDetector.ParseVersion("not build info"));

    [Fact]
    public async Task CatalogImportUsesStableIdTierAndCopiesOnlyReferencedIcon()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(_root);
        string images = Path.Combine(_root, "source-images");
        Directory.CreateDirectory(images);
        await File.WriteAllBytesAsync(Path.Combine(images, "valla.png"), [1, 2, 3], cancellationToken);
        string json = Path.Combine(_root, "heroes.json");
        await File.WriteAllTextAsync(json, """
        {"meta":{"heroesVersion":"2.55.17.98025"},"items":{"Valla":{"name":"Valla","unitId":"HeroValla","talents":{"Level4":[{"talentId":"VallaHotPursuit","name":"Hot Pursuit","icon":"valla.png"}]}}}}
        """, cancellationToken);
        AppPaths paths = new(Path.Combine(_root, "state"));
        CatalogDocument result = await new CatalogBuilder(paths).ImportAsync(json, images, "enUS", cancellationToken);

        TalentCatalogEntry talent = Assert.Single(result.Talents);
        Assert.Equal("VallaHotPursuit", talent.TalentTreeId);
        Assert.Equal(4, talent.Level);
        Assert.True(File.Exists(Path.Combine(paths.TalentAssets, "valla.png")));
        Assert.Equal(98025, result.Build);
    }

    [Fact]
    public async Task ConfigStorePersistsSettingsAtomically()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        AppPaths paths = new(Path.Combine(_root, "state"));
        ConfigStore store = new(paths);
        AppConfig expected = new()
        {
            BattleTag = "Chike#1234", ObsPort = 4874, Locale = "esES", UiLanguage = "en",
            OverlayStyle = new OverlayStyleConfig { AccentColor = "#ff00aa", BorderStyle = "double", EntryAnimation = "pop", ShowTalentNames = true },
        };
        await store.SaveAsync(expected, cancellationToken);
        Assert.Equal(expected, new ConfigStore(paths).Current);
        Assert.NotNull(JsonDocument.Parse(await File.ReadAllTextAsync(paths.ConfigFile, cancellationToken)));
    }

    [Fact]
    public async Task ConfigStoreRejectsInvalidOverlayStyle()
    {
        AppPaths paths = new(Path.Combine(_root, "state"));
        ConfigStore store = new(paths);
        AppConfig invalid = new() { OverlayStyle = new OverlayStyleConfig { AccentColor = "purple" } };
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(invalid, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConfigStoreAddsDefaultsToLegacyConfig()
    {
        AppPaths paths = new(Path.Combine(_root, "legacy-state"));
        await File.WriteAllTextAsync(paths.ConfigFile, """{"obsPort":3874,"locale":"enUS"}""", TestContext.Current.CancellationToken);

        AppConfig config = new ConfigStore(paths).Current;

        Assert.Equal("es", config.UiLanguage);
        Assert.Equal(new OverlayStyleConfig(), config.OverlayStyle);
    }

    [Fact]
    public async Task SnapshotCopyCanReadSourceSharedForWriting()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(_root);
        string source = Path.Combine(_root, "live.StormSave");
        await File.WriteAllBytesAsync(source, [1, 2, 3, 4], cancellationToken);
        await using FileStream held = new(source, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        string copy = await ResilientFileCopy.CreateSnapshotAsync(source, Path.Combine(_root, "temp"), cancellationToken);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, await File.ReadAllBytesAsync(copy, cancellationToken));
    }

    [Fact]
    public async Task RealStormSaveParsesWhenFixtureIsProvided()
    {
        string? path = Environment.GetEnvironmentVariable("HOTS_STORMSAVE_FIXTURE");
        if (string.IsNullOrWhiteSpace(path)) return;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string copy = await ResilientFileCopy.CreateSnapshotAsync(path, Path.Combine(_root, "live-copy"), cancellationToken);
        ReplaySnapshot replay = new StormReplaySnapshotReader().Read(copy, identitySourcePath: path);
        Assert.True(replay.Build > 90000);
        Assert.NotEmpty(replay.HeroUnitId);
        Assert.NotEmpty(replay.TalentIds);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
    }
}
