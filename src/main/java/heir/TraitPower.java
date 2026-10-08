package heir;
import com.megacrit.cardcrawl.powers.*;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.core.AbstractCreature;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.actions.common.*;

/** Runtime card/turn hooks for an inherited trait, with exact sheet description. */
public final class TraitPower extends AbstractPower {
    private final String effect;
    private final int value;
    private boolean firstSkill=true,firstAttack=true,shocked=false;
    private int pendingEnergy=0;
    public TraitPower(Data.Row row){owner=AbstractDungeon.player;ID="heir:trait:"+row.s("id");name=row.s("name");description=row.s("summary");effect=row.s("effect");value=row.i("amount");amount=-1;type=PowerType.BUFF;img=HeirMod.texture(row.s("asset")+".png");}
    public boolean canPlayCard(AbstractCard card){return !effect.equals("pacifist")||card.type!=AbstractCard.CardType.ATTACK;}
    public void atStartOfTurn(){firstSkill=firstAttack=true;}
    public void onEnergyRecharge(){
        if(effect.equals("energy"))addToBot(new GainEnergyAction(value));
        if(effect.equals("exhausted")&&com.megacrit.cardcrawl.actions.GameActionManager.turn%2==0)AbstractDungeon.player.loseEnergy(value);
        if(pendingEnergy>0){addToBot(new GainEnergyAction(pendingEnergy));pendingEnergy=0;}
    }
    public void atStartOfTurnPostDraw(){if(effect.equals("turn_block"))addToBot(new GainBlockAction(owner,owner,value));}
    public void onCardDraw(AbstractCard card){if(effect.equals("costly")&&card.type==AbstractCard.CardType.ATTACK&&card.costForTurn>=0)card.setCostForTurn(card.costForTurn+1);if(shocked&&card.type==AbstractCard.CardType.ATTACK){shocked=false;addToBot(new DiscardSpecificCardAction(card));}}
    public void onUseCard(AbstractCard card,com.megacrit.cardcrawl.actions.utility.UseCardAction action){
        if(card.type==AbstractCard.CardType.SKILL){if(effect.equals("skill_block"))addToBot(new GainBlockAction(owner,owner,value));if(effect.equals("skill_draw")&&firstSkill){firstSkill=false;addToBot(new DrawCardAction(value));}}
        if(card.type==AbstractCard.CardType.ATTACK&&firstAttack){firstAttack=false;if(effect.equals("first_attack_block"))addToBot(new GainBlockAction(owner,owner,value));}
    }
    public float atDamageGive(float damage,DamageInfo.DamageType type){if(type!=DamageInfo.DamageType.NORMAL)return damage;if(effect.equals("costly"))return damage*(1+value/100f);if(effect.equals("perfectionist"))return damage*(owner.currentBlock>0?1.5f:.75f);if(effect.equals("piercing"))return damage+value;return damage;}
    public void wasHPLost(DamageInfo info,int damage){if(damage<=0||info.owner==null||info.owner==owner||info.type!=DamageInfo.DamageType.NORMAL)return;if(effect.equals("hurt_energy"))pendingEnergy+=value;if(effect.equals("hurt_weak"))addToBot(new ApplyPowerAction(owner,owner,new WeakPower(owner,value,false),value));if(effect.equals("shock"))shocked=true;}
    public int onAttackedToChangeDamage(DamageInfo info,int damage){return effect.equals("algesia")&&damage>0&&info.type==DamageInfo.DamageType.NORMAL?damage+value:damage;}
}
