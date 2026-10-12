using System.Text.Json;

namespace SpireLegacy;

/// <summary>Only frozen run effects cross the network; the crown bank stays local.</summary>
public static class FamilySnapshot
{
    public static FamilyProfile.FamilyRun Freeze(FamilyProfile profile,FamilyProfile.Heir heir,ulong owner=0,bool coop=false)
    {
        var run=new FamilyProfile.FamilyRun {
            id=Guid.NewGuid().ToString(),familyId=profile.familyId,family=profile.family,
            generation=profile.generation,banner=profile.banner,motto=profile.chronicle.motto,
            ownerNetId=owner,coop=coop,quick=!coop&&profile.generation==0,
            heir=JsonSerializer.Deserialize<FamilyProfile.Heir>(JsonSerializer.Serialize(heir,FamilyProfile.JsonOptions),FamilyProfile.JsonOptions)!,
            chronicleRelics=FamilyChronicle.Relics(profile).ToList()
        };
        foreach(var effect in new[]{"hp","strength","dexterity","draw","gold","heal"})run.bonuses[effect]=profile.Bonus(effect);
        return run;
    }
    public static string Serialize(FamilyProfile.FamilyRun run)=>JsonSerializer.Serialize(run,FamilyProfile.JsonOptions);
    public static FamilyProfile.FamilyRun Read(string json){using var doc=FamilyData.Document(json);FamilyData.Required(doc.RootElement,"id","familyId","heir","bonuses");FamilyData.HeirFields(doc.RootElement.GetProperty("heir"));var run=JsonSerializer.Deserialize<FamilyProfile.FamilyRun>(json,FamilyProfile.JsonOptions)??throw new InvalidDataException("Missing family snapshot.");FamilyData.Run(run);return run;}
    public static FamilyProfile.FamilyRun ReadNetwork(string json,ulong sender)
    {
        if(json.Length>8192)throw new InvalidDataException("Family snapshot exceeds network limit.");
        var run=Read(json);
        if(!run.coop||run.quick||run.ownerNetId!=sender||!Guid.TryParse(run.id,out _)||!Guid.TryParse(run.familyId,out _)||run.generation<0||run.banner is <0 or >3||run.family.Length>24||run.motto.Length>40)throw new InvalidDataException("Invalid family snapshot identity.");
        if(!Design.Rows("classes").Any(r=>r.Text("id")==run.heir.classId)||run.heir.name.Length>40||run.heir.traits.Count>2||run.heir.traits.Distinct().Count()!=run.heir.traits.Count)throw new InvalidDataException("Invalid network heir.");
        var traits=run.heir.traits.Select(t=>Design.Get("traits",t)).ToArray();
        if(traits.Any(t=>!t.Flag("enabled"))||traits.Length==2&&!FamilyProfile.Compatible(traits[0],traits[1]))throw new InvalidDataException("Incompatible network traits.");
        foreach(var bonus in run.bonuses) {
            var rows=Design.Rows("manor").Where(r=>r.Text("effect")==bonus.Key).ToArray();
            if(rows.Length==0||bonus.Value<0||bonus.Value>rows.Sum(r=>r.Number("maxLevel")*r.Number("perLevel")))throw new InvalidDataException("Invalid network manor bonus.");
        }
        var allowed=Design.Rows("family_chronicle").Where(r=>r.Text("rewardKind")=="relic").Select(r=>r.Text("rewardId")).ToHashSet();
        if(run.chronicleRelics.Count>3||run.chronicleRelics.Distinct().Count()!=run.chronicleRelics.Count||run.chronicleRelics.Any(r=>!allowed.Contains(r)))throw new InvalidDataException("Invalid network Chronicle unlock.");
        return run;
    }
    public static bool CanSettle(FamilyProfile profile,FamilyProfile.FamilyRun run,ulong localNetId)=>
        (run.ownerNetId==localNetId||!run.coop&&run.ownerNetId==0)&&run.familyId==profile.familyId&&
        profile.active is {} active&&active.id==run.id&&active.familyId==profile.familyId&&
        active.ownerNetId==run.ownerNetId&&active.coop==run.coop&&
        profile.settledId!=run.id&&!profile.chronicle.settledRuns.Contains(run.id);
}
