package heir;

import basemod.*;
import basemod.abstracts.CustomSavable;
import basemod.interfaces.*;
import com.evacipated.cardcrawl.modthespire.lib.*;
import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.graphics.*;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import com.megacrit.cardcrawl.actions.common.*;
import com.megacrit.cardcrawl.cards.AbstractCard;
import com.megacrit.cardcrawl.characters.AbstractPlayer;
import com.megacrit.cardcrawl.core.*;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.helpers.FontHelper;
import com.megacrit.cardcrawl.powers.*;
import com.megacrit.cardcrawl.rooms.AbstractRoom;
import com.megacrit.cardcrawl.unlock.UnlockTracker;
import java.nio.file.*;
import java.util.*;

@SpireInitializer
public final class HeirMod implements EditCardsSubscriber,EditCharactersSubscriber,PostInitializeSubscriber,PreStartGameSubscriber,StartGameSubscriber,PostDungeonInitializeSubscriber,PostDeathSubscriber,PostBattleSubscriber,OnStartBattleSubscriber,OnPlayerTurnStartSubscriber,PostUpdateSubscriber,PostRenderSubscriber,CustomSavable<Profile.Run> {
    public static final class Enums {
        @SpireEnum public static AbstractPlayer.PlayerClass HEIR;
        @SpireEnum public static AbstractCard.CardColor HEIR_COLOR;
        @SpireEnum(name="HEIR_COLOR") public static com.megacrit.cardcrawl.helpers.CardLibrary.LibraryType HEIR_LIBRARY;
    }
    public static Profile profile;
    public static Path root,profilePath;
    private static final Map<String,Texture> textures=new HashMap<>();
    public static HeirMod instance;
    public static boolean quickPending=false;
    public static String asset(String name){return root.resolve("cache").resolve(name).toString().replace('\\','/');}
    public static Texture texture(String name){if(!textures.containsKey(name))textures.put(name,new Texture(Gdx.files.absolute(asset(name))));return textures.get(name);}
    public static void initialize(){instance=new HeirMod();}
    public HeirMod(){
        String override=System.getenv("HEIR_DATA_DIR");root=Paths.get(override!=null?override:System.getenv("LOCALAPPDATA")+"/HeirOfTheSpire");profilePath=root.resolve("family.json");
        if(!Files.exists(root.resolve("cache/source.json")))throw new IllegalStateException("Rogue Legacy 2 content is missing. Launch using HeirOfTheSpire/Play.cmd so your installed content is prepared first.");
        profile=Profile.load(profilePath);BaseMod.subscribe(this);BaseMod.addSaveField("heir:run",this);
        BaseMod.addColor(Enums.HEIR_COLOR,new Color(.65f,.48f,.2f,1),asset("host_attack512.png"),asset("host_skill512.png"),asset("host_power512.png"),asset("host_orb512.png"),asset("host_attack1024.png"),asset("host_skill1024.png"),asset("host_power1024.png"),asset("host_orb1024.png"),asset("host_orb32.png"));
        System.out.println("HEIR: loaded family "+profile.family+", generation "+profile.generation);
    }
    public static Profile.Heir heir(){
        if(profile.active!=null)return profile.active.heir;
        if(profile.selected==null){if(profile.offers.isEmpty())profile.generateOffers(System.nanoTime());profile.selected=profile.offers.get(new java.util.Random().nextInt(profile.offers.size()));}
        return profile.selected;
    }
    public static int maxHp(){
        Profile.Heir h=heir();int hp=Data.row("classes",h.classId).i("hp")+stat("hp");for(String id:h.traits)hp+=Data.row("traits",id).i("hp");return Math.max(20,hp);
    }
    public static int stat(String effect){return profile.active!=null?profile.active.bonuses.get(effect):profile.bonus(effect);}
    public static int handSize(){int n=Data.row("systems","hand").i("amount")+stat("draw");for(String t:heir().traits)n+=Data.row("traits",t).i("draw");return n;}
    public static boolean isHeir(){return AbstractDungeon.player instanceof HeirPlayer;}
    public void receiveEditCards(){
        for(Data.Row row:Data.rows("cards")){BaseMod.addCard(new HeirCard(row.s("id")));UnlockTracker.unlockCard("heir:"+row.s("id"));}
    }
    public void receiveEditCharacters(){BaseMod.loadCustomStrings(com.megacrit.cardcrawl.localization.CharacterStrings.class,"{\"heir:Heir\":{\"NAMES\":[\"The Heir\",\"the Heir\"],\"TEXT\":[\"An heir climbs. A family endures.\",\"The bloodline faces the Heart.\",\"Blood recognizes blood.\"]}}");BaseMod.addCharacter(new HeirPlayer(),Data.row("ui_art","select_icon").s("path"),Data.row("ui_art","select_bg").s("path"),Enums.HEIR);}
    public void receivePostInitialize(){System.out.println("HEIR: character and "+Data.rows("cards").size()+" cards registered");}
    public void receivePreStartGame(){
        if(CardCrawlGame.chosenCharacter==Enums.HEIR&&!CardCrawlGame.loadingSave){
            if(profile.active!=null)profile.settle(profile.active.id,profile.active.floors,0,false);
            Profile.Run run=profile.begin(Settings.seed==null?System.nanoTime():Settings.seed);profile.save(profilePath);quickPending=run.quick;
            System.out.println("HEIR: run "+run.id+" / "+run.heir.classId+" / "+run.heir.traits);
        }
    }
    public void receiveStartGame(){
        if(!isHeir())return;
        AbstractDungeon.player.masterHandSize=AbstractDungeon.player.gameHandSize=handSize();
    }
    public void receivePostDungeonInitialize(){if(isHeir())System.out.println("HEIR: dungeon initialized, class "+heir().classId);}
    public void receiveOnBattleStart(AbstractRoom room){
        if(!isHeir())return;int strength=Data.row("classes",heir().classId).i("strength")+stat("strength"),dex=Data.row("classes",heir().classId).i("dexterity")+stat("dexterity");
        for(String id:heir().traits){strength+=Data.row("traits",id).i("strength");dex+=Data.row("traits",id).i("dexterity");}
        if(strength!=0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(AbstractDungeon.player,AbstractDungeon.player,new StrengthPower(AbstractDungeon.player,strength),strength));
        if(dex!=0)AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(AbstractDungeon.player,AbstractDungeon.player,new DexterityPower(AbstractDungeon.player,dex),dex));
        System.out.println("HEIR: battle bonuses Strength="+strength+", Dexterity="+dex);
    }
    public void receiveOnPlayerTurnStart(){}
    public void receivePostBattle(AbstractRoom room){
        if(!isHeir())return;int heal=Data.row("classes",heir().classId).i("heal")+stat("heal");for(String t:heir().traits)heal+=Data.row("traits",t).i("heal");AbstractDungeon.player.heal(heal);
        if(profile.active!=null){profile.active.floors=Math.max(profile.active.floors,AbstractDungeon.floorNum);profile.save(profilePath);}
    }
    public static void finish(boolean victory){if(!isHeir()||profile.active==null)return;int earned=profile.settle(profile.active.id,AbstractDungeon.floorNum,AbstractDungeon.player.gold,victory);profile.save(profilePath);Manor.autoOpen=true;System.out.println("HEIR: settled run; crowns="+earned+", family crowns="+profile.crowns);}
    public void receivePostDeath(){finish(false);}
    public Profile.Run onSave(){return isHeir()?profile.active:null;}
    public void onLoad(Profile.Run run){if(run!=null&&!run.id.equals(profile.settledId)){profile.active=run;profile.save(profilePath);}quickPending=false;}
    public void receivePostUpdate(){
        if(quickPending&&isHeir()&&CardCrawlGame.mode==CardCrawlGame.GameMode.GAMEPLAY&&!AbstractDungeon.isFadingIn&&!AbstractDungeon.isFadingOut&&AbstractDungeon.currMapNode!=null&&AbstractDungeon.floorNum==0){
            for(com.megacrit.cardcrawl.map.MapRoomNode node:AbstractDungeon.map.get(0))if(node.hasEdges()){
                AbstractDungeon.nextRoom=node;AbstractDungeon.firstRoomChosen=true;AbstractDungeon.nextRoomTransitionStart();quickPending=false;System.out.println("HEIR: entering first fight");break;
            }
        }
        Manor.update();TestBridge.update();
    }
    public void receivePostRender(SpriteBatch sb){
        TraitTips.begin();
        if(isHeir()&&AbstractDungeon.isPlayerInDungeon()&&profile.active!=null){
            Profile.Heir h=heir();FontHelper.renderFontLeftTopAligned(sb,FontHelper.tipBodyFont,h.name+" "+profile.family+"  |  "+Data.row("classes",h.classId).s("name"),30*Settings.scale,Settings.HEIGHT-100*Settings.scale,Manor.bannerColor());
            float x=30*Settings.scale,y=Settings.HEIGHT-165*Settings.scale;
            for(String id:h.traits){
                Data.Row t=Data.row("traits",id);float width=new com.badlogic.gdx.graphics.g2d.GlyphLayout(FontHelper.tipBodyFont,t.s("name")).width;
                sb.setColor(Color.WHITE);sb.draw(texture(t.s("asset")+".png"),x,y,34*Settings.scale,34*Settings.scale);
                FontHelper.renderFontLeftTopAligned(sb,FontHelper.tipBodyFont,t.s("name"),x+42*Settings.scale,y+31*Settings.scale,Color.WHITE);
                TraitTips.hover(id,x,y-4*Settings.scale,width+46*Settings.scale,42*Settings.scale);
                x+=width+66*Settings.scale;
            }
        }
        Manor.render(sb);
        TraitTips.render(sb);
        TestBridge.renderGallery(sb);
        if(Manor.open&&Data.row("systems","manor_cursor").b("enabled"))CardCrawlGame.cursor.render(sb);
        sb.flush();
        TestBridge.capture();
    }
}
