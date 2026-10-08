package heir;

import basemod.abstracts.CustomCard;
import com.megacrit.cardcrawl.actions.AbstractGameAction.AttackEffect;
import com.megacrit.cardcrawl.actions.common.*;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.characters.AbstractPlayer;
import com.megacrit.cardcrawl.monsters.AbstractMonster;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.powers.*;

public class HeirCard extends CustomCard {
    public final String key;
    private final Data.Row row;
    public HeirCard(String key){
        super("heir:"+key,Data.row("cards",key).s("name"),Data.row("card_art",Data.row("cards",key).s("art")).s("path"),Data.row("cards",key).i("cost"),"",CardType.valueOf(Data.row("cards",key).s("type")),HeirMod.Enums.HEIR_COLOR,CardRarity.valueOf(Data.row("cards",key).s("rarity")),Data.row("cards",key).s("type").equals("ATTACK")?CardTarget.ENEMY:CardTarget.SELF);
        this.key=key;row=Data.row("cards",key);baseDamage=row.i("damage");baseBlock=row.i("block");baseMagicNumber=magicNumber=0;exhaust=row.b("exhaust");describe();
        if(row.s("rarity").equals("BASIC")){if(type==CardType.ATTACK)tags.add(CardTags.STARTER_STRIKE);else tags.add(CardTags.STARTER_DEFEND);}
    }
    private int amount(String field){return row.i(field)+(upgraded && row.i(field)>0?row.i("upgradeMagic"):0);}
    private void describe(){
        StringBuilder s=new StringBuilder();
        if(baseDamage>0){s.append("Deal !D! damage");if(row.i("hits")>1)s.append(" ").append(row.i("hits")).append(" times");s.append(".");}
        if(baseBlock>0)s.append(" NL Gain !B! Block.");
        for(String field:new String[]{"draw","energy","strength","dexterity","weak","vulnerable"})if(amount(field)>0){
            int n=amount(field);s.append(" NL ");
            if(field.equals("draw"))s.append("Draw ").append(n).append(" card(s).");
            else if(field.equals("energy"))s.append("Gain ").append(n).append(" Energy.");
            else s.append(field.equals("weak")||field.equals("vulnerable")?"Apply ":"Gain ").append(n).append(" ").append(Character.toUpperCase(field.charAt(0))).append(field.substring(1)).append(".");
        }
        if(exhaust)s.append(" NL Exhaust.");rawDescription=s.toString().replaceFirst("^ NL ","");initializeDescription();
    }
    public void use(AbstractPlayer p,AbstractMonster m){
        if(baseDamage>0)for(int i=0;i<row.i("hits");i++)AbstractDungeon.actionManager.addToBottom(new DamageAction(m,new DamageInfo(p,damage,damageTypeForTurn),AttackEffect.SLASH_DIAGONAL));
        if(baseBlock>0)AbstractDungeon.actionManager.addToBottom(new GainBlockAction(p,p,block));
        if(amount("draw")>0)AbstractDungeon.actionManager.addToBottom(new DrawCardAction(p,amount("draw")));
        if(amount("energy")>0)AbstractDungeon.actionManager.addToBottom(new GainEnergyAction(amount("energy")));
        if(amount("strength")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new StrengthPower(p,amount("strength")),amount("strength")));
        if(amount("dexterity")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new DexterityPower(p,amount("dexterity")),amount("dexterity")));
        if(amount("weak")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,p,new WeakPower(m,amount("weak"),false),amount("weak")));
        if(amount("vulnerable")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,p,new VulnerablePower(m,amount("vulnerable"),false),amount("vulnerable")));
    }
    public void upgrade(){if(!upgraded){upgradeName();if(baseDamage>0)upgradeDamage(row.i("upgradeDamage"));if(baseBlock>0)upgradeBlock(row.i("upgradeBlock"));describe();}}
    public AbstractCard makeCopy(){return new HeirCard(key);}
}
