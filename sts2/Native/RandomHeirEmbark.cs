using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace SpireLegacy;

[HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
internal static class RandomHeirEmbark
{
    internal sealed record Roll(string? LobbySeed, string? DebugSeed, string Seed, bool IsHeir)
    {
        internal bool IsCurrent(StartRunLobby lobby) =>
            LobbySeed == lobby.Seed && DebugSeed == NGame.Instance?.DebugSeedOverride;
    }

    private static readonly ConditionalWeakTable<StartRunLobby, Roll> Rolls = new();
    private static readonly Func<StartRunLobby, UnlockState> UnlockState =
        AccessTools.MethodDelegate<Func<StartRunLobby, UnlockState>>(AccessTools.Method(typeof(StartRunLobby), "GetUnlockState"));

    internal static Roll Preview(StartRunLobby lobby)
    {
        if (Rolls.TryGetValue(lobby, out var roll) && roll.IsCurrent(lobby)) return roll;

        var lobbySeed = lobby.Seed;
        var debugSeed = NGame.Instance?.DebugSeedOverride;
        var seed = debugSeed ?? (lobbySeed is null ? SeedHelper.GetRandomSeed(null, 12) : SeedHelper.CanonicalizeSeed(lobbySeed));
        roll = new Roll(lobbySeed, debugSeed, seed, Resolve(lobby, seed) is HeirCharacter);
        Rolls.Remove(lobby);
        Rolls.Add(lobby, roll);
        return roll;
    }

    internal static CharacterModel Resolve(StartRunLobby lobby, string seed)
    {
        // BeginRunLocally uses the same RNG for selecting Acts, then the Random character.
        var rng = new Rng(StringHelper.GetDeterministicHashCode(seed), "act_selection");
        _ = ActModel.GetRandomList(rng, UnlockState(lobby), false).ToList();
        return rng.NextItem(ModelDb.AllCharacters) ?? throw new InvalidOperationException("Random has no characters to choose from.");
    }

    private static void Prefix(StartRunLobby __instance, ref string seed)
    {
        if (!Rolls.TryGetValue(__instance, out var roll)) return;
        Rolls.Remove(__instance);
        if (__instance.NetService.Type != NetGameType.Singleplayer ||
            CoopBloodlines.Character(CoopBloodlines.Local(__instance)) is not RandomCharacter || !roll.IsCurrent(__instance))
            return;
        seed = roll.Seed;
        if (roll.IsHeir && __instance.LobbyListener is Control screen &&
            AccessTools.Field(screen.GetType(), "_bgContainer")?.GetValue(screen) is Control background)
            HeirSelectionBackground.Apply(background);
    }
}
