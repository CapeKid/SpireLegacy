using System.Text.Json;

namespace SpireLegacy;

/// <summary>Compatible family JSON; game run saves remain owned by the StS2 engine.</summary>
public sealed class FamilyProfile
{
    public string family = "Ashford";
    public int banner, generation, crowns, lastEarned;
    public Dictionary<string, int> manor = new();
    public List<Heir> offers = new();
    public Heir? selected;
    public FamilyRun? active;
    public string settledId = "";
    public sealed class Heir
    {
        public string name = "", classId = "knight";
        public List<string> traits = new();
    }
    public sealed class FamilyRun
    {
        public string id = "";
        public Heir heir = new();
        public int floors;
        public Dictionary<string, int> bonuses = new();
        public bool quick;
    }
    public static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true, WriteIndented = true };
    private static readonly string[] Names = ["Ada", "Rowan", "Mira", "Alden", "Kit", "Iris", "Bram", "Sage", "Wren", "Theo", "June", "Vale"];
    public int Level(string id) => manor.GetValueOrDefault(id);
    public int Bonus(string effect) => Design.Rows("manor").Where(r => r.Text("effect") == effect).Sum(r => Level(r.Text("id")) * r.Number("perLevel"));
    public static bool Compatible(Design.Row a, Design.Row b) => a.Text("id") != b.Text("id") && !a.List("excludes").Contains(b.Text("id")) && !b.List("excludes").Contains(a.Text("id"));
    public void GenerateOffers(int seed)
    {
        var random = new Random(seed);
        var classes = Design.Rows("classes").OrderBy(_ => random.Next()).ToArray();
        var traits = Design.Rows("traits").Where(r => r.Flag("enabled")).ToArray();
        offers.Clear();
        foreach (var cls in classes)
        {
            var first = traits[random.Next(traits.Length)];
            var remaining = traits.Where(t => Compatible(first, t)).ToArray();
            if (remaining.Length == 0) throw new InvalidDataException("No compatible trait pair");
            var second = remaining[random.Next(remaining.Length)];
            offers.Add(new Heir { name = Names[random.Next(Names.Length)], classId = cls.Text("id"), traits = [first.Text("id"), second.Text("id")] });
        }
    }
    public FamilyRun Begin(int seed)
    {
        if (active is not null && active.id != settledId) throw new InvalidOperationException("Resume or abandon the unfinished heir first.");
        if (offers.Count == 0) GenerateOffers(seed);
        active = new FamilyRun { id = Guid.NewGuid().ToString(), heir = selected ?? offers[new Random(seed).Next(offers.Count)], quick = generation == 0 };
        foreach (var effect in new[] { "hp", "strength", "dexterity", "draw", "gold", "heal" }) active.bonuses[effect] = Bonus(effect);
        selected = null;
        return active;
    }
    public int Settle(string runId, int floors, int gold, bool victory)
    {
        if (active is null || active.id != runId || settledId == runId) return 0;
        var percent = 100 + active.bonuses.GetValueOrDefault("gold") + active.heir.traits.Sum(id => Design.Get("traits", id).Number("goldBonus"));
        long basis = (long)Math.Max(0, floors) * Design.Get("systems", "legacy").Number("amount") + Math.Max(0, gold) / Design.Get("systems", "gold_conversion").Number("amount") + (victory ? Design.Get("systems", "victory").Number("amount") : 0);
        var earned = (int)Math.Min(int.MaxValue, basis * Math.Clamp(percent, 0, 200) / 100);
        crowns = (int)Math.Min(int.MaxValue, (long)crowns + earned);
        lastEarned = earned; generation++; settledId = runId; active = null;
        GenerateOffers(unchecked(generation * 1009 + StableHash(runId)));
        return earned;
    }
    private static int StableHash(string text) { var result = 0; foreach (var c in text) result = unchecked(result * 31 + c); return result; }
    public int Cost(Design.Row row) => row.Number("baseCost") + Level(row.Text("id")) * row.Number("costStep");
    private bool Purchase(string id)
    {
        var row = Design.Get("manor", id); var prerequisite = row.Text("requires"); var price = Cost(row);
        if (active is not null || Level(id) >= row.Number("maxLevel") || crowns < price || (prerequisite != "none" && Level(prerequisite) == 0)) return false;
        crowns -= price; manor[id] = Level(id) + 1; return true;
    }
    public FamilyProfile? PreviewPurchases(IEnumerable<string> ids)
    {
        var trial = JsonSerializer.Deserialize<FamilyProfile>(JsonSerializer.Serialize(this, JsonOptions), JsonOptions)!;
        foreach (var id in ids) if (!trial.Purchase(id)) return null;
        return trial;
    }
    public bool CommitPurchases(IReadOnlyList<string> ids, string path)
    {
        var trial = PreviewPurchases(ids);
        if (ids.Count == 0 || trial is null) return false;
        trial.Save(path); crowns = trial.crowns; manor = trial.manor; return true;
    }
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { JsonSerializer.Serialize(stream, this, JsonOptions); stream.Flush(true); }
        if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
    }
    private static FamilyProfile Read(string path)
    {
        var value = JsonSerializer.Deserialize<FamilyProfile>(File.ReadAllText(path), JsonOptions);
        if (value is null || value.family is null || value.manor is null || value.offers is null || value.crowns < 0) throw new InvalidDataException("Invalid family save");
        return value;
    }
    public static FamilyProfile Load(string path)
    {
        if (!File.Exists(path)) return new();
        try { return Read(path); }
        catch (Exception first) { try { return Read(path + ".bak"); } catch (Exception second) { throw new InvalidDataException("Family save and backup are damaged; both preserved.", new AggregateException(first, second)); } }
    }
}
