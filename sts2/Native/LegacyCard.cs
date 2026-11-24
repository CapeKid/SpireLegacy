using BaseLib.Utils;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
    [SavedProperty] public int DiscardRewardTurn { get; set; } = -1;
    [SavedProperty] public int RetainedGrowth { get; set; }
    public CardDesign DesignCard => new(Key, IsUpgraded, IsMutable && TraitSnapshot.Length > 0 ? JsonSerializer.Deserialize<string[]>(TraitSnapshot)! : IsMutable && Owner!=null ? PlayerBloodlines.Heir(Owner).traits : PlayerBloodlines.Creating.Value?.heir.traits ?? Runtime.Heir.traits);
    protected Design.Row Row => Design.Get("cards", Key);
    protected LegacyCard(string key) : base(Design.Get("cards", key).Number("cost"), Enum.Parse<CardType>(Design.Get("cards", key).Text("type"), true), Enum.Parse<CardRarity>(Design.Get("cards", key).Text("rarity"), true), GetTarget(key), autoAdd: false) { }
    private static TargetType GetTarget(string key)
    {
        var row = Design.Get("cards", key);
        if (row.Flag("aoe") || row.Text("special") == "random_attack") return TargetType.AllEnemies;
        return row.Text("type") == "ATTACK" || row.Number("poison") + row.Number("weak") + row.Number("vulnerable") > 0 || row.Text("special") is "double_poison" or "poison_twice" ? TargetType.AnyEnemy : TargetType.Self;
    }
    public override CardPoolModel Pool => IsMutable && Owner?.Character is HeirCharacter heir ? heir.CardPool : ModelDb.Character<HeirCharacter>().CardPool;
    public override CardType Type => DesignCard.Mercy ? CardType.Skill : base.Type;
    // BaseLib infers this from the presence of a BlockVar, including our zero-value
    // compatibility slot. Enchantment eligibility must reflect actual card effects.
    public override bool GainsBlock => DesignCard.Block > 0 || Row.Number("nextBlock") > 0 || HasSpecialBlock || Row.Text("special") == "double_block";
    public bool HasSpecialBlock => Row.Text("special") is "discard_block" or "exhaust_block" or "exhaust_nonattacks" or "guard_block";
    public BlockVar NextBlock => (BlockVar)DynamicVars["BlockNextTurn"];
    public BlockVar SpecialBlock => (BlockVar)DynamicVars["SpecialBlock"];
    public override Texture2D CustomPortrait => Runtime.Texture(Design.Get("card_art", Row.Text("art")).Text("path"));
    public override string PortraitPath => MissingPortraitPath;
    public override string BetaPortraitPath => MissingPortraitPath;
    public override IEnumerable<string> AllPortraitPaths => [];
    public override List<(string, string)> Localization
    {
        get
        {
            var entries = HeirLocalization.Entries(("title", Row.Text("name")), ("description", "{Rules}"), ("returnPrompt", "Choose cards to return to your hand."), ("attackReturnPrompt", "Choose Attacks to return to your hand."));
            if (Row.Text("special") == "discount_card") entries.Add(("discountPrompt", HeirLocalization.Text("Choose a card to discount this turn.")));
            if (Row.Text("special") == "discount_choice") entries.Add(("discountPrompt", HeirLocalization.Text("Choose cards to discount this turn.")));
            return entries;
        }
    }
    protected override void AddExtraArgsToDescription(LocString description) => description.Add("Rules", DesignCard.Rules(
        Row.Number("nextBlock") > 0 ? NextBlock.ToHighlightedString(false) : null,
        HasSpecialBlock ? SpecialBlock.ToHighlightedString(false) : null, HeirLocalization.Text, ClassReactions.ClassOf(this)).Replace("{Damage}", DynamicVars.Damage.ToHighlightedString(false)).Replace("{Block}", DynamicVars.Block.ToHighlightedString(false)));
    protected override IEnumerable<DynamicVar> CanonicalVars {
        get {
            var design = new CardDesign(Key);
            yield return new DamageVar(design.Damage, ValueProp.Move);
            yield return new BlockVar(design.Block, ValueProp.Move);
            if (Row.Number("nextBlock") > 0) yield return new BlockVar("BlockNextTurn", design.Amount("nextBlock"), ValueProp.Move);
            if (HasSpecialBlock) yield return new BlockVar("SpecialBlock", design.SpecialAmount, ValueProp.Unpowered);
        }
    }
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        if (TraitSnapshot.Length > 0) return;
        if (PlayerBloodlines.Creating.Value is not {} creating) return;
        TraitSnapshot = JsonSerializer.Serialize(creating.heir.traits);
        RefreshInheritedStats();
    }
    public void CaptureOwnerTraits() {
        if(Owner==null)return;
        var traits=JsonSerializer.Serialize(PlayerBloodlines.Heir(Owner).traits);
        if(TraitSnapshot==traits)return;
        // Rebinding replaces the inherited contribution without erasing
        // card-local buffs, enchantments or retained growth.
        var previous=new CardDesign(Key,IsUpgraded,TraitSnapshot.Length>0?JsonSerializer.Deserialize<string[]>(TraitSnapshot)!:[]);
        TraitSnapshot=traits;
        DynamicVars.Damage.BaseValue+=DesignCard.Damage-previous.Damage;
        DynamicVars.Block.BaseValue+=DesignCard.Block-previous.Block;
    }
    protected override void AfterDeserialized() { base.AfterDeserialized(); RefreshInheritedStats(); }
    protected override void AfterDowngraded() { base.AfterDowngraded(); RefreshInheritedStats(); }
    private void RefreshInheritedStats()
    {
        DynamicVars.Damage.BaseValue = DesignCard.Damage + (Row.Text("special")=="retain_damage" ? RetainedGrowth : 0);
        DynamicVars.Block.BaseValue = DesignCard.Block + (Row.Text("special")=="retain_block" ? RetainedGrowth : 0);
        if (Row.Number("nextBlock") > 0) NextBlock.BaseValue = DesignCard.Amount("nextBlock");
        if (HasSpecialBlock) SpecialBlock.BaseValue = DesignCard.SpecialAmount;
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Row.Text("power") == "intangible"
        ? base.ExtraHoverTips.Append(HoverTipFactory.FromPower<IntangiblePower>())
        : base.ExtraHoverTips;
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { ("exhaust", CardKeyword.Exhaust), ("retain", CardKeyword.Retain), ("ethereal", CardKeyword.Ethereal), ("innate", CardKeyword.Innate) }.Where(e => Row.Flag(e.Item1)).Select(e => e.Item2);
    protected override HashSet<CardTag> CanonicalTags => Key.EndsWith("_strike") ? [CardTag.Strike] : Key.EndsWith("_guard") ? [CardTag.Defend] : [];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var design = DesignCard;
        var repeatPoison = Row.Text("special") == "arcane_poison" && CardEffects.FullArcaneFlowReady(Owner);
        if (!design.Mercy && (DynamicVars.Damage.BaseValue > 0 || Row.Text("special") is "block_damage" or "chain_damage"))
            {
                // Native Vigor and other attack-scoped effects last for the complete
                // multi-hit command, rather than being consumed by its first hit.
                var hits = Row.Number("hits") + (Row.Text("special") == "grudge_repeat" && play.Target is {} marked && (marked.GetPowerAmount<WeakPower>() > 0 || marked.GetPowerAmount<VulnerablePower>() > 0) ? 1 : 0);
                var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCardCompatibility(this, play);
                var victim = play.Target; var victimAlive = victim?.IsAlive == true;
                if (Row.Flag("aoe") || ClassReactions.Is(this, "birthright", "ranger")) attack.TargetingAllOpponents(CombatState!);
                else if (Row.Text("special") == "random_attack") attack.TargetingRandomOpponents(CombatState!);
                else attack.Targeting(play.Target!);
                await attack.Execute(context);
                // Only a kill made by this play pays out, not a replay on a corpse.
                if (Row.Text("special") == "crown_breaker" && victimAlive && victim is { IsAlive: false })
                {
                    await PlayerCmd.GainEnergy(1, Owner);
                    await CardPileCmd.Draw(context, 2, Owner);
                }
            }
        var hadBlock = Owner.Creature.Block > 0;
        if (DynamicVars.Block.BaseValue > 0) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        if (Row.Text("special") == "guard_block" && hadBlock) await CreatureCmd.GainBlock(Owner.Creature, SpecialBlock, play);
        await CardEffects.Execute(this, context, play, repeatPoison);
        await ClassReactions.OnPlay(this, context, play);
    }
    protected override void OnUpgrade()
    {
        // Native UpgradeValueBy(0) still marks a variable as upgraded, which
        // falsely colors unchanged stats green in cost-only upgrade previews.
        if (!DesignCard.Mercy && Row.Number("upgradeDamage") != 0) DynamicVars.Damage.UpgradeValueBy(Row.Number("upgradeDamage"));
        if (!DesignCard.Mercy && Row.Number("upgradeBlock") != 0) DynamicVars.Block.UpgradeValueBy(Row.Number("upgradeBlock"));
        if (Row.Number("nextBlock") > 0 && Row.Number("upgradeMagic") != 0) NextBlock.UpgradeValueBy(Row.Number("upgradeMagic"));
        if (HasSpecialBlock && Row.Number("upgradeSpecial") != 0) SpecialBlock.UpgradeValueBy(Row.Number("upgradeSpecial"));
        if (Row.Flag("upgradeCost")) EnergyCost.UpgradeBy(-1);
        if (!Row.Flag("retain") && DesignCard.Retain) AddKeyword(CardKeyword.Retain);
        if (!Row.Flag("innate") && DesignCard.Innate) AddKeyword(CardKeyword.Innate);
    }
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || Row.Text("special") != "power_discount" || Owner?.Creature.GetPower<P_class>() is not { PowersPlayed: > 0 } counter) return false;
        modifiedCost = Math.Max(0, modifiedCost - counter.PowersPlayed);
        return true;
    }
    public decimal InheritedDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? source)
    {
        if (source != this || !props.IsPoweredAttack()) return 0;
        var n = DesignCard.SpecialAmount; var basis = DynamicVars.Damage.BaseValue;
        return Row.Text("special") switch {
            "block_damage" => Owner.Creature.Block - basis,
            "chain_damage" => (Owner.Creature.GetPower<P_class>()?.AttacksThisTurn ?? 0) * n - basis,
            "exhaust_damage" => Math.Min(10, PileType.Exhaust.GetPile(Owner).Cards.Count) * n,
            "arcane_aftershock" when PlayerBloodlines.Heir(Owner).classId=="mage" && Owner.Creature.GetPower<P_class>() is {ArcaneDrawUsed:true} => n,
            "charge_burst" when PlayerBloodlines.Heir(Owner).classId=="mage" => (Owner.Creature.GetPower<P_class>()?.Charges ?? 0)*n,
            "full_charge_bonus" when PlayerBloodlines.Heir(Owner).classId == "mage" &&
                Owner.Creature.GetPower<P_class>() is { Charges: >= 2 } => n,
            "skill_damage" => Math.Min(3,Owner.Creature.GetPower<P_class>()?.SkillsThisTurn ?? 0)*n,
            "guard_bonus" when Owner.Creature.Block > 0 => n,
            "execute" when target is not null && target.CurrentHp * 2 <= target.MaxHp => basis * (n - 1),
            "full_hp_bonus" when target is not null && target.CurrentHp == target.MaxHp => n,
            "marked_bonus" when target is not null && (target.GetPowerAmount<VulnerablePower>() > 0 || target.GetPowerAmount<PoisonPower>() > 0) => n,
            _ => 0 } + ClassReactions.DamageBonus(this);
    }
    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? card)
    {
        if (card != this || Row.Text("special") != "hit_poison" || dealer != Owner.Creature ||
            !props.IsPoweredAttack() || !target.IsAlive || target.Side == Owner.Creature.Side)
            return Task.CompletedTask;

        return CardEffects.Apply(context, "poison", target, DesignCard.SpecialAmount, Owner.Creature, this);
    }
    public override async Task AfterCardDiscarded(PlayerChoiceContext context, CardModel card)
    {
        if (card != this || Owner.PlayerCombatState?.Phase != PlayerTurnPhase.Play || CombatState?.CurrentSide != Owner.Creature.Side) return;
        var special = Row.Text("special");
        if (special is not ("discard_draw" or "discard_energy" or "discard_block" or "discard_vigor" or "discard_energy_exhaust")) return;
        if (special == "discard_energy_exhaust")
        {
            if (Pile?.Type != PileType.Discard) return;
            await BranchCommands.Exhaust(context, this);
            await PlayerCmd.GainEnergy(DesignCard.SpecialAmount, Owner);
            return;
        }
        var turn = Owner.PlayerCombatState.TurnNumber;
        if (DiscardRewardTurn == turn) return;
        DiscardRewardTurn = turn; // Set before drawing: nested discard effects cannot re-enter the reward.
        var n = DesignCard.SpecialAmount;
        if (Row.Text("special") == "discard_draw") await CardPileCmd.Draw(context, n, Owner);
        if (Row.Text("special") == "discard_energy") await PlayerCmd.GainEnergy(n, Owner);
        if (Row.Text("special") == "discard_vigor") await CardEffects.Apply(context,"vigor",Owner.Creature,n,Owner.Creature,this);
        if (Row.Text("special") == "discard_block") {
            // This is a discard trigger rather than a card play. Resolve the card's
            // enchantment once, then deliver the result without powered Block hooks.
            decimal block = SpecialBlock.BaseValue;
            if (Enchantment is {} enchantment) {
                block += enchantment.EnchantBlockAdditive(block);
                block *= enchantment.EnchantBlockMultiplicative(block);
            }
            await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Unpowered, null);
        }
    }
    public override Task BeforeCombatStart() {
        DiscardRewardTurn = -1;
        if(Row.Text("special")=="retain_damage")DynamicVars.Damage.BaseValue-=RetainedGrowth;
        if(Row.Text("special")=="retain_block")DynamicVars.Block.BaseValue-=RetainedGrowth;
        RetainedGrowth=0;
        return Task.CompletedTask;
    }
    public override Task AfterFlush(PlayerChoiceContext context, Player player, IReadOnlyCollection<CardModel> flushed, IReadOnlyCollection<CardModel> retained)
    {
        if (player == Owner && retained.Contains(this)) {
            if(Row.Text("special") is "retain_damage" or "retain_block") {
                var next=(int)Math.Min(int.MaxValue,(long)RetainedGrowth+DesignCard.SpecialAmount);
                var added=next-RetainedGrowth;RetainedGrowth=next;
                if (Row.Text("special") == "retain_damage") DynamicVars.Damage.BaseValue += added;
                if (Row.Text("special") == "retain_block") DynamicVars.Block.BaseValue += added;
            }
        }
        return Task.CompletedTask;
    }
}
