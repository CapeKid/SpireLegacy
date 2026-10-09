package heir;
import com.megacrit.cardcrawl.actions.*;
import com.megacrit.cardcrawl.actions.common.*;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.characters.AbstractPlayer;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.monsters.AbstractMonster;
import com.megacrit.cardcrawl.powers.*;
import com.megacrit.cardcrawl.ui.panels.EnergyPanel;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;

/** Opt-in disposable scenarios; wait for the native action queue after every step. */
public final class BuildChecks {
    private static int stage=-1,checks,waitFrames,hp;
    private static AbstractPlayer p;
    private static AbstractMonster enemy;
    private static HeirCard held,ward;
    private static void check(boolean ok,String message){checks++;if(!ok)throw new AssertionError(message);}
    private static void queue(AbstractGameAction a){AbstractDungeon.actionManager.addToBottom(a);}
    private static void hand(String... ids){p.hand.clear();for(String id:ids)p.hand.addToHand(new HeirCard(id));p.hand.refreshHandLayout();p.hand.applyPowers();}
    private static void play(String id){HeirCard c=new HeirCard(id);p.hand.addToHand(c);p.hand.refreshHandLayout();p.hand.applyPowers();EnergyPanel.totalCount=10;AbstractDungeon.actionManager.addCardQueueItem(new CardQueueItem(c,enemy,10));}
    private static void playUpgraded(String id){HeirCard c=new HeirCard(id);c.upgrade();p.hand.addToHand(c);p.hand.refreshHandLayout();p.hand.applyPowers();EnergyPanel.totalCount=10;AbstractDungeon.actionManager.addCardQueueItem(new CardQueueItem(c,enemy,10));}
    private static void reset(){
        p.powers.clear();p.relics.clear();p.hand.clear();p.drawPile.clear();p.discardPile.clear();p.exhaustPile.clear();p.limbo.clear();p.currentBlock=0;
        enemy.powers.clear();enemy.maxHealth=enemy.currentHealth=1000;enemy.isDying=false;enemy.isDead=false;
        AbstractDungeon.actionManager.cardsPlayedThisTurn.clear();AbstractDungeon.actionManager.turnHasEnded=false;GameActionManager.totalDiscardedThisTurn=0;EnergyPanel.totalCount=10;
        for(int i=0;i<15;i++)p.drawPile.addToTop(new HeirCard("ranger_strike"));
    }
    public static void start(){
        if(!HeirMod.isHeir()||AbstractDungeon.getCurrRoom().phase!=com.megacrit.cardcrawl.rooms.AbstractRoom.RoomPhase.COMBAT)throw new IllegalStateException("Start a disposable heir fight first");
        p=AbstractDungeon.player;enemy=AbstractDungeon.getMonsters().monsters.get(0);checks=0;waitFrames=0;stage=0;
    }
    public static void update(){
        if(stage<0)return;
        try{
            if(!AbstractDungeon.actionManager.actions.isEmpty()||!AbstractDungeon.actionManager.cardQueue.isEmpty()||AbstractDungeon.actionManager.currentAction!=null||AbstractDungeon.isScreenUp){if(++waitFrames>3600)throw new AssertionError("Scenario timeout at stage "+stage);return;}
            waitFrames=0;
            switch(stage++){
                case 0:reset();play("ranger_light_pack");break;
                case 1:check(p.hasPower("heir:build:discard_block"),"Light Pack installs through native card use");play("ranger_tailwind");break;
                case 2:check(p.hasPower("heir:build:discard_energy"),"Tailwind installs through native use");hand("ranger_loose_fletching","ranger_pocket_wind","ranger_shed_cloak");EnergyPanel.totalCount=0;queue(new DiscardAction(p,p,3,false));break;
                case 3:
                    check(p.currentBlock==9,"Three discards grant 6 engine Block plus Shed Cloak's 3");check(EnergyPanel.totalCount==2,"Pocket Wind and one Tailwind grant two Energy");check(p.hand.size()==1,"Loose Fletching draws one card");hand("ranger_strike");queue(new DiscardAction(p,p,1,false));break;
                case 4:
                    check(p.currentBlock==11,"Later discards still grant Block");check(EnergyPanel.totalCount==2,"Tailwind pays once per turn");
                    AbstractDungeon.actionManager.turnHasEnded=true;new HeirCard("ranger_pocket_wind").triggerOnManualDiscard();GameActionManager.incrementDiscard(true);check(AbstractDungeon.actionManager.actions.isEmpty(),"End-turn discards do not earn rewards");
                    AbstractDungeon.actionManager.turnHasEnded=false;for(AbstractPower power:p.powers)power.atStartOfTurn();hand("ranger_strike");queue(new DiscardAction(p,p,1,false));break;
                case 5:check(EnergyPanel.totalCount==3,"Tailwind resets next turn");reset();play("knight_forge_sparks");break;
                case 6:check(p.hasPower("heir:build:exhaust_damage"),"Forge Sparks installs through native use");hand("knight_guard","knight_layered_mail","knight_strike");play("knight_cast_off");break;
                case 7:
                    check(p.exhaustPile.size()==2,"Cast Off exhausts both non-Attacks");check(p.hand.size()==1,"Cast Off keeps the Attack");check(p.currentBlock==6,"Cast Off grants Block per exhausted card");check(enemy.currentHealth==996,"Native exhaust hooks trigger Forge Sparks twice");
                    HeirCard ash=new HeirCard("knight_ashen_advance");ash.calculateCardDamage(enemy);check(ash.damage==5,"Ashen Advance scales with Exhaust pile");for(int i=0;i<20;i++)p.exhaustPile.addToTop(new HeirCard("knight_guard"));ash.calculateCardDamage(enemy);check(ash.damage==13,"Exhaust scaling caps at ten cards");
                    reset();held=new HeirCard("mage_stored_ember");ward=new HeirCard("mage_crystal_seed");p.hand.addToHand(held);p.hand.addToHand(ward);p.powers.add(CardPowers.create("establishment",p,1));for(AbstractPower power:p.powers)power.atEndOfTurn(true);queue(new DiscardAtEndOfTurnAction());break;
                case 8:
                    check(p.hand.contains(held)&&p.hand.contains(ward),"Native retention keeps prepared cards");check(held.baseDamage==5&&ward.baseBlock==5,"Native retention grows Attack and Block");check(held.cost==0&&ward.cost==0,"Native Establishment lowers combat costs");queue(new DiscardAtEndOfTurnAction());break;
                case 9:check(held.baseDamage==7&&ward.baseBlock==7,"Growth repeats next retained turn");reset();play("mage_woven_incantation");break;
                case 10:play("mage_arcane_meter");break;
                case 11:play("mage_warding_verse");break;
                case 12:play("mage_warding_verse");break;
                case 13:check(p.currentBlock==4&&!p.hasPower("Energized"),"Two Skills do not trigger third-Skill payoffs");play("mage_warding_verse");break;
                case 14:check(p.currentBlock==9,"Third Skill adds Spellweave Block");check(p.hasPower("Energized")&&p.getPower("Energized").amount==1,"Third Skill grants delayed Energy");reset();play("ranger_tracking_rhythm");break;
                case 15:play("ranger_strike");break;
                case 16:play("ranger_strike");break;
                case 17:check(!p.hasPower("Vigor"),"Two Attacks do not produce Vigor");play("ranger_strike");break;
                case 18:check(p.hasPower("Vigor")&&p.getPower("Vigor").amount==2,"Third Attack reserves Vigor");hp=enemy.currentHealth;play("ranger_strike");break;
                case 19:check(enemy.currentHealth==hp-7,"Fourth Attack spends Vigor");check(!p.hasPower("Vigor"),"Spent Vigor is removed");play("ranger_strike");break;
                case 20:play("ranger_strike");break;
                case 21:check(p.hasPower("Vigor")&&p.getPower("Vigor").amount==2,"Second three-Attack cycle earns fresh Vigor");reset();play("ranger_forest_oracle");break;
                case 22:p.drawPile.clear();queue(new CardEffectAction("scry",3,null,false));break;
                case 23:check(p.currentBlock==4,"Native Scry triggers Forest Oracle Block");reset();hand("ranger_strike","ranger_guard");play("ranger_trail_reset");break;
                case 24:check(p.hand.size()==2,"Trail Reset replaces two remaining cards");check(p.discardPile.size()==3,"Refill discards both cards and the played Skill");reset();play("knight_splintered_pike");break;
                case 25:
                    int wounds=0;for(AbstractCard c:p.drawPile.group)if(c.cardID.equals("Wound"))wounds++;check(wounds==1,"Splintered Pike creates a native Wound");
                    for(Data.Row row:Data.rows("power_effects")){AbstractPower power=CardPowers.create(row.s("id"),p,2);check(power!=null&&!power.description.isEmpty(),"Power factory "+row.s("id"));check(power.img!=null||power.region128!=null,"Power has a valid flash icon "+row.s("id"));}
                    reset();hand("knight_strike","knight_guard");play("ranger_master_plan");break;
                case 26:check(p.hasPower("No Draw"),"Master Plan applies native No Draw");check(p.hand.size()==2,"Master Plan does not draw extra cards");for(AbstractCard c:p.hand.group)check(c.costForTurn==0,"Current hand receives free costs");queue(new DrawCardAction(p,1));break;
                case 27:check(p.hand.size()==2&&p.drawPile.size()==15,"No Draw blocks further drawing in the same turn");p.getPower("No Draw").atEndOfTurn(true);break;
                case 28:check(!p.hasPower("No Draw"),"No Draw expires through native turn hook");reset();p.drawPile.clear();p.drawPile.addToTop(new HeirCard("knight_guard"));p.drawPile.addToTop(new HeirCard("knight_strike"));playUpgraded("mage_cold_read");break;
                case 29:check(p.hand.size()==0&&p.discardPile.size()==3,"Cold Read upgrade draws two and discards two, without a free positive-draw loop");reset();hand("knight_strike","knight_guard");play("mage_enchanted_ink");break;
                case 30:int free=0;for(AbstractCard c:p.hand.group)if(c.costForTurn==0)free++;check(free==1,"Enchanted Ink discounts only one eligible card");reset();hand("knight_strike","knight_guard");playUpgraded("mage_enchanted_ink");break;
                case 31:for(AbstractCard c:p.hand.group)check(c.costForTurn==0,"Ink upgrade discounts two eligible cards");
                    Files.write(HeirMod.root.resolve("build-checks.json"),("{\"nativeEngineAssertions\":"+checks+",\"scenarios\":15,\"result\":\"passed\"}").getBytes(StandardCharsets.UTF_8));stage=-1;System.out.println("HEIR TEST: build engines passed "+checks+" assertions");break;
            }
        }catch(Throwable e){stage=-1;try{Files.write(HeirMod.root.resolve("build-checks-error.txt"),e.toString().getBytes(StandardCharsets.UTF_8));}catch(Exception ignored){}System.err.println("HEIR BUILD CHECK ERROR: "+e);}
    }
}
