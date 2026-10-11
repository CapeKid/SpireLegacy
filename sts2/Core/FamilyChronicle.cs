namespace SpireLegacy;

public sealed class ChronicleState
{
    public int schema;
    public HashSet<string> unlocked = new(), read = new(), settledRuns = new();
    public Dictionary<string, ClassHistory> classes = new();
    public string motto = "";
    public sealed class ClassHistory { public int returns, maxFloor, wins; }
}

/// <summary>Family-local progression. No native Timeline epochs or existing content are gated.</summary>
public static class FamilyChronicle
{
    public static bool Reconcile(FamilyProfile profile)
    {
        var state=profile.chronicle ??= new();var changed=state.schema==0;if(changed)state.schema=1;
        changed|=state.unlocked.Add("founding");
        if(profile.generation>0)changed|=state.unlocked.Add("first_return");
        if(profile.manor.Values.Any(level=>level>0))changed|=state.unlocked.Add("restoration");
        foreach(var cls in new[]{"knight","mage","ranger"}) {
            if(!state.classes.TryGetValue(cls,out var history))continue;
            if(history.maxFloor>=6)changed|=state.unlocked.Add(cls+"_lesson");
            if(history.wins>0)changed|=state.unlocked.Add(cls+"_victory");
        }
        if(state.motto.Length>0&&!Design.Rows("family_chronicle").Any(r=>r.Text("rewardKind")=="motto"&&r.Text("rewardId")==state.motto&&state.unlocked.Contains(r.Text("id")))) {state.motto="";changed=true;}
        return changed;
    }
    public static void Record(FamilyProfile profile,FamilyProfile.FamilyRun run,int floors,bool victory)
    {
        var state=profile.chronicle;
        if(!state.settledRuns.Add(run.id))return;
        if(!state.classes.TryGetValue(run.heir.classId,out var history))state.classes[run.heir.classId]=history=new();
        history.returns=(int)Math.Min(int.MaxValue,(long)history.returns+1);
        history.maxFloor=Math.Max(history.maxFloor,Math.Max(0,floors));
        if(victory)history.wins=(int)Math.Min(int.MaxValue,(long)history.wins+1);
        Reconcile(profile);
    }
    public static IEnumerable<string> Relics(FamilyProfile profile)=>Design.Rows("family_chronicle")
        .Where(r=>r.Text("rewardKind")=="relic"&&profile.chronicle.unlocked.Contains(r.Text("id"))).Select(r=>r.Text("rewardId"));
    public static bool RelicAvailable(FamilyProfile profile,string key)
    {
        if(!Design.Rows("family_chronicle").Any(r=>r.Text("rewardKind")=="relic"&&r.Text("rewardId")==key))return true;
        return profile.active is {} run?run.chronicleRelics.Contains(key):Relics(profile).Contains(key);
    }
    public static string Progress(FamilyProfile profile,Design.Row entry)
    {
        var cls=entry.Text("classId");var history=profile.chronicle.classes.GetValueOrDefault(cls);
        return cls is not ("knight" or "mage" or "ranger")?entry.Text("requirement"):entry.Text("rewardKind")=="motto"
            ?$"Victories: {history?.wins??0} / 1":$"Best completed climb: {Math.Min(6,history?.maxFloor??0)} / 6 floors";
    }
    public static string Reward(Design.Row entry)=>entry.Text("rewardKind") switch {
        "relic"=>$"Unlock: {Design.Get("heir_relics",entry.Text("rewardId")).Text("title")} in future Heir reward and shop pools (not a starting relic).",
        "motto"=>$"Unlock: family motto “{entry.Text("rewardId")}” for your combat banner.",
        _=>"Unlock: an original family story entry."};
    public static bool SelectMotto(FamilyProfile profile,string motto)
    {
        if(motto.Length>0&&!Design.Rows("family_chronicle").Any(r=>r.Text("rewardKind")=="motto"&&r.Text("rewardId")==motto&&profile.chronicle.unlocked.Contains(r.Text("id"))))return false;
        profile.chronicle.motto=motto;return true;
    }
}
