using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace SpireLegacy;

// Disposable family fixtures reached only through the existing opt-in lab bridge.
public static class ChronicleOracle
{
    public static object Fixture(bool unlocked)
    {
        if(Runtime.Profile.active!=null)throw new InvalidOperationException("End the isolated test run before replacing its fixture.");
        var epochs=System.Text.Json.JsonSerializer.Serialize(SaveManager.Instance.Progress.Epochs);
        var profile=new FamilyProfile{family="Chronicle Test",crowns=777,manor=new(){["smith"]=1}};
        FamilyChronicle.Reconcile(profile);
        if(unlocked)foreach(var cls in new[]{"knight","mage","ranger"}) {
            profile.selected=new(){name="Chronicler",classId=cls,traits=[]};var run=profile.Begin(21);
            profile.Settle(run.id,50,0,true);
            var count=profile.chronicle.classes[cls].returns;profile.Settle(run.id,50,999,true);
            if(profile.chronicle.classes[cls].returns!=count)throw new InvalidDataException("Duplicated Chronicle settlement.");
        }
        profile.GenerateOffers(5);profile.CopyStorage(Runtime.Profile);profile.Save(Runtime.FamilyPath);
        AccessTools.Property(typeof(Runtime),nameof(Runtime.Profile)).SetValue(null,FamilyProfile.Load(Runtime.FamilyPath));
        if(epochs!=System.Text.Json.JsonSerializer.Serialize(SaveManager.Instance.Progress.Epochs))throw new InvalidDataException("Chronicle altered native Timeline.");
        return State();
    }
    public static object State()=>new{chronicle=System.Text.Json.JsonSerializer.SerializeToElement(Runtime.Profile.chronicle,FamilyProfile.JsonOptions),Runtime.Profile.family,Runtime.Profile.crowns,
        unlockedRelics=FamilyChronicle.Relics(Runtime.Profile).ToArray(),
        availableRelics=HeirDropRelics.All.OfType<FamilyDropRelic>().Where(r=>FamilyChronicle.RelicAvailable(Runtime.Profile,r.Key)).Select(r=>r.Key).ToArray(),
        snapshot=Runtime.Profile.active?.chronicleRelics.ToArray(),
        motto=Runtime.Profile.chronicle.motto};
    public static object RunEligibility(RunState run)
    {
        var player=run.Players.Single();var bag=player.RelicGrabBag.ToSerializable().RelicIdLists.Values.SelectMany(v=>v).ToHashSet();
        foreach(var row in Design.Rows("heir_relics")) {
            var key=row.Text("id");var relic=HeirDropRelics.Get(key);
            if(bag.Contains(relic.Id)!=FamilyChronicle.RelicAvailable(Runtime.Profile,key))throw new InvalidDataException("Wrong native Chronicle bag membership: "+key);
        }
        return new{checkedRelics=10,eligible=HeirDropRelics.All.OfType<FamilyDropRelic>().Where(r=>r.IsAllowed(run)).Select(r=>r.Key).ToArray(),
            native=ModelDb.AllCharacters.Where(c=>c is not HeirCharacter).Select(c=>new{character=c.Id.ToString(),contaminated=c.RelicPool.AllRelics.Any(r=>r is FamilyDropRelic)}).ToArray()};
    }
}
