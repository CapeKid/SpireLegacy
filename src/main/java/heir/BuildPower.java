package heir;
import com.megacrit.cardcrawl.actions.AbstractGameAction.AttackEffect;
import com.megacrit.cardcrawl.actions.common.*;
import com.megacrit.cardcrawl.actions.utility.UseCardAction;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.core.AbstractCreature;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.powers.*;

/** Separate engines with explicit counters; end-turn discards never earn rewards. */
public final class BuildPower extends AbstractPower {
    final String effect;
    private int plays;
    private boolean discardPaid;
    public BuildPower(String effect,AbstractCreature owner,int n){
        this.effect=effect;this.owner=owner;ID="heir:build:"+effect;amount=n;type=PowerType.BUFF;
        name=effect.equals("exhaust_damage")?"Forge Sparks":effect.equals("skill_block")?"Spellweave":effect.equals("skill_energy")?"Arcane Meter":effect.equals("discard_block")?"Light Pack":effect.equals("discard_energy")?"Tailwind":effect.equals("attack_vigor")?"Tracking Rhythm":effect.equals("scry_block")?"Forest Oracle":"Rune Familiar";
        loadRegion(effect.equals("discard_energy")||effect.equals("skill_energy")?"energized_green":"strength");updateDescription();
    }
    public void updateDescription(){
        description=Data.row("power_effects",effect).s("summary").replace("!M!",String.valueOf(amount));
        if(effect.equals("skill_block")||effect.equals("skill_energy")||effect.equals("attack_vigor"))description+=" NL Current cycle: "+plays+" / 3.";
        if(effect.equals("discard_energy"))description+=" NL "+(discardPaid?"Used this turn.":"Ready this turn.");
    }
    public void atStartOfTurn(){plays=0;discardPaid=false;updateDescription();}
    public void onUseCard(AbstractCard card,UseCardAction action){
        if(effect.equals("power_draw")&&card.type==AbstractCard.CardType.POWER){flash();addToBot(new DrawCardAction(AbstractDungeon.player,amount));}
        boolean skill=(effect.equals("skill_block")||effect.equals("skill_energy"))&&card.type==AbstractCard.CardType.SKILL;
        boolean attack=effect.equals("attack_vigor")&&card.type==AbstractCard.CardType.ATTACK;
        if((skill||attack)&&++plays==3){
            plays=0;flash();
            if(effect.equals("skill_block"))addToBot(new GainBlockAction(owner,owner,amount));
            if(effect.equals("skill_energy"))addToBot(new ApplyPowerAction(owner,owner,new EnergizedPower(owner,amount),amount));
            // UseCardAction's after-use callback runs after consumption of any existing Vigor.
        }
        updateDescription();
    }
    public void onAfterUseCard(AbstractCard card,UseCardAction action){
        if(effect.equals("attack_vigor")&&card.type==AbstractCard.CardType.ATTACK&&plays==0)
            addToBot(new ApplyPowerAction(owner,owner,new com.megacrit.cardcrawl.powers.watcher.VigorPower(owner,amount),amount));
    }
    public void onExhaust(AbstractCard card){
        if(effect.equals("exhaust_damage")){flash();addToBot(new DamageAllEnemiesAction(owner,DamageInfo.createDamageMatrix(amount,true),DamageInfo.DamageType.THORNS,AttackEffect.FIRE));}
    }
    public void onScry(){if(effect.equals("scry_block")){flash();addToBot(new GainBlockAction(owner,owner,amount));}}
    public void onManualDiscard(){
        if(effect.equals("discard_block")){flash();addToBot(new GainBlockAction(owner,owner,amount));}
        if(effect.equals("discard_energy")&&!discardPaid){discardPaid=true;updateDescription();flash();addToBot(new GainEnergyAction(amount));}
    }
}
