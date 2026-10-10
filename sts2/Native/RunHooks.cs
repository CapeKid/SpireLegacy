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
        if (__result.Character is HeirCharacter) __result.Relics.OfType<BloodlineRelic>().Single().SelectedHeir = JsonSerializer.Serialize(Runtime.Heir,FamilyProfile.JsonOptions);
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
public static class InheritedHealing
{
    internal static readonly AsyncLocal<Creature?> StartingHp = new();
    public static void Prefix(Creature creature, ref decimal amount)
    {
        if (creature.Player?.Character is HeirCharacter && StartingHp.Value != creature) amount = Inheritance.Healing(amount);
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
        var run = Runtime.Profile.Begin(System.Environment.TickCount);
        relic.FamilyRunSnapshot = JsonSerializer.Serialize(run,FamilyProfile.JsonOptions);
        Runtime.Profile.Save(Runtime.FamilyPath);
        if (state.Players.Any(p => p.Character is HeirCharacter) && Runtime.Profile.active?.quick == true) state.ExtraFields.StartedWithNeow = false;
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen), "BeginRun")]
public static class RequireOwnedContent
{
    public static bool Prefix(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen __instance)
    {
        var button = (MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectButton?)AccessTools.Field(__instance.GetType(),"_selectedButton").GetValue(__instance);
        if (button?.Character is not HeirCharacter) return true;
        if (__instance.Lobby.NetService.Type != MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType.Singleplayer)
        {
            ManorUi.Notice="Spire Legacy is a solo character. Start a singleplayer climb to use your family and manor.";
            ManorUi.Open(); return false;
        }
        ManorUi.Notice=null;
        if(ContentBootstrap.Ready) return true;
        ManorUi.Open(); return false;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.FromSerializable))]
public static class ResumeBloodlineRun
{
    public static void Postfix(Player __result)
    {
        if (__result.Character is not HeirCharacter) return;
        var snapshot = __result.Relics.OfType<BloodlineRelic>().Single().FamilyRunSnapshot;
        if (snapshot.Length == 0) throw new InvalidDataException("This heir save is missing its family snapshot.");
        var run = JsonSerializer.Deserialize<FamilyProfile.FamilyRun>(snapshot,FamilyProfile.JsonOptions)!;
        if (run.id == Runtime.Profile.settledId) throw new InvalidDataException("This heir has already completed its climb.");
        Runtime.Profile.active = run;
        Runtime.Profile.Save(Runtime.FamilyPath);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
public static class SettleBloodlineRun
{
    public static void Postfix(RunManager __instance, bool isVictory)
    {
        var state = (RunState?)AccessTools.Property(typeof(RunManager), "State").GetValue(__instance);
        var player = state?.Players.FirstOrDefault(p => p.Character is HeirCharacter);
        if (player == null || Runtime.Profile.active is not {} run) return;
        Runtime.Profile.Settle(run.id,state!.TotalFloor,player.Gold,isVictory);
        Runtime.Profile.Save(Runtime.FamilyPath);
    }
}
