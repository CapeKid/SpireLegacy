package heir;
import com.google.gson.*;
import com.megacrit.cardcrawl.core.*;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.monsters.*;
import com.megacrit.cardcrawl.screens.charSelect.*;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;

/** Opt-in local test bridge; disabled in ordinary launches. Executes on the game thread. */
public final class TestBridge {
    private static long frames=0;
    private static String capturePath;
    private static Integer hoverX,hoverY;
    private static java.util.List<HeirCard> gallery;
    public static void renderGallery(com.badlogic.gdx.graphics.g2d.SpriteBatch sb){
        if(gallery==null)return;
        int i=0;for(HeirCard card:gallery){card.current_x=(160+i*310)*Settings.scale;card.current_y=540*Settings.scale;card.drawScale=.85f;card.angle=0;card.render(sb);i++;}
    }
    public static void capture(){
        if(capturePath==null)return;
        try{
            com.badlogic.gdx.graphics.Pixmap p=new com.badlogic.gdx.graphics.Pixmap(Settings.WIDTH,Settings.HEIGHT,com.badlogic.gdx.graphics.Pixmap.Format.RGBA8888);
            p.getPixels().put(com.badlogic.gdx.utils.ScreenUtils.getFrameBufferPixels(0,0,Settings.WIDTH,Settings.HEIGHT,true));
            com.badlogic.gdx.graphics.PixmapIO.writePNG(com.badlogic.gdx.Gdx.files.absolute(capturePath),p);p.dispose();System.out.println("HEIR TEST: captured real game framebuffer "+capturePath);
        }finally{capturePath=null;}
    }
    public static void update(){
        if(!Boolean.getBoolean("heir.test")&&!"1".equals(System.getenv("HEIR_TEST_MODE")))return;
        try{
            Path command=HeirMod.root.resolve("command.json");
            if(Files.exists(command)){
                JsonObject cmd=Data.GSON.fromJson(new String(Files.readAllBytes(command),StandardCharsets.UTF_8),JsonObject.class);Files.delete(command);String action=cmd.get("action").getAsString();
                if(action.equals("gallery")){gallery=new java.util.ArrayList<>();for(JsonElement id:cmd.getAsJsonArray("cards"))gallery.add(new HeirCard(id.getAsString()));}
                else if(action.equals("galleryoff")){gallery=null;}
                else if(action.equals("hover")){hoverX=cmd.has("x")?cmd.get("x").getAsInt():null;hoverY=cmd.has("y")?cmd.get("y").getAsInt():null;}
                else if(action.equals("shot")){capturePath=HeirMod.root.resolve(cmd.get("name").getAsString()+".png").toString();}
                else if(action.equals("select")){
                    CardCrawlGame.mainMenuScreen.charSelectScreen.open(false);
                    for(CharacterOption o:CardCrawlGame.mainMenuScreen.charSelectScreen.options){o.selected=o.c instanceof HeirPlayer;if(o.selected){CardCrawlGame.chosenCharacter=HeirMod.Enums.HEIR;o.locked=false;}}
                    CardCrawlGame.mainMenuScreen.charSelectScreen.justSelected();
                    CardCrawlGame.mainMenuScreen.charSelectScreen.bgCharImg=com.megacrit.cardcrawl.helpers.ImageMaster.loadImage(Data.row("ui_art","select_bg").s("path"));
                }else if(action.equals("start")){CardCrawlGame.mainMenuScreen.charSelectScreen.confirmButton.hb.clicked=true;}
                else if(action.equals("play")&&HeirMod.isHeir()){
                    int index=cmd.get("index").getAsInt();AbstractCard card=AbstractDungeon.player.hand.group.get(index);AbstractMonster target=AbstractDungeon.getMonsters().getRandomMonster(true);
                    if(card.canUse(AbstractDungeon.player,target))AbstractDungeon.player.useCard(card,target,card.costForTurn);
                }else if(action.equals("die")&&HeirMod.isHeir()){AbstractDungeon.player.damage(new DamageInfo(null,9999,DamageInfo.DamageType.HP_LOSS));}
                else if(action.equals("manor")){Manor.openSelection();Manor.open=true;Manor.settingsOnly=false;}
                else if(action.equals("settings")){Manor.openSelection();Manor.open=true;Manor.settingsOnly=true;}
                else if(action.equals("menu")){CardCrawlGame.startOver=true;CardCrawlGame.mode=CardCrawlGame.GameMode.CHAR_SELECT;CardCrawlGame.mainMenuScreen=new com.megacrit.cardcrawl.screens.mainMenu.MainMenuScreen();CardCrawlGame.mainMenuScreen.screen=com.megacrit.cardcrawl.screens.mainMenu.MainMenuScreen.CurScreen.MAIN_MENU;}
                else if(action.equals("dismiss")){AbstractDungeon.closeCurrentScreen();}
                else if(action.equals("audit")){
                    int checks=0;
                    for(Data.Row row:Data.rows("cards")){HeirCard c=new HeirCard(row.s("id"));c.upgrade();if(c.baseDamage!=row.i("damage")+row.i("upgradeDamage")||c.baseBlock!=row.i("block")+row.i("upgradeBlock")||!c.upgraded)throw new IllegalStateException("Card upgrade mismatch "+row.s("id"));checks++;}
                    Files.write(HeirMod.root.resolve("audit.json"),("{\"cardUpgradesVerified\":"+checks+"}").getBytes(StandardCharsets.UTF_8));
                }
                else if(action.equals("purchase")){boolean bought=HeirMod.profile.purchase(cmd.get("id").getAsString());if(!bought)throw new IllegalStateException("Purchase rejected");HeirMod.profile.save(HeirMod.profilePath);}
                else if(action.equals("choose")){HeirMod.profile.selected=HeirMod.profile.offers.get(cmd.get("index").getAsInt());HeirMod.profile.save(HeirMod.profilePath);}
                else if(action.equals("fixture")){
                    java.util.List<String> traits=new java.util.ArrayList<>();for(JsonElement t:cmd.getAsJsonArray("traits")){Data.row("traits",t.getAsString());traits.add(t.getAsString());}
                    Data.row("classes",cmd.get("classId").getAsString());HeirMod.profile.selected=new Profile.Heir(cmd.has("name")?cmd.get("name").getAsString():"Test",cmd.get("classId").getAsString(),traits);HeirMod.profile.save(HeirMod.profilePath);
                }
                System.out.println("HEIR TEST: "+action);
            }
            if(hoverX!=null&&hoverY!=null){com.megacrit.cardcrawl.helpers.input.InputHelper.mX=hoverX;com.megacrit.cardcrawl.helpers.input.InputHelper.mY=hoverY;}
            if(++frames%60==0){
                JsonObject out=new JsonObject();out.addProperty("mode",String.valueOf(CardCrawlGame.mode));out.addProperty("menu",CardCrawlGame.mainMenuScreen==null?"none":String.valueOf(CardCrawlGame.mainMenuScreen.screen));out.addProperty("generation",HeirMod.profile.generation);out.addProperty("crowns",HeirMod.profile.crowns);out.addProperty("manor",Manor.open);
                if(HeirMod.isHeir()&&AbstractDungeon.currMapNode!=null){out.addProperty("hp",AbstractDungeon.player.currentHealth);out.addProperty("maxHp",AbstractDungeon.player.maxHealth);out.addProperty("energy",com.megacrit.cardcrawl.ui.panels.EnergyPanel.totalCount);out.addProperty("floor",AbstractDungeon.floorNum);out.addProperty("room",AbstractDungeon.getCurrRoom().getClass().getSimpleName());out.addProperty("phase",String.valueOf(AbstractDungeon.getCurrRoom().phase));JsonArray cards=new JsonArray();for(AbstractCard c:AbstractDungeon.player.hand.group){JsonObject r=new JsonObject();r.addProperty("id",c.cardID);r.addProperty("cost",c.costForTurn);r.addProperty("damage",c.damage);r.addProperty("block",c.block);cards.add(r);}out.add("hand",cards);JsonArray enemies=new JsonArray();if(AbstractDungeon.getMonsters()!=null)for(AbstractMonster m:AbstractDungeon.getMonsters().monsters){JsonObject e=new JsonObject();e.addProperty("name",m.name);e.addProperty("hp",m.currentHealth);enemies.add(e);}out.add("enemies",enemies);}
                Files.write(HeirMod.root.resolve("state.json"),Data.GSON.toJson(out).getBytes(StandardCharsets.UTF_8));
            }
        }catch(Exception e){System.err.println("HEIR TEST ERROR: "+e);}
    }
}
