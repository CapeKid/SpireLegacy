using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace SpireLegacy;

[HarmonyPatch(typeof(Player), nameof(Player.CreateForNewRun), [typeof(CharacterModel), typeof(UnlockState), typeof(ulong)])]
public static class CaptureSelectedHeir
{
    public static void Postfix(Player __result)
    {
        if (__result.Character is HeirCharacter) {
            var relic=__result.Relics.OfType<BloodlineRelic>().Single();
            relic.SelectedHeir=JsonSerializer.Serialize(PlayerBloodlines.Heir(__result),FamilyProfile.JsonOptions);
            relic.FamilyRunSnapshot=FamilySnapshot.Serialize(PlayerBloodlines.State(__result)!);
        }
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
public static class InheritedHealing
{
    internal static readonly AsyncLocal<Creature?> StartingHp = new();
    public static void Prefix(Creature creature, ref decimal amount)
    {
        if (creature.Player?.Character is HeirCharacter && StartingHp.Value != creature) amount = PlayerBloodlines.Traits(creature.Player).Healing(amount);
    }
}

// Neow sets HP to zero before restoring the run's starting HP through Heal.
// Scope the exception to that async initialization, including its continuations;
// ordinary healing, later Ancients, and dead combat creatures remain unchanged.
[HarmonyPatch(typeof(AncientEventModel), "BeforeEventStarted")]
public static class HeirStartingHp
{
    public static void Prefix(AncientEventModel __instance, bool isPreFinished, out Creature? __state)
    {
        __state = InheritedHealing.StartingHp.Value;
        if (!isPreFinished && __instance is MegaCrit.Sts2.Core.Models.Events.Neow && __instance.Owner?.Character is HeirCharacter)
            InheritedHealing.StartingHp.Value = __instance.Owner.Creature;
    }
    public static void Finalizer(Creature? __state) => InheritedHealing.StartingHp.Value = __state;
}

// Native co-op revives defeated players to one HP before tearing down combat.
// Hero Complex suppresses ordinary healing, not this required initialization.
[HarmonyPatch(typeof(Player),nameof(Player.ReviveBeforeCombatEnd))]
public static class HeirCoopRevival
{
    public static void Prefix(Player __instance,out Creature? __state){__state=InheritedHealing.StartingHp.Value;InheritedHealing.StartingHp.Value=__instance.Creature;}
    public static void Finalizer(Creature? __state)=>InheritedHealing.StartingHp.Value=__state;
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewSingleplayer))]
public static class BeginBloodlineRun
{
    public static void Postfix(RunState state)
    {
        if (!state.Players.Any(p => p.Character is HeirCharacter)) return;
        if (Runtime.Profile.active is {} previous) Runtime.Profile.Settle(previous.id, previous.floors, 0, false);
        // Keep the character chosen for this new run rather than replacing it with the new offers.
        var relic = state.Players.First(p => p.Character is HeirCharacter).Relics.OfType<BloodlineRelic>().Single();
        if (relic.SelectedHeir.Length > 0) Runtime.Profile.selected = JsonSerializer.Deserialize<FamilyProfile.Heir>(relic.SelectedHeir,FamilyProfile.JsonOptions);
        var player=state.Players.First(p=>p.Character is HeirCharacter);
        var run=PlayerBloodlines.State(player)!;
        Runtime.Profile.active=run;Runtime.Profile.selected=null;
        relic.FamilyRunSnapshot=FamilySnapshot.Serialize(run);
        Runtime.Profile.Save(Runtime.FamilyPath);
        if(run.quick)state.ExtraFields.StartedWithNeow=false;
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen), "BeginRun")]
public static class RequireSynchronizedRun
{
    public static bool Prefix(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen __instance)
    {
        var button = (MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectButton?)AccessTools.Field(__instance.GetType(),"_selectedButton").GetValue(__instance);
        if (button?.Character is not HeirCharacter) return true;
        if (__instance.Lobby.NetService.Type != MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType.Singleplayer)
        {
            if(!CoopBloodlines.Complete(__instance.Lobby)) {
                ManorUi.Notice="Waiting for the other players' heir selections to synchronize.";
                ManorUi.Open();return false;
            }
        }
        ManorUi.Notice=null;
        return true;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.FromSerializable))]
public static class ResumeBloodlineRun
{
    public static void Postfix(Player __result)
    {
        if (__result.Character is not HeirCharacter) return;
        var snapshot=PlayerBloodlines.State(__result);
        if(snapshot==null)throw new InvalidDataException("This heir save is missing its family snapshot.");
        // Loading a remote/preview player must never write the local family file.
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
public static class SettleBloodlineRun
{
    public static void Postfix(RunManager __instance, bool isVictory)
    {
        var state = (RunState?)AccessTools.Property(typeof(RunManager), "State").GetValue(__instance);
        var player = state?.Players.FirstOrDefault(p => p.Character is HeirCharacter && p.NetId==__instance.NetService.NetId);
        if (player == null || PlayerBloodlines.State(player) is not {} run || !FamilySnapshot.CanSettle(Runtime.Profile,run,__instance.NetService.NetId)) return;
        Runtime.Profile.Settle(run.id,state!.TotalFloor,player.Gold,isVictory);
        Runtime.Profile.Save(Runtime.FamilyPath);
    }
}
