using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace SpireLegacy;

public static class PlayerBloodlines
{
    public static readonly AsyncLocal<FamilyProfile.FamilyRun?> Creating=new();
    public static readonly AsyncLocal<Player?> EligibilityOwner=new();
    public static FamilyProfile.FamilyRun? State(Player? player)=>player?.Character is HeirCharacter heir ? heir.Bloodline : null;
    public static FamilyProfile.Heir Heir(Player? player)=>State(player)?.heir??(player==null?Runtime.Heir:new FamilyProfile.Heir());
    public static HeirInheritance Traits(Player? player)=>new(Heir(player));
    public static int Bonus(Player? player,string effect)=>State(player)?.bonuses.GetValueOrDefault(effect)??(player==null?Runtime.Bonus(effect):0);
    public static HeirCharacter Bind(CharacterModel canonical,FamilyProfile.FamilyRun snapshot)
    {
        var character=(HeirCharacter)canonical.MutableClone();
        character.Bloodline=snapshot;
        return character;
    }
    public static FamilyProfile.FamilyRun? Saved(SerializablePlayer save)
    {
        var data=save.Relics.FirstOrDefault(r=>r.Id==ModelDb.Relic<BloodlineRelic>().Id)?.Props?.strings?.FirstOrDefault(p=>p.name==nameof(BloodlineRelic.FamilyRunSnapshot)).value;
        if(string.IsNullOrEmpty(data)&&save.CharacterId==ModelDb.Character<HeirCharacter>().Id)throw new InvalidDataException("This Heir save has no frozen family snapshot. The native save is preserved; restore a compatible backup or use its original mod version.");
        return string.IsNullOrEmpty(data)?null:FamilySnapshot.ReadPlayerSave(data,save.NetId);
    }
    public static bool Eligible(Player player,string relic)
    {
        if(player.Character is not HeirCharacter)return false;
        var state=State(player);
        var milestone=Design.Rows("family_chronicle").Any(r=>r.Text("rewardKind")=="relic"&&r.Text("rewardId")==relic);
        if(milestone&&!(state?.chronicleRelics.Contains(relic)??FamilyChronicle.RelicAvailable(Runtime.Profile,relic)))return false;
        return !(Traits(player).Has("pacifist")&&(relic is "training_dummy" or "family_seal" or "guardian_knot" or "trail_compass"||relic=="triune_crest"&&Heir(player).classId=="ranger"));
    }
    public static Player? LocalPlayer=>AccessTools.Property(typeof(RunManager),"State").GetValue(RunManager.Instance) is RunState run
        ?run.Players.FirstOrDefault(p=>p.NetId==RunManager.Instance.NetService?.NetId):null;
}

[HarmonyPatch(typeof(Player),nameof(Player.CreateForNewRun),[typeof(CharacterModel),typeof(MegaCrit.Sts2.Core.Unlocks.UnlockState),typeof(ulong)])]
public static class BindNewPlayerBloodline
{
    public static void Prefix(ref CharacterModel character,ulong netId,out FamilyProfile.FamilyRun? __state)
    {
        __state=PlayerBloodlines.Creating.Value;
        if(character is not HeirCharacter)return;
        if(CoopBloodlines.CreatingMultiplayer.Value&&!CoopBloodlines.Pending.ContainsKey(netId))throw new InvalidDataException("A co-op heir began without its synchronized family snapshot.");
        var snapshot=CoopBloodlines.CreatingMultiplayer.Value&&CoopBloodlines.Pending.TryGetValue(netId,out var pending)?FamilySnapshot.Read(FamilySnapshot.Serialize(pending))
            :FamilySnapshot.Freeze(Runtime.Profile,Runtime.Heir,netId);
        PlayerBloodlines.Creating.Value=snapshot;
        character=PlayerBloodlines.Bind(character,snapshot);
    }
    public static void Finalizer(FamilyProfile.FamilyRun? __state)=>PlayerBloodlines.Creating.Value=__state;
}

