using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Disposable-profile bridge only. Exercises the native refinement and save carrier.
public static class OrobasOracle
{
    private static void Require(bool condition,string text) {if(!condition)throw new InvalidDataException(text);}
    public static async Task<object> NativeControl()
    {
        var player=PlayerBloodlines.LocalPlayer!;
        Require(player.Character is MegaCrit.Sts2.Core.Models.Characters.Ironclad,"Native control requires Ironclad.");
        var touch=(TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
        Require(touch.SetupForPlayer(player)&&touch.UpgradedRelic==ModelDb.Relic<BlackBlood>().Id,"Native starter refinement changed.");
        await RelicCmd.Obtain(touch,player);
        Require(player.Relics.OfType<BlackBlood>().Count()==1&&!player.Relics.OfType<BurningBlood>().Any(),"Native refinement acquisition changed.");
        Require(PlayerBloodlines.Saved(player.ToSerializable())==null,"Native player received family inheritance.");
        return new {nativeStarter="BlackBlood",unchanged=true};
    }
    public static Task<object> Check(RunState run,bool baseline,bool inspect)=>CheckPlayer(PlayerBloodlines.LocalPlayer!,baseline,inspect);
    public static async Task<object> Coop(RunState run)
    {
        var results=new List<object>();
        foreach(var player in run.Players.Where(p=>p.Character is HeirCharacter)) {
            results.Add(await CheckPlayer(player,false,false));
            var refined=player.Relics.OfType<RefinedBloodlineRelic>().Single();
            foreach(var other in run.Players.Where(p=>p!=player))Require(refined.ModifyHandDraw(other,5)==5,"Refinement affected another owner's draw.");
        }
        return results;
    }
    private static async Task<object> CheckPlayer(MegaCrit.Sts2.Core.Entities.Players.Player player,bool baseline,bool inspect)
    {
        var original=player.Relics.OfType<BloodlineRelic>().Single();
        var touch=(TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
        if(baseline) {
            Require(touch.SetupForPlayer(player),"Missing native refinement offer.");
            return new {classId=PlayerBloodlines.Heir(player).classId,upgraded=touch.UpgradedRelic!.ToString(),circlet=touch.UpgradedRelic==ModelDb.Relic<Circlet>().Id};
        }
        var snapshot=original.FamilyRunSnapshot;var selected=original.SelectedHeir;var granted=original.StartingRelicsGranted;
        var frozen=FamilySnapshot.Serialize(PlayerBloodlines.State(player)!);
        var draw=original.ModifyHandDraw(player,5);var relicCount=player.Relics.Count;
        if(!inspect) {
            Require(touch.SetupForPlayer(player),"Missing native refinement offer.");
            Require(touch.UpgradedRelic!=ModelDb.Relic<Circlet>().Id,"Heir refinement still falls back to Circlet.");
            Require(!touch.DynamicDescription.GetFormattedText().Contains('{'),"Unresolved native refinement tooltip.");
            await RelicCmd.Obtain(touch,player);
        }
        var refined=player.Relics.OfType<BloodlineRelic>().Single();
        Require(refined.GetType().Name=="RefinedBloodlineRelic","Native refinement did not replace the starter.");
        Require(refined.FamilyRunSnapshot==snapshot&&refined.SelectedHeir==selected&&refined.StartingRelicsGranted==granted,"Refinement lost frozen inheritance or starting-relic state.");
        Require(refined.ModifyHandDraw(player,5)==draw+(inspect?0:1),"Refinement draw bonus not applied exactly once.");
        Require(FamilySnapshot.Serialize(PlayerBloodlines.State(player)!)==frozen,"Refinement mutated frozen run inheritance.");
        var restored=PlayerBloodlines.Saved(player.ToSerializable());
        Require(restored!=null&&FamilySnapshot.Serialize(restored)==frozen,"Refined carrier cannot restore native saved inheritance.");
        var clone=MegaCrit.Sts2.Core.Entities.Players.Player.FromSerializable(player.ToSerializable());
        Require(PlayerBloodlines.Heir(clone).classId==PlayerBloodlines.Heir(player).classId,"Refined player lost class on deserialization.");
        Require(clone.Relics.OfType<BloodlineRelic>().Single().StartingRelicsGranted==granted,"Refined saved relic lost starting-relic guard.");
        Require(!touch.SetupForPlayer(player),"Already refined bloodline offered another empty refinement.");
        var before=player.Relics.Count;await refined.AfterActEntered();
        Require(player.Relics.Count==before,"Refinement re-granted starting relics.");
        Require(inspect||player.Relics.Count==relicCount+1,"Refinement duplicated or removed unrelated relics.");
        var bonus=refined.ModifyHandDraw(player,5);
        return new {classId=PlayerBloodlines.Heir(player).classId,refined=refined.Id.ToString(),draw=bonus,snapshotPreserved=true,saveCarrier=true,relics=player.Relics.Count};
    }
}
