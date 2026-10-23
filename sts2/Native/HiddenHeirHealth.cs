using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace SpireLegacy;

// Health is still simulated and saved normally. Mask only the affected owner's
// displays, including NHealthBars used by multiplayer side panels. Block is a
// separate native control and must remain readable.
public static class HiddenHeirHealth
{
    public static bool Applies(Player? player) => player?.Character is HeirCharacter
        && PlayerBloodlines.Traits(player).Has("hidden_health");

    public static void Mask(NHealthBar bar, Player? player)
    {
        if (!Applies(player)) return;
        bar.HpBarContainer.Hide();
        bar.GetNode<Control>("%HpLabel").Hide();
        bar.GetNode<Control>("%InfinityTex").Hide();
    }
}

[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.SetCreature))]
public static class HiddenHeirHealthSetup
{
    public static void Postfix(NHealthBar __instance, MegaCrit.Sts2.Core.Entities.Creatures.Creature creature)
        => HiddenHeirHealth.Mask(__instance, creature.Player);
}

[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.RefreshValues))]
public static class HiddenHeirHealthRefresh
{
    public static void Postfix(NHealthBar __instance, MegaCrit.Sts2.Core.Entities.Creatures.Creature ____creature)
        => HiddenHeirHealth.Mask(__instance, ____creature.Player);
}

[HarmonyPatch(typeof(NTopBarHp), nameof(NTopBarHp.Initialize))]
public static class HiddenHeirTopBarHealth
{
    // Initialize also runs when the top bar is rebound to another run/player.
    public static void Postfix(NTopBarHp __instance, Player player)
        => __instance.Visible = !HiddenHeirHealth.Applies(player);
}
