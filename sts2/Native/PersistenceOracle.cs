using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Invoked only through the guarded disposable-profile test bridge.
public static class PersistenceOracle
{
    public static object Snapshot(RunState run)
    {
        var player=run.Players.Single();return new{snapshot=FamilySnapshot.Serialize(PlayerBloodlines.State(player)!),selected=Runtime.Profile.selected?.classId,profileFamily=Runtime.Profile.family};
    }
    public static object ChangeMenu(RunState run)
    {
        var current=PlayerBloodlines.Heir(run.Players.Single()).classId;
        Runtime.Profile.selected=new(){name="Menu replacement",classId=current=="mage"?"ranger":"mage",traits=[]};Runtime.Profile.family="Changed menu family";Runtime.Profile.manor["vitality"]=5;Runtime.Profile.Save(Runtime.FamilyPath);
        return Snapshot(run);
    }
    public static object Adoption(RunState run)
    {
        var profile=Runtime.Profile;var active=profile.active;var selected=profile.selected;var settled=profile.settledId;var hashes=profile.chronicle.settledRuns.ToArray();var player=run.Players.Single();var state=PlayerBloodlines.State(player)!;var family=state.familyId;var id=state.id;var owner=state.ownerNetId;var bank=profile.crowns;
        var before=File.ReadAllBytes(Runtime.FamilyPath);var failures=0;var refusals=0;
        try {
            state.familyId=Guid.NewGuid().ToString();AdoptLocalBloodline.Postfix(RunManager.Instance);if(profile.active!=active||profile.crowns!=bank||!before.SequenceEqual(File.ReadAllBytes(Runtime.FamilyPath)))throw new InvalidDataException("Foreign solo snapshot changed family.");refusals++;
            state.familyId=family;state.ownerNetId=ulong.MaxValue;AdoptLocalBloodline.Postfix(RunManager.Instance);if(profile.active!=active||profile.crowns!=bank)throw new InvalidDataException("Mismatched owner adopted.");refusals++;state.ownerNetId=owner;
            profile.settledId=id;try{AdoptLocalBloodline.Postfix(RunManager.Instance);throw new Exception("Settled adoption passed");}catch(InvalidDataException){failures++;}profile.settledId=settled;
            profile.chronicle.settledRuns.Add(id);try{AdoptLocalBloodline.Postfix(RunManager.Instance);throw new Exception("Historical settlement adopted");}catch(InvalidDataException){failures++;}profile.chronicle.settledRuns.Remove(id);
            profile.active=new FamilyProfile.FamilyRun{id=Guid.NewGuid().ToString()};try{AdoptLocalBloodline.Postfix(RunManager.Instance);throw new Exception("Another unfinished run overwritten");}catch(InvalidDataException){failures++;}profile.active=active;
            profile.active=null;profile.selected=new(){classId="knight"};var stagedSelection=profile.selected;
            using(var locked=new FileStream(Runtime.FamilyPath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){
                try{AdoptLocalBloodline.Postfix(RunManager.Instance);throw new Exception("Locked adoption save passed");}catch(IOException){if(profile.active!=null||profile.selected!=stagedSelection)throw new InvalidDataException("Failed adoption changed in-memory family.");failures++;}
            }
            var save=player.ToSerializable();var carrier=save.Relics.Single(r=>r.Id==ModelDb.Relic<BloodlineRelic>().Id);carrier.Props!.strings!.RemoveAll(p=>p.name==nameof(BloodlineRelic.FamilyRunSnapshot));
            try{Player.FromSerializable(save);throw new Exception("Missing snapshot silently changed heir");}catch(InvalidDataException){failures++;}
        }finally{
            state.familyId=family;state.id=id;state.ownerNetId=owner;profile.active=active;profile.selected=selected;profile.settledId=settled;profile.chronicle.settledRuns.Clear();foreach(var s in hashes)profile.chronicle.settledRuns.Add(s);
        }
        if(!before.SequenceEqual(File.ReadAllBytes(Runtime.FamilyPath))||profile.crowns!=bank)throw new InvalidDataException("Adoption challenge wrote family data.");
        return new{refusals,failures,familyUnchanged=true};
    }
}
