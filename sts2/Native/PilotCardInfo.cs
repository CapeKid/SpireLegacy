using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace SpireLegacy;

public static class PilotCardInfo
{
    // Legacy's explicit payment field is consumed immediately by CardEffects.
    // Native costs stay explicitly named: Haunt's HpLoss variable is a delayed
    // power, so a generic variable-name check would incorrectly forbid it.
    public static int ImmediateHpLoss(CardModel card)=>card is LegacyCard legacy?Math.Max(0,legacy.DesignCard.Amount("hpLoss")):
        card.GetType().Name is "Offering" or "Bloodletting" or "Hemokinesis"
        &&card.DynamicVars.TryGetValue("HpLoss",out var loss)?Math.Max(0,(int)Math.Ceiling(loss.BaseValue)):0;
    // Native variable names verified against owned card models. Some complex/generator cards lack these.
    public static int Amount(CardModel card,string effect)
    {
        if(card is LegacyCard mercy&&mercy.DesignCard.Mercy&&effect=="poison")return mercy.DesignCard.MercyPoison;
        if(card is LegacyCard doses&&doses.DesignCard.Row.Text("special")=="poison_twice"&&effect=="poison")return doses.DesignCard.SpecialAmount*2;
        if(card is LegacyCard lc)return lc.DesignCard.Row.Text("power")==effect?Math.Max(lc.DesignCard.Amount(effect),lc.DesignCard.Magic):lc.DesignCard.Amount(effect);
        var name=effect switch{"draw"=>"Cards","energy"=>"Energy","weak"=>"WeakPower","vulnerable"=>"VulnerablePower","poison"=>"PoisonPower","strength"=>"StrengthPower",_=>effect};
        return card.DynamicVars.TryGetValue(name,out var value)?Math.Max(0,(int)value.BaseValue):0;
    }
    public static bool ExhaustsOthers(CardModel card)=>card is LegacyCard lc?lc.DesignCard.Row.Text("special") is "exhaust_one" or "exhaust_nonattacks":card.GetType().Name is "BurningPact" or "TrueGrit" or "SecondWind" or "FiendFire";
    public static bool Engine(CardModel card)=>card is LegacyCard lc?lc.DesignCard.Row.Text("power") is "exhaust_damage" or "evolve" or "fire_breathing":card.GetType().Name is "FeelNoPain" or "DarkEmbrace" or "Evolve" or "FireBreathing";
    public static bool SimpleAttack(CardModel card)=>card is LegacyCard lc?lc.Type==CardType.Attack&&lc.DesignCard.Row.Number("hits")==1&&lc.DesignCard.Row.Text("special") is "none" or "execute" or "guard_bonus" or "marked_bonus" or "retain_damage" or "block_damage" or "chain_damage" or "exhaust_damage":card.GetType().Name is "StrikeIronclad" or "Bash" or "Anger" or "Cleave";
    public static int Hits(CardModel card)=>card is LegacyCard lc?Math.Max(1,lc.DesignCard.Row.Number("hits")):card.GetType().Name=="TwinStrike"?2:card.DynamicVars.TryGetValue("Repeat",out var repeat)?Math.Max(1,(int)repeat.BaseValue):1;
    public static PilotRewards.Card Describe(CardModel card)
    {
        double Value(string name)=>card.DynamicVars.TryGetValue(name,out var v)?Math.Max(0,(double)v.BaseValue):0;
        var damage=Value("Damage")*Hits(card);
        var cost=card.EnergyCost.CostsX?3:Math.Max(0,card.EnergyCost.GetWithModifiers(CostModifiers.All));
        var design=(card as LegacyCard)?.DesignCard;
        return new(card is LegacyCard legacy?legacy.Key:card.Id.ToString(),cost,damage,Value("Block"),Amount(card,"draw"),Amount(card,"energy"),Amount(card,"poison"),Amount(card,"strength"),card.Type==CardType.Power,ExhaustsOthers(card),Engine(card),Amount(card,"weak"),Amount(card,"vulnerable"),design?.Row.Text("special")??"none",design?.SpecialAmount??0,Hits(card),card.Type==CardType.Attack,card.Keywords.Contains(CardKeyword.Exhaust),design?.Amount("nextEnergy")??0,design?.Amount("nextDraw")??0,design?.Amount("nextBlock")??0,!PermanentlyBlocked(card),ImmediateHpLoss(card));
    }
    public static bool PermanentlyBlocked(CardModel card)=>card.Owner is {} owner&&PlayerBloodlines.Traits(owner).Has("pacifist")&&card.Type==CardType.Attack;
    public static bool Offense(CardModel card)=>!PermanentlyBlocked(card)&&(Amount(card,"poison")>0||card.DynamicVars.TryGetValue("Damage",out var damage)&&damage.BaseValue>0);
}
