using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
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
        if (creature.Player?.Character is not HeirCharacter) return;
        if (StartingHp.Value == creature)
        {
            // Weary Traveler gives Neow 0.8 HP to restore on a Fragile start.
            // Native healing truncates fractional HP, so restore at least one.
            if (creature.IsDead && creature.MaxHp == 1)
                amount = Math.Max(1m, amount);
            return;
        }
        amount = PlayerBloodlines.Traits(creature.Player).Healing(amount);
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
        if (__instance.Owner is not { Character: HeirCharacter } player) return;
        var creature = player.Creature;
        if (!isPreFinished)
        {
            if (__instance is Neow) InheritedHealing.StartingHp.Value = creature;
        }
        else if (creature.IsDead && creature.MaxHp == 1 && PlayerBloodlines.Traits(player).Has("fragile") &&
            player.Relics.Any(relic => IsAffectedReward(__instance, relic, player.RunState.TotalFloor)))
        {
            // Older saves can record the finished blessing after its cost killed the heir.
            creature.SetCurrentHpInternal(1);
        }
    }
    public static void Finalizer(Creature? __state) => InheritedHealing.StartingHp.Value = __state;

    private static bool IsAffectedReward(AncientEventModel ancient, RelicModel relic, int floor) => ancient switch
    {
        Neow => relic is LeafyPoultice or PrecariousShears,
        Vakuu => relic.FloorAddedToDeck == floor && relic.Id.Entry is "DISTINGUISHED_CAPE" or "SERE_TALON",
        _ => false,
    };
}

// Neow and Neow's Bones both use this eligibility check before offering a relic.
[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.IsAllowedAtNeow))]
public static class HeirNeowHealthCosts
{
    public static void Postfix(RelicModel __instance, Player player, ref bool __result)
    {
        if (!__result || player.Character is not HeirCharacter) return;
        __result = __instance switch
        {
            LeafyPoultice => player.Creature.MaxHp > __instance.DynamicVars.MaxHp.BaseValue,
            PrecariousShears => player.Creature.CurrentHp > __instance.DynamicVars.Damage.BaseValue,
            _ => true,
        };
    }
}

[HarmonyPatch(typeof(PrecariousShears), nameof(PrecariousShears.AfterObtained))]
internal static class FragileShearsPayment
{
    internal static readonly AsyncLocal<Creature?> Owner = new();

    private static void Prefix(PrecariousShears __instance, out Creature? __state)
    {
        __state = Owner.Value;
        var player = __instance.Owner;
        Owner.Value = player.Character is HeirCharacter && PlayerBloodlines.Traits(player).Has("fragile") ? player.Creature : null;
    }

    private static void Finalizer(Creature? __state) => Owner.Value = __state;
}

[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
internal static class LimitFragileShearsDamage
{
    private static void Prefix(Creature __instance, ref decimal amount)
    {
        if (amount > 0 && __instance.IsAlive && ReferenceEquals(FragileShearsPayment.Owner.Value, __instance))
            amount = Math.Min(amount, __instance.CurrentHp - 1);
    }
}

// Ancient callbacks can finish after the defeat handler deletes the run save.
[HarmonyPatch(typeof(EventRoom), "OnEventStateChanged")]
internal static class SkipDefeatedAncientSave
{
    private static bool Prefix(EventModel eventModel) => eventModel is not AncientEventModel ||
        eventModel.Owner == null || eventModel.Owner.RunState.Players.Any(player => player.Creature.IsAlive);
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.LoseMaxHp))]
internal static class FragileMaxHpLoss
{
    private static bool Prefix(Creature creature, ref decimal amount, ref Task __result)
    {
        if (amount <= 0 || creature.Player is not { Character: HeirCharacter } player ||
            !PlayerBloodlines.Traits(player).Has("fragile")) return true;

        // Native LoseMaxHp applies damage before enforcing its minimum of 1 max HP.
        amount = Math.Min(amount, Math.Max(0, creature.MaxHp - 1));
        if (amount > 0) return true;
        __result = Task.CompletedTask;
        return false;
    }
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
        var player=state.Players.First(p=>p.Character is HeirCharacter);
        var relic=player.Relics.OfType<BloodlineRelic>().Single();
        var run=PlayerBloodlines.State(player)!;
        try {
            if(!Runtime.Profile.CommitRunStart(run,Runtime.FamilyPath))throw new InvalidDataException("Cannot begin an already settled or foreign family climb.");
        } finally {
            // A failure after the durable write still committed the embarkation.
            if(Runtime.Profile.active is {} committed&&committed.id==run.id) {
                run=committed;((HeirCharacter)player.Character).Bloodline=run;
                relic.FamilyRunSnapshot=FamilySnapshot.Serialize(run);
            }
        }
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
    // Commit the crown bank before native OnEnded can delete the resumable run.
    public static void Prefix(RunManager __instance, bool isVictory)
    {
        var state = (RunState?)AccessTools.Property(typeof(RunManager), "State").GetValue(__instance);
        var player = state?.Players.FirstOrDefault(p => p.Character is HeirCharacter && p.NetId==__instance.NetService.NetId);
        if (player == null || PlayerBloodlines.State(player) is not {} run || !FamilySnapshot.CanSettle(Runtime.Profile,run,__instance.NetService.NetId)) return;
        var earned=CrownAccounting.Earned(state!,player);
        Runtime.Profile.CommitSettlement(run,__instance.NetService.NetId,earned.floors,earned.gold,isVictory,Runtime.FamilyPath,state!.TotalFloor);
    }
}
