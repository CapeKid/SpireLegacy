using System.Text.Json;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace SpireLegacy;

// Only reachable through the validated, disposable-profile native bridge.
public static class LifecycleOracle
{
    private static string State()=>JsonSerializer.Serialize(Runtime.Profile,FamilyProfile.JsonOptions);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
    public static object Inspect()=>new {
        profile=JsonSerializer.SerializeToElement(Runtime.Profile,FamilyProfile.JsonOptions),
        persisted=JsonSerializer.SerializeToElement(FamilyProfile.Load(Runtime.FamilyPath),FamilyProfile.JsonOptions),
        snapshot=PlayerBloodlines.State(PlayerBloodlines.LocalPlayer) is {} frozen?JsonSerializer.SerializeToElement(frozen,FamilyProfile.JsonOptions):(JsonElement?)null,
        nativeRuns=SaveManager.Instance.Progress.NumberOfRuns,
        nativeSave=SaveManager.Instance.LoadRunSave().Success
    };
    public static object Completion(RunState state,bool victory,string fault)
    {
        var player=PlayerBloodlines.LocalPlayer!;var profile=Runtime.Profile;
        var run=PlayerBloodlines.State(player)!;var id=run.id;
        var before=State();var bank=profile.crowns;var generation=profile.generation;
        var nativeRuns=SaveManager.Instance.Progress.NumberOfRuns;
        var save=SaveManager.Instance.LoadRunSave();Require(save.Success,"Fixture must begin with a real native save.");
        var oldNative=JsonSerializer.Serialize(save.SaveData!,JsonSerializationUtility.GetTypeInfo<SerializableRun>());
        var percent=Math.Clamp(100+run.heir.traits.Sum(t=>Design.Get("traits",t).Number("goldBonus")),0,200);
        var expected=(int)(((long)state.TotalFloor*2+player.Gold/25+(victory?20:0))*percent*(100+run.bonuses.GetValueOrDefault("gold"))/10000);
        if(fault!="none") {
            FamilyStore.Fault.Value=s=>{if(s==fault)throw new IOException("Native lifecycle interruption: "+s);};
            try {RunManager.Instance.OnEnded(victory);throw new InvalidDataException("Native fault was not reached.");}
            catch(IOException) { }
            finally {FamilyStore.Fault.Value=null;}
            Require(SaveManager.Instance.Progress.NumberOfRuns==nativeRuns,"Failed family write ran native end-of-run history.");
            var retained=SaveManager.Instance.LoadRunSave();
            Require(retained.Success&&JsonSerializer.Serialize(retained.SaveData!,JsonSerializationUtility.GetTypeInfo<SerializableRun>())==oldNative,"Interrupted completion deleted or rewrote the native resumable save.");
            Require(State()==JsonSerializer.Serialize(FamilyProfile.Load(Runtime.FamilyPath),FamilyProfile.JsonOptions),"Live/durable family disagree after interruption.");
            Require(fault=="after-commit"?profile.generation==generation+1:State()==before,"Incorrect family commit boundary.");
        }
        RunManager.Instance.OnEnded(victory);
        Require(profile.generation==generation+1&&profile.crowns==bank+expected&&profile.active==null,"Wrong completion payout/generation.");
        Require(profile.chronicle.settledRuns.Contains(id)&&profile.settledId==id,"Missing settlement receipt.");
        var settled=State();RunManager.Instance.OnEnded(victory);
        Require(State()==settled&&State()==JsonSerializer.Serialize(FamilyProfile.Load(Runtime.FamilyPath),FamilyProfile.JsonOptions),"Repeated callback changed completed family.");
        Require(profile.CommitSettlement(run,player.NetId,50,999,true,Runtime.FamilyPath)==0,"Older snapshot received another payout.");
        Require(SaveManager.Instance.Progress.NumberOfRuns==nativeRuns+1&&!SaveManager.Instance.LoadRunSave().Success,"Native completion history/deletion did not occur exactly once.");
        return new {id,heir=run.heir.classId,victory,fault,expected,bankBefore=bank,bankAfter=profile.crowns,generationBefore=generation,generationAfter=profile.generation,floors=state.TotalFloor,gold=player.Gold,repeatStable=true,nativeSaveRetainedDuringFailure=fault!="none",nativeHistoryExactlyOnce=true};
    }
    public static object OwnerIsolation(RunState state)
    {
        var local=PlayerBloodlines.LocalPlayer!;var profile=Runtime.Profile;var before=State();
        var checkedOwners=0;
        foreach(var player in state.Players.Where(p=>p!=local&&p.Character is HeirCharacter)) {
            var remote=PlayerBloodlines.State(player)!;
            Require(profile.CommitSettlement(remote,local.NetId,50,999,true,Runtime.FamilyPath)==0,"Remote player credited local crown bank.");
            Require(profile.CommitSettlement(remote,remote.ownerNetId,50,999,true,Runtime.FamilyPath)==0,"Remote family credited local crown bank.");
            checkedOwners++;
        }
        Require(checkedOwners>0&&State()==before,"Owner isolation fixture did not preserve the local family.");
        return new{checkedOwners,bank=profile.crowns,generation=profile.generation,active=profile.active!.id};
    }
}
