using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Text.Json;

namespace SpireLegacy;

/// <summary>Run-wide inheritance hooks; the visible manor panel supplies the full trait descriptions.</summary>
public sealed class BloodlineRelic : CustomRelicModel
{
    public BloodlineRelic() : base(false) { }
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override string IconBaseName => "burning_blood";
    public override List<(string,string)> Localization => [("title", "Family Bloodline"), ("description", "Your class, inherited traits and purchased manor upgrades shape this climb. Open the Family Manor to read every trait."), ("flavor", "Every climb ends. Your bloodline grows.")];
    private bool firstSkill, firstAttack, shocked;
    private int pendingEnergy;
    [SavedProperty] public string FamilyRunSnapshot { get; set; } = "";
    [SavedProperty] public string SelectedHeir { get; set; } = "";
    [SavedProperty] public bool StartingRelicsGranted { get; set; }
    public override async Task AfterActEntered()
    {
        if (!StartingRelicsGranted)
        {
            StartingRelicsGranted = true;
            for (var i = 0; i < Inheritance.Amount("relics"); i++)
            {
                var relic = Owner.RunState.SharedRelicGrabBag.PullFromFront(RelicRarity.Common,Owner.RunState);
                if (relic == null) throw new InvalidDataException("No common relic is available for Antique.");
                await RelicCmd.Obtain(relic.ToMutable(),Owner);
            }
        }
        if (Runtime.Profile.active is { quick:true } run && Owner.RunState.TotalFloor == 0)
        {
            run.quick = false;
            FamilyRunSnapshot = JsonSerializer.Serialize(run,FamilyProfile.JsonOptions);
            Runtime.Profile.Save(Runtime.FamilyPath);
            await MegaCrit.Sts2.Core.Runs.RunManager.Instance.EnterMapCoord(Owner.RunState.Map.StartingMapPoint.coord);
        }
    }
    private Task Apply(PlayerChoiceContext context, string key, Creature target, int value) => value == 0 ? Task.CompletedTask : CardEffects.Apply(context,key,target,value,Owner.Creature,null);
    public override decimal ModifyHandDraw(Player player, decimal count) => player == Owner ? count + Runtime.Bonus("draw") + Inheritance.Stat("draw") : count;
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + Inheritance.Amount("energy") : amount;
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost) => player == Owner ? cost * (1 - Inheritance.Amount("shop") / 100m) : cost;
    public override bool ShouldPlay(CardModel card, AutoPlayType type) => card.Owner != Owner || !Inheritance.Has("pacifist") || card.Type != CardType.Attack;
    public override async Task BeforeCombatStart()
    {
        firstSkill = firstAttack = true; shocked = false; pendingEnergy = 0;
        var context = new BlockingPlayerChoiceContext();
        await Apply(context,"strength",Owner.Creature,Runtime.Bonus("strength") + Inheritance.Stat("strength"));
        await Apply(context,"dexterity",Owner.Creature,Runtime.Bonus("dexterity") + Inheritance.Stat("dexterity"));
        await Apply(context,"class",Owner.Creature,1);
        foreach (var enemy in Owner.Creature.CombatState!.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive))
        {
            await Apply(context,"poison",enemy,Inheritance.Amount("poison") + Inheritance.Amount("pacifist"));
            await Apply(context,"weak",enemy,Inheritance.Amount("weak"));
            await Apply(context,"strength",enemy,Inheritance.Amount("enemy_strength"));
            if (Inheritance.Amount("enemy_guard") > 0) await CreatureCmd.GainBlock(enemy,Inheritance.Amount("enemy_guard"),ValueProp.Unpowered,null);
        }
        await Apply(context,"thorns",Owner.Creature,Inheritance.Amount("thorns"));
        if (Inheritance.Has("diva")) await Apply(context,"vulnerable",Owner.Creature,1);
    }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        firstSkill = firstAttack = true;
        if (Inheritance.Has("exhausted") && player.PlayerCombatState!.TurnNumber % 2 == 0) await PlayerCmd.LoseEnergy(Inheritance.Amount("exhausted"),player);
        if (pendingEnergy > 0) { var amount = pendingEnergy; pendingEnergy = 0; await PlayerCmd.GainEnergy(amount,player); }
        if (Inheritance.Amount("turn_block") > 0) await CreatureCmd.GainBlock(player.Creature,Inheritance.Amount("turn_block"),ValueProp.Unpowered,null);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner != Owner) return;
        if (play.Card.Type == CardType.Skill)
        {
            if (Inheritance.Amount("skill_block") > 0) await CreatureCmd.GainBlock(Owner.Creature,Inheritance.Amount("skill_block"),ValueProp.Unpowered,null);
            if (firstSkill) { firstSkill = false; if (Inheritance.Amount("skill_draw") > 0) await CardPileCmd.Draw(context,Inheritance.Amount("skill_draw"),Owner); }
        }
        if (play.Card.Type == CardType.Attack && firstAttack) { firstAttack = false; if (Inheritance.Amount("first_attack_block") > 0) await CreatureCmd.GainBlock(Owner.Creature,Inheritance.Amount("first_attack_block"),ValueProp.Unpowered,null); }
    }
    public override async Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (card.Owner != Owner || card.Type != CardType.Attack) return;
        if (Inheritance.Has("costly") && !card.EnergyCost.CostsX) card.EnergyCost.AddThisTurn(1);
        if (shocked) { shocked = false; await CardCmd.Discard(context,card); }
    }
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card) => dealer == Owner.Creature && props.IsPoweredAttack() ? Inheritance.Amount("piercing") : 0;
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card)
    {
        if (dealer != Owner.Creature || !props.IsPoweredAttack()) return 1;
        var factor = 1 + Inheritance.Amount("costly") / 100m;
        if (Inheritance.Has("perfectionist")) factor *= Owner.Creature.Block > 0 ? 1.5m : .75m;
        return factor;
    }
    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? card) => target == Owner.Creature && amount > 0 && props.IsPoweredAttack() ? amount + Inheritance.Amount("algesia") : amount;
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? card)
    {
        if (target != Owner.Creature || dealer == null || dealer == target || result.UnblockedDamage <= 0 || !props.IsPoweredAttack()) return;
        pendingEnergy += Inheritance.Amount("hurt_energy");
        await Apply(context,"weak",target,Inheritance.Amount("hurt_weak"));
        if (Inheritance.Has("shock")) shocked = true;
    }
    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || creature == Owner.Creature || creature.Side == Owner.Creature.Side || Owner.Creature.IsDead) return;
        if (Inheritance.Amount("diva") > 0) await PlayerCmd.GainGold(Inheritance.Amount("diva"),Owner);
        if (Inheritance.Amount("explosions") > 0) await CreatureCmd.Damage(context,Owner.Creature,Inheritance.Amount("explosions"),ValueProp.Unblockable | ValueProp.Unpowered,Owner.Creature);
    }
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is TreasureRoom && Inheritance.Amount("chest") > 0) await CreatureCmd.Damage(new BlockingPlayerChoiceContext(),Owner.Creature,Inheritance.Amount("chest"),ValueProp.Unblockable | ValueProp.Unpowered,Owner.Creature);
        if (Runtime.Profile.active is {} run) { run.floors = Math.Max(run.floors,Owner.RunState.TotalFloor); FamilyRunSnapshot = JsonSerializer.Serialize(run,FamilyProfile.JsonOptions); Runtime.Profile.Save(Runtime.FamilyPath); }
    }
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Creature.IsDead) return;
        var heal = Runtime.Bonus("heal") + Inheritance.Stat("heal") + Design.Get("classes",Runtime.Heir.classId).Number("heal");
        if (heal > 0) await CreatureCmd.Heal(Owner.Creature,heal);
        if (Inheritance.Amount("gold") > 0) await PlayerCmd.GainGold(Inheritance.Amount("gold"),Owner);
        if (Inheritance.Amount("coin_loss") > 0) await PlayerCmd.LoseGold(Inheritance.Amount("coin_loss"),Owner);
    }
}
