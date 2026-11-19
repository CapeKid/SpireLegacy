using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace SpireLegacy;

[HarmonyPatch]
internal static class RandomCharacterCardPools
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return CardPlayStateMachine(typeof(Splash));
        yield return AccessTools.Method(typeof(Orobas), "GenerateInitialOptions");

        // These routes are present only on some supported game versions.
        var assembly = typeof(CardModel).Assembly;
        if (assembly.GetType("MegaCrit.Sts2.Core.Models.Cards.TheLovers") is { } lovers)
            yield return CardPlayStateMachine(lovers);
        if (assembly.GetType("MegaCrit.Sts2.Core.Entities.Merchant.BlitzMerchantPrismaticCardRewardEntry") is { } blitz)
            yield return AccessTools.Constructor(blitz, [typeof(Player)]);
        if (AccessTools.Method(typeof(ModifierModel), "Pick2Good1Bad", [typeof(Rng), typeof(IEnumerable<CharacterModel>)]) is { } modifiers)
            yield return modifiers;
    }

    private static MethodInfo CardPlayStateMachine(Type cardType)
    {
        var onPlay = AccessTools.Method(cardType, "OnPlay");
        var stateMachine = onPlay.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        return AccessTools.Method(stateMachine, "MoveNext");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var replacements = new Dictionary<MethodInfo, MethodInfo>
        {
            [AccessTools.PropertyGetter(typeof(UnlockState), nameof(UnlockState.CharacterCardPools))] =
                AccessTools.Method(typeof(RandomCharacterCardPools), nameof(EligiblePools)),
            [AccessTools.PropertyGetter(typeof(UnlockState), nameof(UnlockState.Characters))] =
                AccessTools.Method(typeof(RandomCharacterCardPools), nameof(EligibleUnlockedCharacters)),
            [AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters))] =
                AccessTools.Method(typeof(RandomCharacterCardPools), nameof(EligibleCharacters)),
        };

        foreach (var instruction in instructions)
        {
            if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                instruction.operand is MethodInfo original && replacements.TryGetValue(original, out var replacement))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }
            yield return instruction;
        }
    }

    private static IEnumerable<CardPoolModel> EligiblePools(UnlockState unlocks) =>
        unlocks.CharacterCardPools.Where(pool => pool is not FamilyCardPool);

    private static IEnumerable<CharacterModel> EligibleUnlockedCharacters(UnlockState unlocks) =>
        unlocks.Characters.Where(character => character is not HeirCharacter);

    private static IEnumerable<CharacterModel> EligibleCharacters() =>
        ModelDb.AllCharacters.Where(character => character is not HeirCharacter);
}
