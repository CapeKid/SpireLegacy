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
        selfRetain=row.b("retain");isEthereal=row.b("ethereal");isInnate=row.b("innate");isMultiDamage=row.b("aoe");
        if(row.b("aoe")||row.s("special").equals("random_attack"))target=CardTarget.ALL_ENEMY;
        else if(row.i("poison")>0||row.i("weak")>0||row.i("vulnerable")>0||row.s("special").equals("double_poison"))target=CardTarget.ENEMY;
        if(genes.mercy){type=CardType.SKILL;target=CardTarget.ENEMY;name="Mercy: "+name;initializeTitle();}describe();
        if(row.s("rarity").equals("BASIC")){if(type==CardType.ATTACK)tags.add(CardTags.STARTER_STRIKE);else tags.add(CardTags.STARTER_DEFEND);}
    }
    private int amount(String field){if(field.equals("discard"))return row.i(field)+(upgraded?row.i("upgradeDiscard"):0);return row.i(field)+(field.equals("draw")?genes.draw:0)+(upgraded && !field.equals("hpLoss") && row.i(field)>0?row.i("upgradeMagic"):0);}
    private int specialAmount(){return row.i("specialAmount")+(upgraded?row.i("upgradeSpecial"):0);}
    public int poison(){return genes.poison+(genes.mercy&&upgraded?1:0);}
    public java.util.List<basemod.helpers.TooltipInfo> getCustomTooltips(){java.util.List<basemod.helpers.TooltipInfo> tips=new java.util.ArrayList<>();String title=CardPools.shared(key)?"Family card":Data.row("classes",row.s("classId")).s("name")+" card";String poolText=CardPools.shared(key)?"Shared by Knight, Mage and Ranger. Counts toward each class's 75-card library.":CardPools.active(key)?"Unique to this class. Its 75-card library contains 30 shared and 45 unique cards.":"Legacy card: retained for existing saves; outside the current class reward and shop pools.";tips.add(new basemod.helpers.TooltipInfo(title,poolText));for(String id:genes.traits)tips.add(new basemod.helpers.TooltipInfo(Data.row("traits",id).s("name"),Data.row("starter_genes",id).s("summary")));if(row.s("rarity").equals("BASIC")){Data.Row mechanic=Data.row("class_mechanics",row.s("classId"));tips.add(new basemod.helpers.TooltipInfo(mechanic.s("name"),mechanic.s("summary")));}return tips;}
    private void describe(){
        StringBuilder s=new StringBuilder();
        if(baseDamage>0||row.s("special").equals("block_damage")||row.s("special").equals("chain_damage")){s.append("Deal !D! damage");if(row.b("aoe"))s.append(" to ALL enemies");if(row.i("hits")>1)s.append(" ").append(row.i("hits")).append(" times");s.append(".");}
        if(baseBlock>0)s.append(" NL Gain !B! Block.");
        if(poison()>0)s.append(" NL Apply ").append(poison()).append(" Poison.");
        if(genes.heal>0)s.append(" NL Heal ").append(genes.heal).append(" HP.");
        for(String field:new String[]{"hpLoss","heal","poison","vigor","thorns","plated","nextEnergy","nextDraw","nextBlock"})if(amount(field)>0){
            int n=amount(field);s.append(" NL ");
            if(field.equals("hpLoss"))s.append("Lose ").append(n).append(" HP.");
            if(field.equals("heal"))s.append("Heal ").append(n).append(" HP.");
            if(field.equals("poison"))s.append("Apply ").append(n).append(" Poison").append(row.b("aoe")?" to ALL enemies.":".");
            if(field.equals("vigor"))s.append("Your next Attack deals ").append(n).append(" additional damage.");
            if(field.equals("thorns"))s.append("Gain ").append(n).append(" Thorns.");
            if(field.equals("plated"))s.append("Gain ").append(n).append(" Plated Armor.");
            if(field.equals("nextEnergy"))s.append("Next turn, gain ").append(n).append(" Energy.");
            if(field.equals("nextDraw"))s.append("Next turn, draw ").append(n).append(" card(s).");
            if(field.equals("nextBlock"))s.append("Next turn, gain ").append(n).append(" Block.");
        }
        if(row.s("special").equals("scry"))s.append(" NL ").append(Data.row("special_effects","scry").s("summary").replace("{n}",String.valueOf(specialAmount())));
        for(String field:new String[]{"draw","energy","strength","dexterity","weak","vulnerable"})if(amount(field)>0){
            int n=amount(field);s.append(" NL ");
            if(field.equals("draw"))s.append("Draw ").append(n).append(" card(s).");
            else if(field.equals("energy"))s.append("Gain ").append(n).append(" Energy.");
            else s.append(field.equals("weak")||field.equals("vulnerable")?"Apply ":"Gain ").append(n).append(" ").append(Character.toUpperCase(field.charAt(0))).append(field.substring(1)).append(row.b("aoe")&&(field.equals("weak")||field.equals("vulnerable"))?" to ALL enemies.":".");
        }
        if(amount("discard")>0)s.append(" NL Discard ").append(amount("discard")).append(" card(s).");
        if(row.s("power").equals("reservoir"))s.append("Each Arcane Charge adds !M! extra attack damage.");
        if(row.s("power").equals("quiver"))s.append("Hunter Rhythm draws !M! additional card(s).");
        if(!row.s("power").equals("none")&&!row.s("power").equals("quiver")&&!row.s("power").equals("reservoir"))s.append(" NL ").append(Data.row("power_effects",row.s("power")).s("summary"));
        if(row.s("special").equals("block_damage")||row.s("special").equals("chain_damage")){
            s.append(" NL ").append(row.s("special").equals("block_damage")?"Base damage equals your current Block.":"Base damage is "+specialAmount()+" per OTHER Attack played this turn.");
        }else if(!row.s("special").equals("none")&&!row.s("special").equals("scry"))s.append(" NL ").append(Data.row("special_effects",row.s("special")).s("summary").replace("{n}",String.valueOf(specialAmount())).replace("the target",row.b("aoe")?"ALL enemies":"the target"));
        if(selfRetain)s.append(" NL Retain.");if(isEthereal)s.append(" NL Ethereal.");if(isInnate)s.append(" NL Innate.");
        if(exhaust)s.append(" NL Exhaust.");rawDescription=s.toString().replaceFirst("^ NL ","");initializeDescription();
    }
    public void use(AbstractPlayer p,AbstractMonster m){
        if(baseDamage>0||row.s("special").equals("block_damage")||row.s("special").equals("chain_damage"))for(int i=0;i<row.i("hits");i++){
            if(row.b("aoe"))AbstractDungeon.actionManager.addToBottom(new DamageAllEnemiesAction(p,multiDamage,damageTypeForTurn,AttackEffect.SLASH_DIAGONAL));
            else if(row.s("special").equals("random_attack"))AbstractDungeon.actionManager.addToBottom(new DamageRandomEnemyAction(new DamageInfo(p,damage,damageTypeForTurn),AttackEffect.SLASH_DIAGONAL));
            else AbstractDungeon.actionManager.addToBottom(new DamageAction(m,new DamageInfo(p,damage,damageTypeForTurn),AttackEffect.SLASH_DIAGONAL));
        }
        if(baseBlock>0)AbstractDungeon.actionManager.addToBottom(new GainBlockAction(p,p,block));
        if(poison()>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,p,new PoisonPower(m,p,poison()),poison()));
        if(genes.heal>0)AbstractDungeon.actionManager.addToBottom(new HealAction(p,p,genes.heal));
        if(amount("hpLoss")>0)AbstractDungeon.actionManager.addToBottom(new LoseHPAction(p,p,amount("hpLoss")));
        if(amount("heal")>0)AbstractDungeon.actionManager.addToBottom(new HealAction(p,p,amount("heal")));
        if(amount("vigor")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new com.megacrit.cardcrawl.powers.watcher.VigorPower(p,amount("vigor")),amount("vigor")));
        if(amount("thorns")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new ThornsPower(p,amount("thorns")),amount("thorns")));
        if(amount("plated")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new PlatedArmorPower(p,amount("plated")),amount("plated")));
        if(amount("nextEnergy")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new EnergizedPower(p,amount("nextEnergy")),amount("nextEnergy")));
        if(amount("nextDraw")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new DrawCardNextTurnPower(p,amount("nextDraw")),amount("nextDraw")));
        if(amount("nextBlock")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new NextTurnBlockPower(p,amount("nextBlock")),amount("nextBlock")));
        if(row.s("special").equals("scry"))AbstractDungeon.actionManager.addToBottom(new CardEffectAction("scry",specialAmount(),m,false));
        if(amount("draw")>0)AbstractDungeon.actionManager.addToBottom(new DrawCardAction(p,amount("draw")));
        if(amount("discard")>0)AbstractDungeon.actionManager.addToBottom(new DiscardAction(p,p,amount("discard"),false));
        if(amount("energy")>0)AbstractDungeon.actionManager.addToBottom(new GainEnergyAction(amount("energy")));
        if(amount("strength")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new StrengthPower(p,amount("strength")),amount("strength")));
        if(amount("dexterity")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,new DexterityPower(p,amount("dexterity")),amount("dexterity")));
        java.util.List<AbstractMonster> targets=new java.util.ArrayList<>();if(row.b("aoe"))targets.addAll(AbstractDungeon.getMonsters().monsters);else if(m!=null)targets.add(m);
        for(AbstractMonster enemy:targets)if(!enemy.isDeadOrEscaped()){
            if(amount("weak")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(enemy,p,new WeakPower(enemy,amount("weak"),false),amount("weak")));
            if(amount("vulnerable")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(enemy,p,new VulnerablePower(enemy,amount("vulnerable"),false),amount("vulnerable")));
            if(amount("poison")>0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(enemy,p,new PoisonPower(enemy,p,amount("poison")),amount("poison")));
        }
        if(!row.s("power").equals("none"))AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(p,p,CardPowers.create(row.s("power"),p,magicNumber),magicNumber));
        if(!row.s("special").equals("none")&&!java.util.Arrays.asList("block_damage","chain_damage","random_attack","execute","guard_bonus","marked_bonus","scry","exhaust_damage","retain_damage","retain_block","discard_draw","discard_energy","discard_block").contains(row.s("special")))AbstractDungeon.actionManager.addToBottom(new CardEffectAction(row.s("special"),specialAmount(),m,row.b("aoe")));
    }
    public void triggerOnManualDiscard(){
        if(AbstractDungeon.actionManager.turnHasEnded)return;
        String effect=row.s("special");AbstractPlayer p=AbstractDungeon.player;
        if(effect.equals("discard_draw"))addToBot(new DrawCardAction(p,specialAmount()));
        if(effect.equals("discard_energy"))addToBot(new GainEnergyAction(specialAmount()));
        if(effect.equals("discard_block"))addToBot(new GainBlockAction(p,p,specialAmount()));
    }
    public void onRetained(){
        if(row.s("special").equals("retain_damage"))baseDamage+=specialAmount();
        if(row.s("special").equals("retain_block"))baseBlock+=specialAmount();
        applyPowers();
    }
    private int conditionalBase(AbstractMonster m){
        String special=row.s("special");
        if(special.equals("exhaust_damage")&&AbstractDungeon.player!=null)return baseDamage+Math.min(10,AbstractDungeon.player.exhaustPile.size())*specialAmount();
        if(special.equals("block_damage"))return AbstractDungeon.player==null?0:AbstractDungeon.player.currentBlock;
        if(special.equals("chain_damage")){
            int count=0;
            if(AbstractDungeon.actionManager!=null){
                for(AbstractCard card:AbstractDungeon.actionManager.cardsPlayedThisTurn)if(card.type==CardType.ATTACK)count++;
                java.util.List<AbstractCard> played=AbstractDungeon.actionManager.cardsPlayedThisTurn;
                // The native queue records the current Attack before recalculating damage.
                if(!played.isEmpty()&&played.get(played.size()-1)==this&&!AbstractDungeon.actionManager.cardQueue.isEmpty()&&AbstractDungeon.actionManager.cardQueue.get(0).card==this)count--;
            }
            return count*specialAmount();
        }
        if(special.equals("guard_bonus")&&AbstractDungeon.player!=null&&AbstractDungeon.player.currentBlock>0)return baseDamage+specialAmount();
        if(special.equals("execute")&&m!=null&&m.currentHealth*2<=m.maxHealth)return baseDamage*specialAmount();
        if(special.equals("marked_bonus")&&m!=null&&(m.hasPower("Vulnerable")||m.hasPower("Poison")))return baseDamage+specialAmount();
        return baseDamage;
    }
    public void applyPowers(){int original=baseDamage;baseDamage=conditionalBase(null);super.applyPowers();baseDamage=original;isDamageModified=isDamageModified||damage!=baseDamage;}
    public void calculateCardDamage(AbstractMonster m){int original=baseDamage;baseDamage=conditionalBase(m);super.calculateCardDamage(m);baseDamage=original;isDamageModified=isDamageModified||damage!=baseDamage;}
    public void upgrade(){if(!upgraded){upgradeName();if(baseDamage>0)upgradeDamage(row.i("upgradeDamage"));if(baseBlock>0)upgradeBlock(genes.mercy?3:row.i("upgradeBlock"));if(baseMagicNumber>0)upgradeMagicNumber(row.i("upgradeMagic"));if(row.b("upgradeCost"))upgradeBaseCost(Math.max(0,cost-1));describe();}}
    public AbstractCard makeCopy(){return new HeirCard(key);}
}
