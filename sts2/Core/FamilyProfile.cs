using System.Text.Json;

namespace SpireLegacy;

/// <summary>Compatible family JSON; game run saves remain owned by the StS2 engine.</summary>
public sealed class FamilyProfile
{
    public string familyId = Guid.NewGuid().ToString();
    public string family = "Ashford";
    public int banner, generation, crowns, lastEarned;
    public Dictionary<string, int> manor = new();
    public List<Heir> offers = new();
    public Heir? selected;
    public FamilyRun? active;
    public string settledId = "";
    public ChronicleState chronicle = new();
    public sealed class Heir
    {
        public string name = "", classId = "knight";
        public List<string> traits = new();
    }
    public sealed class FamilyRun
    {
        public string id = "";
        public string familyId = "", family = "", motto = "";
        public ulong ownerNetId;
        public int generation, banner;
        public bool coop;
        public Heir heir = new();
        public int floors;
        public Dictionary<string, int> bonuses = new();
        public bool quick;
        public List<string> chronicleRelics = new();
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
        active = FamilySnapshot.Freeze(this,selected ?? offers[new Random(seed).Next(offers.Count)]);
        FamilyChronicle.Reconcile(this);
        active.chronicleRelics=FamilyChronicle.Relics(this).ToList();
        foreach (var effect in new[] { "hp", "strength", "dexterity", "draw", "gold", "heal" }) active.bonuses[effect] = Bonus(effect);
        selected = null;
        return active;
    }
    public int Settle(string runId, int floors, int gold, bool victory)
    {
        if (active is null || active.id != runId || settledId == runId || chronicle.settledRuns.Contains(runId)) return 0;
        var percent = Math.Clamp(100 + active.heir.traits.Sum(id => Design.Get("traits", id).Number("goldBonus")), 0, 200);
        long basis = (long)Math.Max(0, floors) * Design.Get("systems", "legacy").Number("amount") + Math.Max(0, gold) / Design.Get("systems", "gold_conversion").Number("amount") + (victory ? Design.Get("systems", "victory").Number("amount") : 0);
        var earned = (int)Math.Min(int.MaxValue, basis * percent * (100L + active.bonuses.GetValueOrDefault("gold")) / 10000);
        crowns = (int)Math.Min(int.MaxValue, (long)crowns + earned);
        lastEarned = earned; generation++; settledId = runId;
        FamilyChronicle.Record(this,active,floors,victory);active = null;
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
        FamilyChronicle.Reconcile(trial);
        trial.Save(path); crowns = trial.crowns; manor = trial.manor; chronicle=trial.chronicle; return true;
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
        var serialized=File.ReadAllText(path);
        var value = JsonSerializer.Deserialize<FamilyProfile>(serialized, JsonOptions);
        if (value is null || value.family is null || value.manor is null || value.offers is null || value.crowns < 0) throw new InvalidDataException("Invalid family save");
        if(value.chronicle is {} history&&(history.classes is null||history.unlocked is null||history.read is null||history.settledRuns is null||history.motto is null||history.classes.Any(p=>p.Value is null||p.Value.returns<0||p.Value.maxFloor<0||p.Value.wins<0)))throw new InvalidDataException("Invalid family Chronicle; backup will be tried.");
        using var document=JsonDocument.Parse(serialized);
        if(!document.RootElement.TryGetProperty("familyId",out _))value.familyId="";
        return value;
    }
    public bool RefundRetiredLevels()
    {
        var changed = false;
        foreach (var row in Design.Rows("manor"))
        {
            var id = row.Text("id"); var old = Level(id); var max = row.Number("maxLevel");
            if (old <= max) continue;
            if (old > 5) throw new InvalidDataException("Invalid legacy manor level: " + id);
            long refund = 0;
            for (var level = max; level < old; level++) refund += row.Number("baseCost") + (long)level * row.Number("costStep");
            manor[id] = max; crowns = (int)Math.Min(int.MaxValue, crowns + refund); changed = true;
        }
        return changed;
    }
    public static FamilyProfile Load(string path)
    {
        if (!File.Exists(path)) {var fresh=new FamilyProfile();FamilyChronicle.Reconcile(fresh);return fresh;}
        FamilyProfile profile;
        try { profile = Read(path); }
        catch (Exception first) { try { profile = Read(path + ".bak"); } catch (Exception second) { throw new InvalidDataException("Family save and backup are damaged; both preserved.", new AggregateException(first, second)); } }
        var changed=profile.RefundRetiredLevels();changed|=FamilyChronicle.Reconcile(profile);
        if(string.IsNullOrEmpty(profile.familyId)){profile.familyId=Guid.NewGuid().ToString();changed=true;}
        if (changed) profile.Save(path);
        return profile;
    }
}
