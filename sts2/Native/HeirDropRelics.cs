using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace SpireLegacy;

public static class HeirDropRelics
{
    private static readonly Dictionary<string,Type> types=typeof(HeirDropRelics).Assembly.GetTypes()
        .Where(t=>!t.IsAbstract&&typeof(FamilyDropRelic).IsAssignableFrom(t)).ToDictionary(t=>t.Name[2..]);
    public static RelicModel Get(string key)=>ModelDb.GetById<RelicModel>(ModelDb.GetId(types[key]));
    public static IEnumerable<RelicModel> All=>Design.Rows("heir_relics").Select(r=>Get(r.Text("id")));
}

/// <summary>Bounded, owner-scoped family synergies. Claims precede asynchronous effects.</summary>
public abstract class FamilyDropRelic : CustomRelicModel
{
    private HeirInheritance Inheritance=>PlayerBloodlines.Traits(Owner);
    protected FamilyDropRelic():base(false) { }
    public string Key=>GetType().Name[2..];
    public Design.Row Row=>Design.Get("heir_relics",Key);
    private string TexturePath(string field)
    {
        var path="res://SpireLegacy/"+Design.Get("relic_art",Key).Text(field);
        var texture=Runtime.Texture(Design.Get("relic_art",Key).Text(field));
        if(texture.ResourcePath!=path)texture.TakeOverPath(path);
        return path;
    }
    // Native UI code may inline texture getters. Virtual paths point to cached,
    // mod-owned textures so those call sites use the same original artwork.
    public override string PackedIconPath=>TexturePath("path");
    protected override string PackedIconOutlinePath=>TexturePath("outline");
    protected override string BigIconPath=>TexturePath("path");
    public override RelicRarity Rarity=>Enum.Parse<RelicRarity>(Row.Text("rarity"));
    public override List<(string,string)> Localization=>HeirLocalization.Entries(("title",Row.Text("title")),("description",Row.Text("description")),("flavor",Row.Text("flavor")));
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if(Key is "heirloom_brooch" or "wayfarer_ribbon" or "triune_crest" or "guardian_knot")yield return HoverTipFactory.Static(StaticHoverTip.Block);
            if(Key=="training_dummy")yield return HoverTipFactory.FromPower<VigorPower>();
            if(Key is "triune_crest" or "trail_compass")yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
    }
    internal bool IsAllowedForFamily(IRunState runState)=>PlayerBloodlines.EligibilityOwner.Value is {} player
        ?PlayerBloodlines.Eligible(player,Key):runState.Players.Any(p=>PlayerBloodlines.Eligible(p,Key));
    [SavedProperty] public bool CombatClaimed { get; set; }
    [SavedProperty] public int TriggerTurn { get; set; }=-1;
    [SavedProperty] public int TypeMask { get; set; }
    [SavedProperty] public int MaskTurn { get; set; }=-1; // Historical save field.
    [SavedProperty] public bool HourglassPending { get; set; }
    [SavedProperty] public bool SpentVisual {get=>Status==RelicStatus.Disabled;set=>Status=value?RelicStatus.Disabled:RelicStatus.Normal;}
    public override bool ShowCounter=>CombatManager.Instance.IsInProgress;
    private const int AllCardTypes = 7;
    public override int DisplayAmount=>CombatClaimed ? 0 : Key=="family_seal"
        ? System.Numerics.BitOperations.PopCount((uint)TypeMask)
        : IsMutable&&TriggerTurn==Owner?.PlayerCombatState?.TurnNumber ? 0 : 1;
    private void Display(bool spent) {Status=spent?RelicStatus.Disabled:RelicStatus.Normal;InvokeDisplayAmountChanged();}
    private bool ClaimTurn()
    {
        var turn=Owner.PlayerCombatState!.TurnNumber;
        if(TriggerTurn==turn)return false;
        TriggerTurn=turn;Display(true);Flash();return true;
    }
    protected bool ClaimCombat()
    {
        if(CombatClaimed)return false;
        CombatClaimed=true;Display(true);Flash();return true;
    }
    private Task Block(int amount)=>CreatureCmd.GainBlock(Owner.Creature,amount,ValueProp.Unpowered,null);
    public override Task BeforeCombatStart()
    {
        CombatClaimed=false;HourglassPending=false;TriggerTurn=MaskTurn=-1;TypeMask=0;Display(false);return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room) {Status=RelicStatus.Normal;return Task.CompletedTask;}
    public override Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Key == "legacy_hourglass" && side == Owner.Creature.Side && Owner.Creature.IsAlive && participants.Contains(Owner.Creature) &&
            Owner.PlayerCombatState!.Energy >= Row.Number("threshold") && ClaimCombat())
            HourglassPending = true;
        return Task.CompletedTask;
    }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context,Player player)
    {
        if(player!=Owner)return;
        Display(CombatClaimed);
        if(Key=="legacy_hourglass"&&HourglassPending)
        {
            HourglassPending=false;
            await PlayerCmd.GainEnergy(Row.Number("amount"),Owner);
            await CardPileCmd.Draw(context,Row.Number("extra"),Owner);
        }
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context,CardPlay play)
    {
        if(play.Card.Owner!=Owner||!CombatManager.Instance.IsInProgress||!Owner.Creature.IsAlive)return;
        switch(Key)
        {
            case "heirloom_brooch" when play.Card.Type==CardType.Power:
                if(ClaimCombat())await Block(Row.Number("amount"));break;
            case "training_dummy" when play.Card.Type==CardType.Attack&&Owner.Creature.Block>0:
                if(ClaimTurn())await CardEffects.Apply(context,"vigor",Owner.Creature,Row.Number("amount"),Owner.Creature,null);break;
            case "family_seal" when !CombatClaimed:
                TypeMask|=play.Card.Type switch {CardType.Attack=>1,CardType.Skill=>2,CardType.Power=>4,_=>0};
                InvokeDisplayAmountChanged();
                break;
        }
    }
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int count) =>
        Key == "family_seal" && !CombatClaimed && TypeMask == AllCardTypes &&
        card.Owner == Owner && card.Type is CardType.Attack or CardType.Skill ? count + Row.Number("amount") : count;
    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (Key == "family_seal") ClaimCombat();
        return Task.CompletedTask;
    }
    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (Key == "scholar_bookmark" && card.Owner == Owner && card.Type == CardType.Power &&
            !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.All) > 0 && ClaimCombat())
            card.EnergyCost.AddThisTurn(-Row.Number("amount"));
        return Task.CompletedTask;
    }
    public override Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (Key == "guardian_knot" && target == Owner.Creature && dealer != null && dealer.Side != target.Side &&
            props.IsPoweredAttack() && result.WasFullyBlocked && result.BlockedDamage > 0 && ClaimCombat())
            return CardEffects.Apply(context,"nextEnergy",Owner.Creature,Row.Number("amount"),Owner.Creature,null);

        if (Key is not ("trail_compass" or "triune_crest") || dealer != Owner.Creature ||
            target.Side == dealer.Side || !props.IsPoweredAttack() ||
            cardSource?.Type != CardType.Attack || !Owner.Creature.IsAlive || result.BlockedDamage + result.UnblockedDamage <= 0)
            return Task.CompletedTask;
        if (Key == "trail_compass" && target.GetPowerAmount<VulnerablePower>() > 0 && ClaimCombat())
            return CardPileCmd.Draw(context,Row.Number("amount"),Owner);
        if (Key == "triune_crest" && PlayerBloodlines.Heir(Owner).classId == "knight" &&
            Owner.Creature.GetPower<P_class>() is { Charges: > 0 } && ClaimCombat())
            return Block(Row.Number("amount"));
        return Task.CompletedTask;
    }
    public override async Task AfterCardExhausted(PlayerChoiceContext context,CardModel card,bool causedByEthereal)
    {
        if(Key=="threadbound_ledger"&&card.Owner==Owner&&CombatManager.Instance.IsInProgress&&Owner.Creature.IsAlive&&Owner.Creature.CombatState?.CurrentSide==Owner.Creature.Side
            &&Owner.PlayerCombatState?.Phase!=PlayerTurnPhase.End&&ClaimTurn())await CardPileCmd.Draw(context,Row.Number("amount"),Owner);
    }
    public override async Task AfterCardDiscarded(PlayerChoiceContext context,CardModel card)
    {
        if(Key=="wayfarer_ribbon"&&card.Owner==Owner&&CombatManager.Instance.IsInProgress&&Owner.Creature.IsAlive&&Owner.Creature.CombatState?.CurrentSide==Owner.Creature.Side
            &&Owner.PlayerCombatState?.Phase!=PlayerTurnPhase.End&&ClaimTurn())await Block(Row.Number("amount"));
    }
}
public sealed class R_heirloom_brooch : FamilyDropRelic { }
public sealed class R_training_dummy : FamilyDropRelic { }
public sealed class R_threadbound_ledger : FamilyDropRelic { }
public sealed class R_wayfarer_ribbon : FamilyDropRelic { }
public sealed class R_triune_crest : FamilyDropRelic
{
    internal Task OnArcaneFlow(int charges) =>
        charges >= 2 && ClaimCombat() ? PlayerCmd.GainEnergy(Row.Number("extra"),Owner) : Task.CompletedTask;
    internal int ExtraRhythmVulnerable() => ClaimCombat() ? Row.Number("threshold") : 0;
}
public sealed class R_legacy_hourglass : FamilyDropRelic { }
public sealed class R_family_seal : FamilyDropRelic { }
public sealed class R_guardian_knot : FamilyDropRelic { }
public sealed class R_scholar_bookmark : FamilyDropRelic { }
public sealed class R_trail_compass : FamilyDropRelic { }

// Load distributed SVGs directly, as with the original trait icons. Only our drop
// relics are affected; native relics and the existing Bloodline icon retain their hooks.
[HarmonyPatch]
public static class HeirDropRelicArt
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{"get_Icon","get_IconOutline","get_BigIcon"}.Select(name=>AccessTools.Method(typeof(RelicModel),name));
    public static bool Prefix(RelicModel __instance,System.Reflection.MethodBase __originalMethod,ref Texture2D __result)
    {
        if(__instance is not FamilyDropRelic relic)return true;
        __result=Runtime.Texture(Design.Get("relic_art",relic.Key).Text(__originalMethod.Name=="get_IconOutline"?"outline":"path"));return false;
    }
}
