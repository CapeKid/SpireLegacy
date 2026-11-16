using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Unlocks;

namespace SpireLegacy;

[HarmonyPatch]
internal static class KaleidoscopeCardPools
{
    // The relic still uses the full roster to determine whether it can appear.
    // Only replace the pool lookup inside its asynchronous reward roll.
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method(typeof(Kaleidoscope), nameof(Kaleidoscope.AfterObtained));
        var stateMachine = method?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        return stateMachine == null ? null : AccessTools.Method(stateMachine, "MoveNext");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var original = AccessTools.PropertyGetter(typeof(UnlockState), nameof(UnlockState.CharacterCardPools));
        var replacement = AccessTools.Method(typeof(KaleidoscopeCardPools), nameof(OtherCharacterPools));
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }
            yield return instruction;
        }
    }

    private static IEnumerable<CardPoolModel> OtherCharacterPools(UnlockState state) =>
        state.CharacterCardPools.Where(pool => pool is not FamilyCardPool);
}
