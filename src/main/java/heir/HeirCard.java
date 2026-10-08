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
    public final StarterGenes genes;
    public HeirCard(String key){
        super("heir:"+key,Data.row("cards",key).s("name"),Data.row("card_art",Data.row("cards",key).s("art")).s("path"),Data.row("cards",key).i("cost"),"",CardType.valueOf(Data.row("cards",key).s("type")),HeirMod.Enums.HEIR_COLOR,CardRarity.valueOf(Data.row("cards",key).s("rarity")),Data.row("cards",key).s("type").equals("ATTACK")?CardTarget.ENEMY:CardTarget.SELF);
        this.key=key;row=Data.row("cards",key);genes=new StarterGenes(row,HeirMod.heir().traits);baseDamage=genes.damage;baseBlock=genes.block;baseMagicNumber=magicNumber=row.i("magic");exhaust=row.b("exhaust");
        if(genes.mercy){type=CardType.SKILL;target=CardTarget.ENEMY;name="Mercy: "+name;initializeTitle();}describe();
        if(row.s("rarity").equals("BASIC")){if(type==CardType.ATTACK)tags.add(CardTags.STARTER_STRIKE);else tags.add(CardTags.STARTER_DEFEND);}
    }
    private int amount(String field){return row.i(field)+(field.equals("draw")?genes.draw:0)+(upgraded && row.i(field)>0?row.i("upgradeMagic"):0);}
    public int poison(){return genes.poison+(genes.mercy&&upgraded?1:0);}
    public java.util.List<basemod.helpers.TooltipInfo> getCustomTooltips(){java.util.List<basemod.helpers.TooltipInfo> tips=new java.util.ArrayList<>();for(String id:genes.traits)tips.add(new basemod.helpers.TooltipInfo(Data.row("traits",id).s("name"),Data.row("starter_genes",id).s("summary")));if(row.s("rarity").equals("BASIC")){Data.Row mechanic=Data.row("class_mechanics",HeirMod.heir().classId);tips.add(new basemod.helpers.TooltipInfo(mechanic.s("name"),mechanic.s("summary")));}return tips;}
    private void describe(){
        StringBuilder s=new StringBuilder();
        if(baseDamage>0){s.append("Deal !D! damage");if(row.i("hits")>1)s.append(" ").append(row.i("hits")).append(" times");s.append(".");}
        if(baseBlock>0)s.append(" NL Gain !B! Block.");
        if(poison()>0)s.append(" NL Apply ").append(poison()).append(" Poison.");
        if(genes.heal>0)s.append(" NL Heal ").append(genes.heal).append(" HP.");
        for(String field:new String[]{"draw","energy","strength","dexterity","weak","vulnerable"})if(amount(field)>0){
            int n=amount(field);s.append(" NL ");
            if(field.equals("draw"))s.append("Draw ").append(n).append(" card(s).");
            else if(field.equals("energy"))s.append("Gain ").append(n).append(" Energy.");
            else s.append(field.equals("weak")||field.equals("vulnerable")?"Apply ":"Gain ").append(n).append(" ").append(Character.toUpperCase(field.charAt(0))).append(field.substring(1)).append(".");
        }
        if(row.i("discard")>0)s.append(" NL Discard ").append(row.i("discard")).append(" card.");
        if(row.s("power").equals("reservoir"))s.append("Each Arcane Charge adds !M! extra attack damage.");
        if(row.s("power").equals("quiver"))s.append("Hunter Rhythm draws !M! additional card(s).");
        if(exhaust)s.append(" NL Exhaust.");rawDescription=s.toString().replaceFirst("^ NL ","");initializeDescription();
    }
    public void use(AbstractPlayer p,AbstractMonster m){
        if(baseDamage>0)for(int i=0;i<row.i("hits");i++)AbstractDungeon.actionManager.addToBottom(new DamageAction(m,new DamageInfo(p,damage,damageTypeForTurn),AttackEffect.SLASH_DIAGONAL));
        if(baseBlock>0)AbstractDungeon.actionManager.addToBottom(new GainBlockAction(p,p,block));
        if(poison()>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,p,new PoisonPower(m,p,poison()),poison()));
        if(genes.heal>0)AbstractDungeon.actionManager.addToBottom(new HealAction(p,p,genes.heal));
        if(amount("draw")>0)AbstractDungeon.actionManager.addToBottom(new DrawCardAction(p,amount("draw")));
        if(row.i("discard")>0)AbstractDungeon.actionManager.addToBottom(new DiscardAction(p,p,row.i("discard"),false));
        if(amount("energy")>0)AbstractDungeon.actionManager.addToBottom(new GainEnergyAction(amount("energy")));
        if(amount("strength")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new StrengthPower(p,amount("strength")),amount("strength")));
        if(amount("dexterity")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new DexterityPower(p,amount("dexterity")),amount("dexterity")));
        if(amount("weak")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,p,new WeakPower(m,amount("weak"),false),amount("weak")));
        if(amount("vulnerable")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,p,new VulnerablePower(m,amount("vulnerable"),false),amount("vulnerable")));
        if(!row.s("power").equals("none"))AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new HeirEnginePower(row.s("power"),magicNumber),magicNumber));
    }
    public void upgrade(){if(!upgraded){upgradeName();if(baseDamage>0)upgradeDamage(row.i("upgradeDamage"));if(baseBlock>0)upgradeBlock(genes.mercy?3:row.i("upgradeBlock"));if(baseMagicNumber>0)upgradeMagicNumber(row.i("upgradeMagic"));describe();}}
    public AbstractCard makeCopy(){return new HeirCard(key);}
}
