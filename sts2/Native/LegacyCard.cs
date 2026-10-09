using BaseLib.Utils;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Text.Json;

namespace SpireLegacy;

public abstract class LegacyCard : CustomCardModel
{
    public string Key => GetType().Name[2..];
    [SavedProperty] public string TraitSnapshot { get; set; } = "";
    public CardDesign DesignCard => new(Key, IsUpgraded, IsMutable && TraitSnapshot.Length > 0 ? JsonSerializer.Deserialize<string[]>(TraitSnapshot)! : Runtime.Heir.traits);
    protected Design.Row Row => Design.Get("cards", Key);
    protected LegacyCard(string key) : base(Design.Get("cards", key).Number("cost"), Enum.Parse<CardType>(Design.Get("cards", key).Text("type"), true), Enum.Parse<CardRarity>(Design.Get("cards", key).Text("rarity"), true), GetTarget(key), autoAdd: false) { }
    private static TargetType GetTarget(string key)
    {
        var row = Design.Get("cards", key);
        if (row.Flag("aoe") || row.Text("special") == "random_attack") return TargetType.AllEnemies;
        return row.Text("type") == "ATTACK" || row.Number("poison") + row.Number("weak") + row.Number("vulnerable") > 0 || row.Text("special") == "double_poison" ? TargetType.AnyEnemy : TargetType.Self;
    }
    public override CardPoolModel Pool => ModelDb.Character<HeirCharacter>().CardPool;
    public override CardType Type => DesignCard.Mercy ? CardType.Skill : base.Type;
    public override Texture2D CustomPortrait => Runtime.Texture(Design.Get("card_art", Row.Text("art")).Text("path"));
    public override string PortraitPath => MissingPortraitPath;
    public override string BetaPortraitPath => MissingPortraitPath;
    public override IEnumerable<string> AllPortraitPaths => [];
    public override List<(string, string)> Localization => [("title", Row.Text("name")), ("description", "{Rules}"),("returnPrompt","Choose cards to return to your hand.")];
    protected override void AddExtraArgsToDescription(LocString description) => description.Add("Rules", DesignCard.Rules().Replace("{Damage}", DynamicVars.Damage.ToHighlightedString(false)).Replace("{Block}", DynamicVars.Block.ToHighlightedString(false)));
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(new CardDesign(Key).Damage, ValueProp.Move), new BlockVar(new CardDesign(Key).Block, ValueProp.Move)];
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        if (TraitSnapshot.Length > 0) return;
        TraitSnapshot = JsonSerializer.Serialize(Runtime.Heir.traits);
        RefreshInheritedStats();
    }
    protected override void AfterDeserialized() { base.AfterDeserialized(); RefreshInheritedStats(); }
    protected override void AfterDowngraded() { base.AfterDowngraded(); RefreshInheritedStats(); }
    private void RefreshInheritedStats()
    {
        DynamicVars.Damage.BaseValue = DesignCard.Damage;
        DynamicVars.Block.BaseValue = DesignCard.Block;
    }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { ("exhaust", CardKeyword.Exhaust), ("retain", CardKeyword.Retain), ("ethereal", CardKeyword.Ethereal), ("innate", CardKeyword.Innate) }.Where(e => Row.Flag(e.Item1)).Select(e => e.Item2);
    protected override HashSet<CardTag> CanonicalTags => Key.EndsWith("_strike") ? [CardTag.Strike] : Key.EndsWith("_guard") ? [CardTag.Defend] : [];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var design = DesignCard;
        if (!design.Mercy && (DynamicVars.Damage.BaseValue > 0 || Row.Text("special") is "block_damage" or "chain_damage"))
            for (var hit = 0; hit < Row.Number("hits"); hit++)
            {
                var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, play);
                if (Row.Flag("aoe")) attack.TargetingAllOpponents(CombatState!);
                else if (Row.Text("special") == "random_attack") attack.TargetingRandomOpponents(CombatState!);
                else attack.Targeting(play.Target!);
                await attack.Execute(context);
            }
        if (DynamicVars.Block.BaseValue > 0) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await CardEffects.Execute(this, context, play);
    }
    protected override void OnUpgrade()
    {
        if (!DesignCard.Mercy) DynamicVars.Damage.UpgradeValueBy(Row.Number("upgradeDamage"));
        DynamicVars.Block.UpgradeValueBy(DesignCard.Mercy ? 3 : Row.Number("upgradeBlock"));
        if (Row.Flag("upgradeCost")) EnergyCost.UpgradeBy(-1);
    }
    public decimal InheritedDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? source)
    {
        if (source != this || !props.IsPoweredAttack()) return 0;
        var n = DesignCard.SpecialAmount; var basis = DynamicVars.Damage.BaseValue;
        return Row.Text("special") switch {
            "block_damage" => Owner.Creature.Block - basis,
            "chain_damage" => (Owner.Creature.GetPower<P_class>()?.AttacksThisTurn ?? 0) * n - basis,
            "exhaust_damage" => Math.Min(10, PileType.Exhaust.GetPile(Owner).Cards.Count) * n,
            "guard_bonus" when Owner.Creature.Block > 0 => n,
            "execute" when target is not null && target.CurrentHp * 2 <= target.MaxHp => basis * (n - 1),
            "marked_bonus" when target is not null && (target.GetPowerAmount<VulnerablePower>() > 0 || target.GetPowerAmount<PoisonPower>() > 0) => n,
            _ => 0 };
    }
    public override async Task AfterCardDiscarded(PlayerChoiceContext context, CardModel card)
    {
        if (card != this || Owner.PlayerCombatState?.Phase == PlayerTurnPhase.End) return;
        var n = DesignCard.SpecialAmount;
        if (Row.Text("special") == "discard_draw") await CardPileCmd.Draw(context, n, Owner);
        if (Row.Text("special") == "discard_energy") await PlayerCmd.GainEnergy(n, Owner);
        if (Row.Text("special") == "discard_block") await CreatureCmd.GainBlock(Owner.Creature, n, ValueProp.Unpowered, null);
    }
    public override Task AfterFlush(PlayerChoiceContext context, Player player, IReadOnlyCollection<CardModel> flushed, IReadOnlyCollection<CardModel> retained)
    {
        if (player == Owner && retained.Contains(this)) {
            if (Row.Text("special") == "retain_damage") DynamicVars.Damage.BaseValue += DesignCard.SpecialAmount;
            if (Row.Text("special") == "retain_block") DynamicVars.Block.BaseValue += DesignCard.SpecialAmount;
        }
        return Task.CompletedTask;
    }
}
