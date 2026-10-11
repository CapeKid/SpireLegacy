using System.Reflection;
using System.Runtime.ExceptionServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

public static class BranchCommands
{
    // Beta adds CardPlay to card-sourced damage. Resolve once against the running game.
    private static readonly MethodInfo CardDamage = ApiContract.Require(typeof(CreatureCmd), "Damage",typeof(Task<IEnumerable<DamageResult>>),
        [typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)],
        [typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)]);

    // Beta returns Task<CardPileAddResult?> instead of Task; await either as Task.
    private static readonly MethodInfo ExhaustMethod = ApiContract.Require(typeof(CardCmd), "Exhaust",typeof(Task),
        [typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool)]);
    private static readonly MethodInfo LoseBlockMethod=ApiContract.Require(typeof(CreatureCmd),"LoseBlock",typeof(Task),
        [typeof(PlayerChoiceContext),typeof(Creature),typeof(decimal),typeof(Creature)], [typeof(Creature),typeof(decimal)]);
    public static void Validate(){_ = CardDamage;_ = ExhaustMethod;_ = LoseBlockMethod;}
    public static Task LoseBlock(PlayerChoiceContext context,Creature target,decimal amount,Creature remover)=>(Task)ApiContract.Invoke(LoseBlockMethod,null,LoseBlockMethod.GetParameters().Length==2?[target,amount]:[context,target,amount,remover])!;
    public static Task Exhaust(PlayerChoiceContext context, CardModel card)
    {
        try { return (Task)ExhaustMethod.Invoke(null, [context, card, false, false])!; }
        catch (TargetInvocationException error) when (error.InnerException != null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }

    public static Task<IEnumerable<DamageResult>> DamageFromCard(PlayerChoiceContext context, Creature target,
        decimal amount, ValueProp props, Creature dealer, CardModel card, CardPlay play)
    {
        object?[] args = CardDamage.GetParameters().Length == 7
            ? [context, target, amount, props, dealer, card, play]
            : [context, target, amount, props, dealer, card];
        try { return (Task<IEnumerable<DamageResult>>)CardDamage.Invoke(null, args)!; }
        catch (TargetInvocationException error) when (error.InnerException != null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
}

// The beta adds CardPlay to damage hooks too. Dispatch our unchanged modifiers
// from the actual runtime base hooks, so one assembly preserves them on both branches.
[HarmonyPatch]
public static class InheritedDamageHooks
{
    public static IEnumerable<MethodBase> TargetMethods() => new[]{"ModifyDamageAdditive","ModifyDamageMultiplicative"}.Select(name=>ApiContract.Require(typeof(AbstractModel),name,typeof(decimal),
        [typeof(Creature),typeof(decimal),typeof(ValueProp),typeof(Creature),typeof(CardModel),typeof(CardPlay)],
        [typeof(Creature),typeof(decimal),typeof(ValueProp),typeof(Creature),typeof(CardModel)]));

    public static bool Prefix(AbstractModel __instance, MethodBase __originalMethod, object?[] __args, ref decimal __result)
    {
        if (__originalMethod.Name == "ModifyDamageMultiplicative")
        {
            if (__instance is not BloodlineRelic relic) return true;
            __result = relic.InheritedDamageMultiplicative((Creature?)__args[0], (decimal)__args[1]!, (ValueProp)__args[2]!, (Creature?)__args[3], (CardModel?)__args[4]);
            return false;
        }
        if (__instance is not (LegacyCard or LegacyPower or BloodlineRelic)) return true;
        var target=(Creature?)__args[0]; var amount=(decimal)__args[1]!; var props=(ValueProp)__args[2]!;
        var dealer=(Creature?)__args[3]; var card=(CardModel?)__args[4];
        __result = __instance switch
        {
            LegacyCard legacy => legacy.InheritedDamageAdditive(target,amount,props,dealer,card),
            LegacyPower power => power.InheritedDamageAdditive(target,amount,props,dealer,card),
            BloodlineRelic bloodline => bloodline.InheritedDamageAdditive(target,amount,props,dealer,card),
            _ => 0
        };
        return false;
    }
}
