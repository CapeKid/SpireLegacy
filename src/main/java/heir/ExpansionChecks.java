package heir;
import java.util.*;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.actions.AbstractGameAction;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.helpers.CardLibrary;
import com.megacrit.cardcrawl.monsters.AbstractMonster;
import com.megacrit.cardcrawl.powers.*;

/** Exercise the real library, class pools, constructors and queued card actions. */
public final class ExpansionChecks {
    private static int checks;
    private static void check(boolean pass,String why){checks++;if(!pass)throw new AssertionError(why);}
    public static void run()throws Exception {
        if(!HeirMod.isHeir()||AbstractDungeon.getMonsters()==null)throw new IllegalStateException("Start an isolated heir fight first");
        Profile.Heir heir=HeirMod.heir();String cls=heir.classId;List<String> traits=new ArrayList<>(heir.traits);
        List<AbstractPower> powers=new ArrayList<>(AbstractDungeon.player.powers);ArrayList<AbstractGameAction> actions=new ArrayList<>(AbstractDungeon.actionManager.actions);
        AbstractMonster enemy=AbstractDungeon.getMonsters().monsters.get(0);ArrayList<AbstractPower> enemyPowers=new ArrayList<>(enemy.powers);
        int block=AbstractDungeon.player.currentBlock,hp=enemy.currentHealth,max=enemy.maxHealth;
        try{
            checks=0;heir.traits.clear();AbstractDungeon.player.powers.clear();enemy.powers.clear();enemy.maxHealth=100;enemy.currentHealth=100;
            int originalTotal=0;for(AbstractCard card:CardLibrary.cards.values())if(card.color==AbstractCard.CardColor.RED)originalTotal++;
            check(originalTotal==75,"Owned Ironclad library baseline");
            for(Data.Row target:Data.rows("pool_targets")){
                heir.classId=target.s("id");com.megacrit.cardcrawl.core.CardCrawlGame.dungeon.initializeCardPools();
                check(AbstractDungeon.commonCardPool.size()==target.i("common"),"Common class pool");
                check(AbstractDungeon.uncommonCardPool.size()==target.i("uncommon"),"Uncommon class pool");
                check(AbstractDungeon.rareCardPool.size()==target.i("rare"),"Rare class pool");
                int count=0;
                for(Data.Row row:Data.rows("cards"))if(row.s("classId").equals(heir.classId)){
                    count++;HeirCard card=new HeirCard(row.s("id"));HeirCard copy=(HeirCard)card.makeCopy();
                    check(card.cardID.equals(copy.cardID)&&card.cost==copy.cost&&card.baseDamage==copy.baseDamage&&card.baseBlock==copy.baseBlock,"Copy lost base card behavior");
                    check(card.selfRetain==row.b("retain"),"Prepared card retention");
                    check(!card.rawDescription.isEmpty(),"Missing rules text");
                    for(boolean upgraded:new boolean[]{false,true}){
                        if(upgraded)card.upgrade();
                        AbstractDungeon.actionManager.actions.clear();AbstractDungeon.player.currentBlock=12;
                        card.calculateCardDamage(enemy);card.use(AbstractDungeon.player,enemy);
                        check(!AbstractDungeon.actionManager.actions.isEmpty(),"Card has no combat effect: "+card.cardID);
                        for(AbstractGameAction action:AbstractDungeon.actionManager.actions)check(action!=null,"Invalid action");
                        if(row.i("poison")>0&&!row.b("aoe"))check(card.target==AbstractCard.CardTarget.ENEMY,"Poison Skill needs an enemy target");
                    }
                }
                check(count==originalTotal,"Class is smaller than the regular hero");
            }
            HeirCard training=new HeirCard("knight_veteran_training");training.upgrade();check(training.cost==0&&training.magicNumber==1,"Draw-power upgrade keeps recurring HP cost");
            HeirCard wall=new HeirCard("knight_living_fortress");wall.upgrade();check(wall.cost==2,"Fortress upgrade reduces its cost");
            heir.classId="knight";AbstractDungeon.player.currentBlock=12;HeirCard slam=new HeirCard("knight_shield_slam");slam.calculateCardDamage(enemy);check(slam.damage==12,"Shield Slam uses current Block");
            HeirCard counter=new HeirCard("knight_counter_lunge");counter.calculateCardDamage(enemy);check(counter.damage==15,"Counter Lunge rewards Block");
            enemy.currentHealth=50;HeirCard execute=new HeirCard("knight_executioner_axe");execute.calculateCardDamage(enemy);check(execute.damage==26,"Execution threshold bonus");
            enemy.powers.add(new VulnerablePower(enemy,1,false));HeirCard ambush=new HeirCard("ranger_ambush");ambush.calculateCardDamage(enemy);check(ambush.damage==24,"Marked damage uses native Vulnerable scaling");enemy.powers.clear();
            ArrayList<AbstractCard> played=new ArrayList<>(AbstractDungeon.actionManager.cardsPlayedThisTurn);
            ArrayList<CardQueueItem> queue=new ArrayList<>(AbstractDungeon.actionManager.cardQueue);
            try{
                AbstractDungeon.actionManager.cardsPlayedThisTurn.clear();AbstractDungeon.actionManager.cardQueue.clear();
                AbstractDungeon.actionManager.cardsPlayedThisTurn.add(new HeirCard("ranger_strike"));AbstractDungeon.actionManager.cardsPlayedThisTurn.add(new HeirCard("ranger_strike"));
                HeirCard finish=new HeirCard("ranger_finishing_flurry");finish.calculateCardDamage(enemy);check(finish.damage==8,"Flurry previews earlier Attacks only");
                AbstractDungeon.actionManager.cardsPlayedThisTurn.add(finish);AbstractDungeon.actionManager.cardQueue.add(new CardQueueItem(finish,enemy,3));
                finish.calculateCardDamage(enemy);check(finish.damage==8,"Native queue must not count Flurry itself");
            }finally{AbstractDungeon.actionManager.cardsPlayedThisTurn.clear();AbstractDungeon.actionManager.cardsPlayedThisTurn.addAll(played);AbstractDungeon.actionManager.cardQueue.clear();AbstractDungeon.actionManager.cardQueue.addAll(queue);}
            com.megacrit.cardcrawl.monsters.MonsterGroup realGroup=AbstractDungeon.getCurrRoom().monsters;
            try{
                AbstractMonster left=new com.megacrit.cardcrawl.monsters.exordium.Cultist(-180,0),right=new com.megacrit.cardcrawl.monsters.exordium.Cultist(180,0);
                AbstractDungeon.getCurrRoom().monsters=new com.megacrit.cardcrawl.monsters.MonsterGroup(new AbstractMonster[]{left,right});
                heir.classId="ranger";ClassPower rhythm=new ClassPower("ranger");rhythm.amount=2;
                HeirCard scatter=new HeirCard("ranger_scattershot");AbstractDungeon.actionManager.actions.clear();
                rhythm.onUseCard(scatter,new com.megacrit.cardcrawl.actions.utility.UseCardAction(scatter,left));
                Set<com.megacrit.cardcrawl.core.AbstractCreature> marked=new HashSet<>();
                for(AbstractGameAction action:AbstractDungeon.actionManager.actions)if(action instanceof com.megacrit.cardcrawl.actions.common.ApplyPowerAction)marked.add(action.target);
                check(rhythm.amount==0,"Third area Attack consumes Hunter Rhythm");check(marked.contains(left)&&marked.contains(right),"Third area Attack marks both enemies");
                rhythm.amount=2;AbstractDungeon.actionManager.actions.clear();AbstractCard foreign=new com.megacrit.cardcrawl.cards.red.Cleave();foreign.calculateCardDamage(left);
                rhythm.onUseCard(foreign,new com.megacrit.cardcrawl.actions.utility.UseCardAction(foreign,right));marked.clear();
                for(AbstractGameAction action:AbstractDungeon.actionManager.actions)if(action instanceof com.megacrit.cardcrawl.actions.common.ApplyPowerAction)marked.add(action.target);
                check(marked.contains(left)&&marked.contains(right),"Prismatic Shard area Attacks also mark both enemies");
            }finally{AbstractDungeon.getCurrRoom().monsters=realGroup;AbstractDungeon.actionManager.actions.clear();}
            HeirCard recover=new HeirCard("mage_memory_prism");recover.upgrade();AbstractDungeon.actionManager.actions.clear();recover.use(AbstractDungeon.player,enemy);
            boolean improvedRetrieval=false;for(AbstractGameAction action:AbstractDungeon.actionManager.actions)if(action instanceof CardEffectAction)improvedRetrieval=action.amount==2;
            check(recover.cost==1&&improvedRetrieval,"Retrieval upgrades preserve Energy cost and return more cards");
            HeirCard costly=new HeirCard("knight_blood_price");costly.upgrade();AbstractDungeon.actionManager.actions.clear();costly.use(AbstractDungeon.player,enemy);
            boolean stableHpCost=false;for(AbstractGameAction action:AbstractDungeon.actionManager.actions)if(action instanceof com.megacrit.cardcrawl.actions.common.LoseHPAction)stableHpCost=action.amount==3;
            check(stableHpCost,"Upgrading Blood Price must not increase HP cost");
            Files.write(HeirMod.root.resolve("expansion-checks.json"),("{\"combatAssertions\":"+checks+",\"cardsExercised\":225,\"regularHeroCards\":"+originalTotal+"}").getBytes(StandardCharsets.UTF_8));
        }finally{
            heir.classId=cls;heir.traits.clear();heir.traits.addAll(traits);AbstractDungeon.player.powers.clear();AbstractDungeon.player.powers.addAll(powers);AbstractDungeon.player.currentBlock=block;enemy.powers.clear();enemy.powers.addAll(enemyPowers);enemy.currentHealth=hp;enemy.maxHealth=max;
            AbstractDungeon.actionManager.actions.clear();AbstractDungeon.actionManager.actions.addAll(actions);com.megacrit.cardcrawl.core.CardCrawlGame.dungeon.initializeCardPools();AbstractDungeon.player.hand.applyPowers();
        }
    }
}
