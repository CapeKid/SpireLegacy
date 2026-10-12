using System.Text.Json;

namespace SpireLegacy;

public sealed class UnsupportedFamilySchemaException(string message) : IOException(message);

// Reject malformed progress before it reaches stat arithmetic, adoption or migration.
public static class FamilyData
{
    public const int MaxBytes=8*1024*1024;
    private static readonly HashSet<string> Classes=Design.Rows("classes").Select(r=>r.Text("id")).ToHashSet();
    private static readonly HashSet<string> Traits=Design.Rows("traits").Select(r=>r.Text("id")).ToHashSet();
    private static readonly HashSet<string> Entries=Design.Rows("family_chronicle").Select(r=>r.Text("id")).ToHashSet();
    private static readonly HashSet<string> Relics=Design.Rows("family_chronicle").Where(r=>r.Text("rewardKind")=="relic").Select(r=>r.Text("rewardId")).ToHashSet();
    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool valid,string field){if(!valid)throw new InvalidDataException("Invalid family data: "+field+". Original files preserved; restore a valid backup.");}
    private static bool Text(string? value,int max)=>value!=null&&value.Length<=max;
    private static bool Id(string? value,bool optional=false)=>optional&&value==""||Guid.TryParse(value,out _);
    public static JsonDocument Document(string json)
    {
        Require(System.Text.Encoding.UTF8.GetByteCount(json)<=MaxBytes,"size limit");
        var doc=JsonDocument.Parse(json);
        try{Require(doc.RootElement.ValueKind==JsonValueKind.Object,"root object");if(doc.RootElement.TryGetProperty("schema",out var schema)&&schema.ValueKind==JsonValueKind.Number&&schema.TryGetInt64(out var version)&&version>1)throw new UnsupportedFamilySchemaException("This family uses a newer schema; original primary and backup are preserved. Use a compatible mod version.");
        if(doc.RootElement.TryGetProperty("chronicle",out var history)&&history.ValueKind==JsonValueKind.Object&&history.TryGetProperty("schema",out var historySchema)&&historySchema.ValueKind==JsonValueKind.Number&&historySchema.TryGetInt64(out var historyVersion)&&historyVersion>1)throw new UnsupportedFamilySchemaException("This Chronicle uses a newer schema; original primary and backup are preserved. Use a compatible mod version.");
        Unique(doc.RootElement);return doc;}catch{doc.Dispose();throw;}
    }
    public static void Required(JsonElement value,params string[] fields){Require(value.ValueKind==JsonValueKind.Object,"object fields");foreach(var field in fields)Require(value.TryGetProperty(field,out _),"missing "+field);}
    public static void HeirFields(JsonElement value)=>Required(value,"name","classId","traits");
    private static void Unique(JsonElement element)
    {
        if(element.ValueKind==JsonValueKind.Object){var names=new HashSet<string>();foreach(var p in element.EnumerateObject()){Require(names.Add(p.Name),"duplicate property "+p.Name);if(p.Value.ValueKind==JsonValueKind.Array&&p.Name is "traits" or "chronicleRelics" or "unlocked" or "read" or "settledRuns"){
                var ids=new HashSet<string?>();foreach(var item in p.Value.EnumerateArray())if(item.ValueKind==JsonValueKind.String||item.ValueKind==JsonValueKind.Null)Require(ids.Add(item.GetString()),"duplicate ID in "+p.Name);
            }Unique(p.Value);}}
        else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray())Unique(item);
    }
    public static void Heir(FamilyProfile.Heir? heir)
    {
        Require(heir!=null,"heir");Require(Text(heir!.name,40)&&Classes.Contains(heir.classId),"heir name/class");
        Require(heir.traits!=null&&heir.traits.Count<=2&&heir.traits.All(t=>t!=null&&Traits.Contains(t))&&heir.traits.Distinct().Count()==heir.traits.Count,"traits");
    }
    public static void Run(FamilyProfile.FamilyRun? run)
    {
        Require(run!=null,"run");Require(Id(run!.id)&&Id(run.familyId)&&Text(run.family,24)&&Text(run.motto,40),"run identity");
        Require(run.generation>=0&&run.floors>=0&&run.banner is >=0 and <=3&&(!run.coop||!run.quick),"run counters");Heir(run.heir);
        Require(run.bonuses!=null&&run.bonuses.All(b=>Design.Rows("manor").Any(r=>r.Text("effect")==b.Key)&&b.Value>=0&&b.Value<=Design.Rows("manor").Where(r=>r.Text("effect")==b.Key).Sum(r=>Math.Max(5,r.Number("maxLevel"))*r.Number("perLevel"))),"frozen manor bonuses");
        Require(run.chronicleRelics!=null&&run.chronicleRelics.Count<=3&&run.chronicleRelics.Distinct().Count()==run.chronicleRelics.Count&&run.chronicleRelics.All(r=>r!=null&&Relics.Contains(r)),"run Chronicle relics");
    }
    public static void Profile(FamilyProfile p)
    {
        if(p.schema>1||p.chronicle?.schema>1)throw new UnsupportedFamilySchemaException("This family was written by a newer mod schema; use a compatible version. Primary and backup are preserved.");
        Require(p.schema is >=0 and <=1&&Id(p.familyId)&&Text(p.family,24)&&p.banner is >=0 and <=3&&p.generation>=0&&p.crowns>=0&&p.lastEarned>=0&&Id(p.settledId,true),"profile identity/counters");
        Require(p.manor!=null&&p.manor.All(m=>Design.Rows("manor").Any(r=>r.Text("id")==m.Key)&&m.Value>=0&&m.Value<=Math.Max(5,Design.Get("manor",m.Key).Number("maxLevel"))),"manor levels");
        Require(p.offers!=null&&p.offers.Count<=3,"offers");foreach(var heir in p.offers!)Heir(heir);if(p.selected!=null)Heir(p.selected);
        if(p.active!=null){Run(p.active);Require(p.active.familyId==p.familyId,"active family identity");}
        var c=p.chronicle;Require(c!=null&&c.schema is >=0 and <=1&&Text(c.motto,40)&&c.classes!=null&&c.unlocked!=null&&c.read!=null&&c.settledRuns!=null,"Chronicle fields");
        Require(c!.classes.All(k=>Classes.Contains(k.Key)&&k.Value!=null&&k.Value.returns>=0&&k.Value.maxFloor>=0&&k.Value.wins>=0),"Chronicle classes/counters");
        Require(c.unlocked.All(Entries.Contains)&&c.read.All(Entries.Contains)&&c.settledRuns.Count<=100000&&c.settledRuns.All(s=>Id(s)),"Chronicle IDs");
        Require(p.active==null||p.active.id!=p.settledId&&!c.settledRuns.Contains(p.active.id),"already settled active run");
    }
}