[HarmonyPatch]
public static class BindSavedPlayerCharacter
{
    public static System.Reflection.MethodBase TargetMethod(){
        var available=typeof(Player).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var supported=available.Where(c=>c.GetParameters().Length>0&&c.GetParameters()[0].ParameterType==typeof(CharacterModel)).ToArray();
        if(supported.Length!=1)throw new MissingMethodException("Unsupported native Player constructor layout. Available: "+string.Join("; ",available.Select(c=>c.ToString())));
        return supported[0];
    }
    public static void Prefix(ref CharacterModel character)
    {
        if(character is HeirCharacter&&PlayerBloodlines.Creating.Value is {} saved)character=PlayerBloodlines.Bind(character,saved);
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.FromSerializable))]
public static class PrepareSavedBloodline
{
    public static void Prefix(SerializablePlayer save,out FamilyProfile.FamilyRun? __state)
    {
        __state=PlayerBloodlines.Creating.Value;
        PlayerBloodlines.Creating.Value=PlayerBloodlines.Saved(save);
    }
    public static void Finalizer(FamilyProfile.FamilyRun? __state)=>PlayerBloodlines.Creating.Value=__state;
}

[HarmonyPatch(typeof(CardModel),"set_Owner")]
public static class CaptureOwnerInheritance
{
    public static void Postfix(CardModel __instance)
    {
        if(__instance is LegacyCard card&&card.Owner!=null)card.CaptureOwnerTraits();
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.SyncWithSerializedPlayer))]
public static class SyncSavedBloodline
{
    public static void Prefix(Player __instance,SerializablePlayer player,out FamilyProfile.FamilyRun? __state)
    {
        __state=PlayerBloodlines.Creating.Value;
        if(__instance.NetId!=player.NetId)throw new InvalidDataException("Cannot synchronize another player's saved bloodline.");
        var saved=PlayerBloodlines.Saved(player);
        if(PlayerBloodlines.State(__instance) is {} prior&&saved!=null&&!FamilySnapshot.SameInheritance(prior,saved))
            throw new InvalidDataException("Reconnect cannot change this player's frozen inheritance.");
        if(__instance.Character is HeirCharacter heir&&saved!=null)heir.Bloodline=saved;
        PlayerBloodlines.Creating.Value=saved;
    }
    public static void Finalizer(FamilyProfile.FamilyRun? __state)=>PlayerBloodlines.Creating.Value=__state;
}

[HarmonyPatch]
public static class AdoptLocalBloodline
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>[
        AccessTools.Method(typeof(RunManager),"SetUpNewMultiplayer"),
        AccessTools.Method(typeof(RunManager),"InitializeSavedRun")];
    public static void Postfix(RunManager __instance)
    {
        var player=PlayerBloodlines.LocalPlayer;
        if(player?.Character is not HeirCharacter||PlayerBloodlines.State(player) is not {} run)return;
        if(((run.coop||run.ownerNetId!=0)&&run.ownerNetId!=player.NetId)||run.familyId!=Runtime.Profile.familyId) {
            ManorUi.Notice="This saved heir belongs to a family on another device. Its run effects are preserved; your local crown bank is unchanged.";return;
        }
        try {Runtime.Profile.CommitAdoption(run,player.NetId,Runtime.FamilyPath);}
        finally {
            if(Runtime.Profile.active is {} committed&&committed.id==run.id)
                ((HeirCharacter)player.Character).Bloodline=committed;
        }
    }
}

[HarmonyPatch]
public static class OwnerRelicEligibility
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>[
        AccessTools.Method(typeof(RelicGrabBag),"Populate",[typeof(Player),typeof(MegaCrit.Sts2.Core.Random.Rng)]),
        AccessTools.Method(typeof(MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory),"CreateForNormalMerchant"),
        AccessTools.Method(typeof(MegaCrit.Sts2.Core.Rewards.RelicReward),"Populate")];
    public static void Prefix(object? __instance,object[] __args,out Player? __state)
    {
        __state=PlayerBloodlines.EligibilityOwner.Value;
        var player=__args.OfType<Player>().FirstOrDefault()??(__instance is MegaCrit.Sts2.Core.Rewards.Reward reward?reward.Player:null);
        if(player!=null)PlayerBloodlines.EligibilityOwner.Value=player;
    }
    public static void Finalizer(Player? __state)=>PlayerBloodlines.EligibilityOwner.Value=__state;
}
