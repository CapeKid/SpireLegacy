package heir;
import com.megacrit.cardcrawl.actions.AbstractGameAction;
import com.megacrit.cardcrawl.actions.common.*;
import com.megacrit.cardcrawl.actions.defect.DiscardPileToHandAction;
import com.megacrit.cardcrawl.actions.unique.ExhumeAction;
import com.megacrit.cardcrawl.actions.utility.ScryAction;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.monsters.AbstractMonster;
import com.megacrit.cardcrawl.powers.*;
import java.util.*;

/** Resolve after preceding draw/exhaust actions rather than reading stale state at use(). */
public final class CardEffectAction extends AbstractGameAction {
    private final String effect;private final AbstractMonster enemy;private final boolean all;
    public CardEffectAction(String effect,int n,AbstractMonster enemy,boolean all){this.effect=effect;amount=n;this.enemy=enemy;this.all=all;actionType=ActionType.SPECIAL;}
    public void update(){
        com.megacrit.cardcrawl.characters.AbstractPlayer p=AbstractDungeon.player;
        switch(effect){
            case "double_block":addToTop(new GainBlockAction(p,p,p.currentBlock));break;
            case "cleanse":for(AbstractPower power:new ArrayList<>(p.powers))if(power.type==AbstractPower.PowerType.DEBUFF)addToTop(new RemoveSpecificPowerAction(p,p,power.ID));break;
            case "exhaust_one":addToTop(new ExhaustAction(p,p,amount,false));break;
            case "retrieve":addToTop(new DiscardPileToHandAction(amount));break;
            case "exhume":addToTop(new ExhumeAction(false));break;
            case "scry":addToTop(new ScryAction(amount));break;
            case "charge_gain":AbstractPower charge=p.getPower("heir:class");if(HeirMod.heir().classId.equals("mage")&&charge instanceof ClassPower){charge.amount=Math.min(Data.row("class_mechanics","mage").i("limit"),charge.amount+amount);charge.updateDescription();p.hand.applyPowers();}break;
            case "discount_hand":case "free_hand":for(AbstractCard card:p.hand.group)if(card.costForTurn>=0){card.setCostForTurn(effect.equals("free_hand")?0:Math.max(0,card.costForTurn-1));card.applyPowers();}break;
            case "double_poison":
                List<AbstractMonster> targets=new ArrayList<>();if(all)targets.addAll(AbstractDungeon.getMonsters().monsters);else if(enemy!=null)targets.add(enemy);
                for(AbstractMonster m:targets)if(!m.isDeadOrEscaped()){AbstractPower poison=m.getPower(PoisonPower.POWER_ID);if(poison!=null&&poison.amount>0)addToTop(new ApplyPowerAction(m,p,new PoisonPower(m,p,poison.amount),poison.amount));}break;
            default:throw new IllegalArgumentException("Unknown card action: "+effect);
        }
        isDone=true;
    }
}
