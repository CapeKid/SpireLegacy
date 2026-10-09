package heir;
import com.megacrit.cardcrawl.powers.*;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.actions.common.*;
import com.megacrit.cardcrawl.actions.utility.UseCardAction;
/** Distinct class resource loops: defensive counters, spell charges, attack chains. */
public final class ClassPower extends AbstractPower {
    private final Data.Row row;
    public ClassPower(String classId){row=Data.row("class_mechanics",classId);owner=AbstractDungeon.player;ID="heir:class";name=row.s("name");amount=0;type=PowerType.BUFF;img=HeirMod.texture(Data.row("classes",classId).s("asset")+".png");updateDescription();}
    public void updateDescription(){description=row.s("summary")+" NL Current: "+amount+".";}
    private int bonus(String id){AbstractPower p=owner.getPower("heir:"+id);return p==null?0:p.amount;}
    public float atDamageGive(float damage,DamageInfo.DamageType type){if(type!=DamageInfo.DamageType.NORMAL)return damage;String effect=row.s("effect");if(effect.equals("counterguard"))return damage+(amount>0?row.i("amount"):0);if(effect.equals("charges"))return damage+amount*(row.i("amount")+bonus("reservoir"));return damage;}
    public void onUseCard(AbstractCard card,UseCardAction action){
        String effect=row.s("effect");
        if(card.type==AbstractCard.CardType.SKILL){if(effect.equals("counterguard"))amount=1;if(effect.equals("charges"))amount=Math.min(row.i("limit"),amount+1);}
        if(card.type==AbstractCard.CardType.ATTACK){
            if(effect.equals("counterguard")||effect.equals("charges"))amount=0;
            if(effect.equals("rhythm")){amount++;if(amount>=row.i("limit")){
                amount=0;addToBot(new DrawCardAction(row.i("amount")+bonus("quiver")));
                java.util.List<com.megacrit.cardcrawl.monsters.AbstractMonster> targets=new java.util.ArrayList<>();
                if(card.multiDamage!=null || (card instanceof HeirCard && Data.row("cards",((HeirCard)card).key).b("aoe")))targets.addAll(AbstractDungeon.getMonsters().monsters);
                else if(action.target instanceof com.megacrit.cardcrawl.monsters.AbstractMonster)targets.add((com.megacrit.cardcrawl.monsters.AbstractMonster)action.target);
                else if(card.target==AbstractCard.CardTarget.ALL_ENEMY){com.megacrit.cardcrawl.monsters.AbstractMonster enemy=AbstractDungeon.getRandomMonster();if(enemy!=null)targets.add(enemy);}
                for(com.megacrit.cardcrawl.monsters.AbstractMonster enemy:targets)if(!enemy.isDeadOrEscaped())addToBot(new ApplyPowerAction(enemy,owner,new VulnerablePower(enemy,row.i("amount"),false),row.i("amount")));
            }}
        }
        updateDescription();AbstractDungeon.player.hand.applyPowers();
    }
    public void atStartOfTurn(){if(row.s("effect").equals("rhythm")){amount=0;updateDescription();}}
}
