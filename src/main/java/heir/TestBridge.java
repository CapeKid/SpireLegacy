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
                    if(AbstractDungeon.getCurrRoom().phase!=com.megacrit.cardcrawl.rooms.AbstractRoom.RoomPhase.COMBAT)throw new IllegalStateException("Play requires a combat room");
                    int index=cmd.get("index").getAsInt();AbstractCard card=AbstractDungeon.player.hand.group.get(index);AbstractMonster target=AbstractDungeon.getMonsters().getRandomMonster(true);
                    if(card.canUse(AbstractDungeon.player,target))AbstractDungeon.actionManager.addCardQueueItem(new CardQueueItem(card,target,com.megacrit.cardcrawl.ui.panels.EnergyPanel.totalCount));
                }else if(action.equals("die")&&HeirMod.isHeir()){AbstractDungeon.player.damage(new DamageInfo(null,9999,DamageInfo.DamageType.HP_LOSS));}
                else if(action.equals("manor")){Manor.openSelection();Manor.open=true;Manor.settingsOnly=false;}
                else if(action.equals("settings")){Manor.openSelection();Manor.open=true;Manor.settingsOnly=true;}
                else if(action.equals("menu")){CardCrawlGame.startOver=true;CardCrawlGame.mode=CardCrawlGame.GameMode.CHAR_SELECT;CardCrawlGame.mainMenuScreen=new com.megacrit.cardcrawl.screens.mainMenu.MainMenuScreen();CardCrawlGame.mainMenuScreen.screen=com.megacrit.cardcrawl.screens.mainMenu.MainMenuScreen.CurScreen.MAIN_MENU;}
                else if(action.equals("dismiss")){AbstractDungeon.closeCurrentScreen();}
                else if(action.equals("fight")){HeirMod.quickPending=true;}
                else if(action.equals("issue6checks")){Issue6Checks.run();}
                else if(action.equals("rewardchecks")){RewardChecks.run();}
                else if(action.equals("expansionchecks")){ExpansionChecks.run();}
                else if(action.equals("bossreward")){
                    HeirMod.heir().classId=cmd.get("classId").getAsString();HeirMod.heir().traits.clear();CardCrawlGame.dungeon.initializeCardPools();
                    AbstractDungeon.player.relics.clear();
                    if(cmd.has("question")&&cmd.get("question").getAsBoolean())new com.megacrit.cardcrawl.relics.QuestionCard().instantObtain(AbstractDungeon.player,0,true);
                    if(cmd.has("crown")&&cmd.get("crown").getAsBoolean())new com.megacrit.cardcrawl.relics.BustedCrown().instantObtain(AbstractDungeon.player,AbstractDungeon.player.relics.size(),true);
                    if(cmd.has("shard")&&cmd.get("shard").getAsBoolean())new com.megacrit.cardcrawl.relics.PrismaticShard().instantObtain(AbstractDungeon.player,AbstractDungeon.player.relics.size(),true);
                    com.megacrit.cardcrawl.rooms.MonsterRoomBoss room=new com.megacrit.cardcrawl.rooms.MonsterRoomBoss();room.monsters=new MonsterGroup(new AbstractMonster[0]);room.phase=com.megacrit.cardcrawl.rooms.AbstractRoom.RoomPhase.COMPLETE;AbstractDungeon.currMapNode.room=room;
                    System.out.println("HEIR TEST: requesting boss reward; class="+HeirMod.heir().classId+", rares="+AbstractDungeon.rareCardPool.size()+", relics="+AbstractDungeon.player.relics);
                    java.util.ArrayList<AbstractCard> rewards=AbstractDungeon.getRewardCards();JsonArray result=new JsonArray();for(AbstractCard c:rewards){JsonObject item=new JsonObject();item.addProperty("id",c.cardID);item.addProperty("rarity",c.rarity.toString());result.add(item);}
                    Files.write(HeirMod.root.resolve("boss-reward.json"),Data.GSON.toJson(result).getBytes(StandardCharsets.UTF_8));AbstractDungeon.cardRewardScreen.open(rewards,null,"Boss reward check");
                }
                else if(action.equals("hand")){
                    if(cmd.has("draw")){AbstractDungeon.player.drawPile.clear();for(JsonElement id:cmd.getAsJsonArray("draw"))AbstractDungeon.player.drawPile.addToTop(new HeirCard(id.getAsString()));}
                    AbstractDungeon.player.hand.clear();for(JsonElement id:cmd.getAsJsonArray("cards")){HeirCard card=new HeirCard(id.getAsString());card.current_x=Settings.WIDTH/2f;card.current_y=Settings.HEIGHT/4f;AbstractDungeon.player.hand.addToHand(card);}AbstractDungeon.player.hand.refreshHandLayout();AbstractDungeon.player.hand.applyPowers();
                }
                else if(action.equals("shop")){
                    AbstractDungeon.currMapNode.room=new com.megacrit.cardcrawl.rooms.ShopRoom();AbstractDungeon.getCurrRoom().onPlayerEntry();AbstractDungeon.shopScreen.open();
                }
                else if(action.equals("endturn")&&HeirMod.isHeir()){AbstractDungeon.overlayMenu.endTurnButton.disable(true);}
                else if(action.equals("traitchecks")){
                    java.util.List<String> saved=new java.util.ArrayList<>(HeirMod.heir().traits);int checks=0;int savedHp=HeirMod.isHeir()?AbstractDungeon.player.currentHealth:0;
                    try{
                        HeirMod.heir().traits.clear();HeirMod.heir().traits.add("onehitdeath");if(TraitRules.maxHp(999)!=1)throw new AssertionError("Fragile HP");checks++;
                        HeirMod.heir().traits.clear();HeirMod.heir().traits.add("nomeat");if(TraitRules.healing(9)!=4)throw new AssertionError("Vegan healing");checks++;
                        HeirMod.heir().traits.clear();HeirMod.heir().traits.add("megahealth");if(TraitRules.healing(99)!=0)throw new AssertionError("Hero Complex healing");checks++;
                        HeirMod.heir().traits.clear();HeirMod.heir().traits.add("superhealer");if(TraitRules.healing(7)!=14)throw new AssertionError("Super Healer");checks++;
                        HeirCard attack=new HeirCard("mage_strike");TraitPower pacifist=new TraitPower(Data.row("traits","cantattack"));if(pacifist.canPlayCard(attack)||!pacifist.canPlayCard(new HeirCard("mage_guard")))throw new AssertionError("Pacifist card rules");checks++;
                        TraitPower costly=new TraitPower(Data.row("traits","manacostanddamageup"));costly.onCardDraw(attack);if(attack.costForTurn!=2||costly.atDamageGive(10,DamageInfo.DamageType.NORMAL)!=15)throw new AssertionError("Overcompensation");checks++;
                        TraitPower algesia=new TraitPower(Data.row("traits","noimmunitywindow"));if(algesia.onAttackedToChangeDamage(new DamageInfo(null,4,DamageInfo.DamageType.NORMAL),4)!=6||algesia.onAttackedToChangeDamage(new DamageInfo(null,4,DamageInfo.DamageType.HP_LOSS),4)!=4)throw new AssertionError("Algesia");checks++;
                        HeirMod.heir().traits.clear();HeirMod.heir().traits.add("randomizekit");if(!TraitRules.deck("mage",42).equals(TraitRules.deck("mage",42)))throw new AssertionError("Seeded kit");checks++;
                        if(HeirMod.isHeir()&&AbstractDungeon.player.maxHealth>20){HeirMod.heir().traits.clear();HeirMod.heir().traits.add("nomeat");AbstractDungeon.player.currentHealth=AbstractDungeon.player.maxHealth-20;int before=AbstractDungeon.player.currentHealth;AbstractDungeon.player.heal(9);if(AbstractDungeon.player.currentHealth!=before+4)throw new AssertionError("Native healing patch");checks++;}
                        Files.write(HeirMod.root.resolve("trait-checks.json"),("{\"runtimeTraitChecks\":"+checks+"}").getBytes(StandardCharsets.UTF_8));
                    }finally{HeirMod.heir().traits.clear();HeirMod.heir().traits.addAll(saved);if(HeirMod.isHeir())AbstractDungeon.player.currentHealth=savedHp;}
                }
                else if(action.equals("audit")){
                    int checks=0;
                    for(Data.Row row:Data.rows("cards")){HeirCard c=new HeirCard(row.s("id"));int damage=c.baseDamage,block=c.baseBlock,magic=c.baseMagicNumber;c.upgrade();if(c.baseDamage!=damage+(damage>0?row.i("upgradeDamage"):0)||c.baseBlock!=block+(block>0?(c.genes.mercy?3:row.i("upgradeBlock")):0)||c.baseMagicNumber!=magic+(magic>0?row.i("upgradeMagic"):0)||!c.upgraded)throw new IllegalStateException("Card upgrade mismatch "+row.s("id"));checks++;}
                    for(Data.Row row:Data.rows("traits")){new TraitPower(row);if(TraitTips.effects(row.s("id")).isEmpty())throw new IllegalStateException("Empty trait tooltip");}
                    Files.write(HeirMod.root.resolve("audit.json"),("{\"cardUpgradesVerified\":"+checks+",\"traitPowersVerified\":"+Data.rows("traits").size()+"}").getBytes(StandardCharsets.UTF_8));
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
                if(HeirMod.isHeir()&&AbstractDungeon.currMapNode!=null){out.addProperty("hp",AbstractDungeon.player.currentHealth);out.addProperty("maxHp",AbstractDungeon.player.maxHealth);out.addProperty("energy",com.megacrit.cardcrawl.ui.panels.EnergyPanel.totalCount);out.addProperty("floor",AbstractDungeon.floorNum);out.addProperty("room",AbstractDungeon.getCurrRoom().getClass().getSimpleName());out.addProperty("phase",String.valueOf(AbstractDungeon.getCurrRoom().phase));out.addProperty("screen",String.valueOf(AbstractDungeon.screen));out.addProperty("drawPile",AbstractDungeon.player.drawPile.size());out.addProperty("discardPile",AbstractDungeon.player.discardPile.size());out.addProperty("exhaustPile",AbstractDungeon.player.exhaustPile.size());out.addProperty("queuedCards",AbstractDungeon.actionManager.cardQueue.size());out.addProperty("queuedActions",AbstractDungeon.actionManager.actions.size());out.addProperty("currentAction",AbstractDungeon.actionManager.currentAction==null?"none":AbstractDungeon.actionManager.currentAction.getClass().getSimpleName());JsonArray cards=new JsonArray();for(AbstractCard c:AbstractDungeon.player.hand.group){JsonObject r=new JsonObject();r.addProperty("id",c.cardID);r.addProperty("cost",c.costForTurn);r.addProperty("damage",c.damage);r.addProperty("block",c.block);cards.add(r);}out.add("hand",cards);JsonArray enemies=new JsonArray();if(AbstractDungeon.getMonsters()!=null)for(AbstractMonster m:AbstractDungeon.getMonsters().monsters){JsonObject e=new JsonObject();e.addProperty("name",m.name);e.addProperty("hp",m.currentHealth);enemies.add(e);}out.add("enemies",enemies);}
                Files.write(HeirMod.root.resolve("state.json"),Data.GSON.toJson(out).getBytes(StandardCharsets.UTF_8));
            }
        }catch(Exception e){System.err.println("HEIR TEST ERROR: "+e);}
    }
}
