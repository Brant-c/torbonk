namespace SwtorSurvivors.Frontend;

/// <summary>Owns choices, never the screens. Remembers each faction/class branch during browsing.</summary>
public sealed class SelectionSession
{
    public SelectionConfiguration Configuration { get; private set; } = new();
    private readonly Dictionary<string, string> _baseByFaction = new();
    private readonly Dictionary<string, string> _advancedByBase = new();
    public void SelectFaction(string id, FrontendCatalog catalog)
    {
        _baseByFaction[Configuration.FactionId] = Configuration.BaseClassId;
        Configuration.FactionId = id;
        SelectClass(_baseByFaction.TryGetValue(id, out var remembered) ? remembered : catalog.Classes.First(c => c.FactionId == id).Id, catalog);
    }
    public void SelectClass(string id, FrontendCatalog catalog)
    {
        _advancedByBase[Configuration.BaseClassId] = Configuration.AdvancedClassId;
        var selected = catalog.Class(id);
        if (selected.FactionId != Configuration.FactionId) throw new ArgumentException("Class belongs to another faction.");
        Configuration.BaseClassId = id;
        Configuration.AdvancedClassId = _advancedByBase.TryGetValue(id, out var remembered) ? remembered : selected.AdvancedClasses[0].Id;
    }
    public void SelectAdvanced(string id, FrontendCatalog catalog)
    {
        if (!catalog.Class(Configuration.BaseClassId).AdvancedClasses.Any(a => a.Id == id)) throw new ArgumentException("Advanced class belongs to another base class.");
        Configuration.AdvancedClassId = id;
        _advancedByBase[Configuration.BaseClassId] = id;
    }
    public void SelectMap(string id, FrontendCatalog catalog) { catalog.Map(id); Configuration.MapId = id; }
    public void Restore(SelectionConfiguration config, FrontendCatalog catalog)
    {
        if (!catalog.Factions.Any(f => f.Id == config.FactionId)
            || !catalog.Classes.Any(c => c.Id == config.BaseClassId && c.FactionId == config.FactionId
                && c.AdvancedClasses.Any(a => a.Id == config.AdvancedClassId))
            || !catalog.Maps.Any(m => m.Id == config.MapId)) throw new ArgumentException("Unknown selection identifiers.");
        Configuration = config.Snapshot();
        _baseByFaction.Clear(); _advancedByBase.Clear();
    }
}
