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
    public override List<(string,string)> Localization=>[("title",Row.Text("title")),("description",Row.Text("description")),("flavor",Row.Text("flavor"))];
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if(Key is "heirloom_brooch" or "wayfarer_ribbon" or "triune_crest" or "family_seal" or "guardian_knot")yield return HoverTipFactory.Static(StaticHoverTip.Block);
            if(Key is "training_dummy" or "triune_crest" or "trail_compass")yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }
    public override bool IsAllowed(IRunState runState)=>PlayerBloodlines.EligibilityOwner.Value is {} player
        ?PlayerBloodlines.Eligible(player,Key):runState.Players.Any(p=>PlayerBloodlines.Eligible(p,Key));
    [SavedProperty] public bool CombatClaimed { get; set; }
    [SavedProperty] public int TriggerTurn { get; set; }=-1;
    [SavedProperty] public int TypeMask { get; set; }
    [SavedProperty] public int MaskTurn { get; set; }=-1;
    [SavedProperty] public bool SpentVisual {get=>Status==RelicStatus.Disabled;set=>Status=value?RelicStatus.Disabled:RelicStatus.Normal;}
    public override bool ShowCounter=>CombatManager.Instance.IsInProgress;
    public override int DisplayAmount=>Key=="family_seal"?System.Numerics.BitOperations.PopCount((uint)TypeMask):CombatClaimed||(IsMutable&&TriggerTurn==Owner?.PlayerCombatState?.TurnNumber)?0:1;
    private void Display(bool spent) {Status=spent?RelicStatus.Disabled:RelicStatus.Normal;InvokeDisplayAmountChanged();}
    private bool ClaimTurn()
    {
        var turn=Owner.PlayerCombatState!.TurnNumber;
        if(TriggerTurn==turn)return false;
        TriggerTurn=turn;Display(true);Flash();return true;
    }
    private bool ClaimCombat()
    {
        if(CombatClaimed)return false;
        CombatClaimed=true;Display(true);Flash();return true;
    }
    private Task Block(int amount)=>CreatureCmd.GainBlock(Owner.Creature,amount,ValueProp.Unpowered,null);
    public override Task BeforeCombatStart()
    {
        CombatClaimed=false;TriggerTurn=MaskTurn=-1;TypeMask=0;Display(false);return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room) {Status=RelicStatus.Normal;return Task.CompletedTask;}
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context,Player player)
    {
        if(player!=Owner)return;
        if(Key=="family_seal") {TypeMask=0;MaskTurn=player.PlayerCombatState!.TurnNumber;}
        Display(CombatClaimed);
        if(Key=="legacy_hourglass"&&player.PlayerCombatState!.TurnNumber==Row.Number("threshold")&&ClaimCombat())
        {
            await PlayerCmd.GainEnergy(Row.Number("amount"),Owner);
            await CardPileCmd.Draw(context,Row.Number("extra"),Owner);
        }
        if(Key=="trail_compass"&&player.PlayerCombatState!.TurnNumber==Row.Number("threshold")&&ClaimCombat())
            await CardEffects.Apply(context,"vigor",Owner.Creature,Row.Number("amount"),Owner.Creature,null);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context,CardPlay play)
    {
        if(play.Card.Owner!=Owner||!CombatManager.Instance.IsInProgress||!Owner.Creature.IsAlive)return;
        switch(Key)
        {
            case "heirloom_brooch" when play.Card.Type==CardType.Power:
                if(ClaimCombat())await Block(Row.Number("amount"));break;
            case "guardian_knot" when play.Card.Type==CardType.Attack&&Owner.Creature.Block>0:
                if(ClaimCombat())await Block(Row.Number("amount"));break;
            case "scholar_bookmark" when play.Card.Type==CardType.Power:
                if(ClaimCombat())await CardPileCmd.Draw(context,Row.Number("amount"),Owner);break;
            case "training_dummy" when play.Card.Type==CardType.Attack&&Owner.Creature.Block>0:
                if(ClaimTurn())await CardEffects.Apply(context,"vigor",Owner.Creature,Row.Number("amount"),Owner.Creature,null);break;
            case "triune_crest":
                var cls=PlayerBloodlines.Heir(Owner).classId;
                if((cls=="ranger"?play.Card.Type==CardType.Attack:play.Card.Type==CardType.Skill)&&ClaimCombat())
                {
                    if(cls=="knight")await Block(Row.Number("amount"));
                    else if(cls=="mage"&&Owner.Creature.GetPower<P_class>() is {} charge)charge.Charges=Math.Min(2,charge.Charges+Row.Number("extra"));
                    else if(cls=="ranger")await CardEffects.Apply(context,"vigor",Owner.Creature,Row.Number("threshold"),Owner.Creature,null);
                }
                break;
            case "family_seal":
                var turn=Owner.PlayerCombatState!.TurnNumber;
                if(MaskTurn!=turn) {MaskTurn=turn;TypeMask=0;}
                TypeMask|=play.Card.Type switch {CardType.Attack=>1,CardType.Skill=>2,CardType.Power=>4,_=>0};
                InvokeDisplayAmountChanged();
                if(TypeMask==7&&ClaimTurn()) {await Block(Row.Number("amount"));await CardPileCmd.Draw(context,Row.Number("extra"),Owner);}
                break;
        }
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
public sealed class R_triune_crest : FamilyDropRelic { }
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
