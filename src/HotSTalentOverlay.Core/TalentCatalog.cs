using System.Text.Json;

namespace HotSTalentOverlay.Core;

public sealed class TalentCatalog
{
    private readonly Dictionary<string, TalentCatalogEntry> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TalentCatalogEntry> _byHeroAndId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _heroNames = new(StringComparer.OrdinalIgnoreCase);

    public CatalogDocument? Document { get; private set; }

    public void Load(string path)
    {
        CatalogDocument document = JsonSerializer.Deserialize<CatalogDocument>(File.ReadAllText(path), JsonOptions.Default)
            ?? throw new InvalidDataException("talents.json está vacío o no es válido.");
        Set(document);
    }

    public void Set(CatalogDocument document)
    {
        _byId.Clear();
        _byHeroAndId.Clear();
        _heroNames.Clear();
        foreach (TalentCatalogEntry talent in document.Talents)
        {
            _byId[talent.TalentTreeId] = talent;
            _byHeroAndId[Key(talent.HeroUnitId, talent.TalentTreeId)] = talent;
            _heroNames[talent.HeroUnitId] = talent.Hero;
        }
        Document = document;
    }

    public TalentCatalogEntry? Find(string talentId, string? heroUnitId = null) =>
        !string.IsNullOrWhiteSpace(heroUnitId) && _byHeroAndId.TryGetValue(Key(heroUnitId, talentId), out TalentCatalogEntry? exact)
            ? exact
            : _byId.GetValueOrDefault(talentId);
    public string? HeroName(string unitId) => _heroNames.GetValueOrDefault(unitId);
    private static string Key(string heroUnitId, string talentId) => heroUnitId + "\0" + talentId;
}
