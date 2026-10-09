package heir;
import java.util.*;
import java.nio.file.*;
import com.megacrit.cardcrawl.core.*;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.shop.Merchant;
import com.megacrit.cardcrawl.actions.utility.UseCardAction;

/** Opt-in checks using actual native card pools, merchants and combat powers. */
public final class Issue6Checks {
    private static int checks;
    private static void check(boolean yes,String message){checks++;if(!yes)throw new IllegalStateException(message);}
    public static void run()throws Exception {
        if(!HeirMod.isHeir()||AbstractDungeon.getMonsters()==null)throw new IllegalStateException("Start an isolated heir combat first");
        Profile.Heir heir=HeirMod.heir();String savedClass=heir.classId;List<String> savedTraits=new ArrayList<>(heir.traits);checks=0;
        List<com.megacrit.cardcrawl.powers.AbstractPower> savedPowers=new ArrayList<>(AbstractDungeon.player.powers);
        try{
            AbstractDungeon.player.powers.clear();
            heir.traits.clear();
            for(Data.Row cls:Data.rows("classes")){
                heir.classId=cls.s("id");CardCrawlGame.dungeon.initializeCardPools();
                for(AbstractCard.CardRarity rarity:Arrays.asList(AbstractCard.CardRarity.COMMON,AbstractCard.CardRarity.UNCOMMON,AbstractCard.CardRarity.RARE))for(AbstractCard.CardType kind:Arrays.asList(AbstractCard.CardType.ATTACK,AbstractCard.CardType.SKILL,AbstractCard.CardType.POWER))for(int i=0;i<20;i++){
                    AbstractCard card=AbstractDungeon.getCardFromPool(rarity,kind,true);check(card instanceof HeirCard,"Native pool fell outside the mod");check(card.type==kind,"Native pool returned wrong type");check(CardPools.contains(heir.classId,((HeirCard)card).key),"Wrong class shop card");if(kind!=AbstractCard.CardType.POWER)check(card.rarity==rarity,"Rarity fallback hides missing tier");
                }
                Merchant merchant=new Merchant();java.lang.reflect.Field field=Merchant.class.getDeclaredField("cards1");field.setAccessible(true);List<AbstractCard> stock=(List<AbstractCard>)field.get(merchant);
                check(stock.size()==5,"Merchant stock count");check(!stock.get(0).cardID.equals(stock.get(1).cardID),"Duplicate shop attacks");check(!stock.get(2).cardID.equals(stock.get(3).cardID),"Duplicate shop skills");
                for(AbstractCard card:stock)check(card instanceof HeirCard,"Merchant colored stock left mod pool");
            }
            ClassPower knight=new ClassPower("knight");UseCardAction skill=new UseCardAction(new HeirCard("knight_guard"));knight.onUseCard(new HeirCard("knight_guard"),skill);check(knight.atDamageGive(10,DamageInfo.DamageType.NORMAL)==14,"Knight defensive counter");knight.onUseCard(new HeirCard("knight_strike"),new UseCardAction(new HeirCard("knight_strike")));check(knight.amount==0,"Counter consumed");
            ClassPower mage=new ClassPower("mage");for(int i=0;i<4;i++)mage.onUseCard(new HeirCard("mage_guard"),skill);check(mage.amount==3&&mage.atDamageGive(10,DamageInfo.DamageType.NORMAL)==16,"Mage charges capped and scaled");mage.onUseCard(new HeirCard("mage_strike"),new UseCardAction(new HeirCard("mage_strike")));check(mage.amount==0,"Mage charges consumed");
            ClassPower ranger=new ClassPower("ranger");ranger.onUseCard(new HeirCard("ranger_strike"),new UseCardAction(new HeirCard("ranger_strike")));ranger.onUseCard(new HeirCard("ranger_strike"),new UseCardAction(new HeirCard("ranger_strike")));check(ranger.amount==2,"Ranger chain count");ranger.atStartOfTurn();check(ranger.amount==0,"Ranger chain resets per turn");
            heir.traits.add("large");heir.traits.add("weapon");HeirCard heavy=new HeirCard("knight_strike");check(heavy.baseDamage==11&&heavy.baseBlock==2,"Stacked offensive DNA");heavy.upgrade();check(heavy.baseDamage==14,"DNA retained on upgrade");
            heir.traits.clear();heir.traits.add("magic");check(new HeirCard("mage_guard").rawDescription.contains("Draw 1"),"Bookish starter draw printed");check(new HeirCard("mage_strike").baseDamage==4,"Bookish attack tradeoff");
            heir.traits.clear();heir.traits.add("vampire");check(new HeirCard("ranger_strike").rawDescription.contains("Heal 1"),"Vampirism printed");
            heir.traits.clear();heir.traits.add("cantattack");HeirCard mercy=new HeirCard("knight_strike");check(mercy.type==AbstractCard.CardType.SKILL&&mercy.target==AbstractCard.CardTarget.ENEMY&&mercy.baseDamage==0&&mercy.baseBlock==4&&mercy.poison()==2,"Pacifist Mercy conversion");check(new TraitPower(Data.row("traits","cantattack")).canPlayCard(mercy),"Pacifist permits Mercy");mercy.upgrade();check(mercy.baseBlock==7&&mercy.poison()==3,"Mercy upgrade");check(new HeirCard("cleave").type==AbstractCard.CardType.ATTACK,"Reward card not converted");
            Files.write(HeirMod.root.resolve("issue6-checks.json"),("{\"nativeShopAndClassChecks\":"+checks+",\"merchantsVerified\":3}").getBytes(java.nio.charset.StandardCharsets.UTF_8));
        }finally{heir.classId=savedClass;heir.traits.clear();heir.traits.addAll(savedTraits);AbstractDungeon.player.powers.clear();AbstractDungeon.player.powers.addAll(savedPowers);AbstractDungeon.player.hand.applyPowers();CardCrawlGame.dungeon.initializeCardPools();}
    }
}
