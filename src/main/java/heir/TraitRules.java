package heir;

import com.badlogic.gdx.graphics.Color;
import com.evacipated.cardcrawl.modthespire.lib.*;
import com.megacrit.cardcrawl.cards.*;
import com.megacrit.cardcrawl.core.*;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.monsters.AbstractMonster;
import com.megacrit.cardcrawl.powers.*;
import com.megacrit.cardcrawl.actions.common.*;
import com.megacrit.cardcrawl.relics.AbstractRelic;
import com.megacrit.cardcrawl.rooms.*;
import com.megacrit.cardcrawl.shop.ShopScreen;
import java.util.*;

/** All adaptations dispatch from the authored trait sheet. */
public final class TraitRules {
    public static boolean has(String effect){for(String id:HeirMod.heir().traits)if(Data.row("traits",id).s("effect").equals(effect))return true;return false;}
    public static int amount(String effect){int n=0;for(String id:HeirMod.heir().traits){Data.Row r=Data.row("traits",id);if(r.s("effect").equals(effect))n+=r.i("amount");}return n;}
    public static int maxHp(int hp){return has("fragile")?1:Math.max(20,hp);}
    public static int healing(int hp){if(has("no_heal"))return 0;if(has("super_heal"))hp*=amount("super_heal");if(has("vegan"))hp/=2;return hp;}
    public static List<String> deck(String cls,long seed){
        List<String> deck=new ArrayList<>(Data.row("decks",cls).list("cards"));
        if(has("kit")){Random random=new Random(seed);List<Data.Row> classes=new ArrayList<>(Data.rows("classes"));for(int i=0;i<deck.size();i++){List<String> source=Data.row("decks",classes.get(random.nextInt(classes.size())).s("id")).list("cards");deck.set(i,source.get(random.nextInt(source.size())));}}
        return deck;
    }
    public static void battle(){
        for(String id:HeirMod.heir().traits){Data.Row r=Data.row("traits",id);String e=r.s("effect");int n=r.i("amount");
            AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(AbstractDungeon.player,AbstractDungeon.player,new TraitPower(r),-1));
            for(AbstractMonster m:AbstractDungeon.getMonsters().monsters){
                if(e.equals("poison")||e.equals("pacifist"))AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,AbstractDungeon.player,new PoisonPower(m,AbstractDungeon.player,n),n));
                if(e.equals("weak"))AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,AbstractDungeon.player,new WeakPower(m,n,false),n));
                if(e.equals("enemy_guard"))AbstractDungeon.actionManager.addToBottom(new GainBlockAction(m,m,n));
                if(e.equals("enemy_strength"))AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(m,m,new StrengthPower(m,n),n));
            }
            if(e.equals("thorns"))AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(AbstractDungeon.player,AbstractDungeon.player,new ThornsPower(AbstractDungeon.player,n),n));
            if(e.equals("diva"))AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(AbstractDungeon.player,AbstractDungeon.player,new VulnerablePower(AbstractDungeon.player,1,false),1));
            if(e.equals("energy"))AbstractDungeon.actionManager.addToBottom(new GainEnergyAction(n));
            if(e.equals("turn_block"))AbstractDungeon.actionManager.addToBottom(new GainBlockAction(AbstractDungeon.player,AbstractDungeon.player,n));
        }
    }
    public static Color color(Color banner){
        if(has("gray"))return Color.LIGHT_GRAY;if(has("blue"))return Color.BLUE;if(has("sepia"))return new Color(.7f,.55f,.3f,1);
        if(has("nature"))return Color.GREEN;if(has("medium"))return new Color(.65f,.45f,.9f,1);if(has("festive"))return Color.SCARLET;
        if(has("rainbow")){double t=(System.currentTimeMillis()%6000)/6000.0*Math.PI*2;return new Color((float)(.65+.35*Math.sin(t)),(float)(.65+.35*Math.sin(t+2)),(float)(.65+.35*Math.sin(t+4)),1);}
        if(has("histrionic")&&AbstractDungeon.player!=null&&AbstractDungeon.player.currentHealth<AbstractDungeon.player.maxHealth)return new Color(1,.5f,.5f,1);
        return banner;
    }
    @SpirePatch(clz=com.megacrit.cardcrawl.core.AbstractCreature.class,method="heal",paramtypez={int.class,boolean.class})
    public static class Healing { @SpirePrefixPatch public static void before(com.megacrit.cardcrawl.core.AbstractCreature __instance,@ByRef int[] healAmount){if(__instance instanceof HeirPlayer)healAmount[0]=healing(healAmount[0]);}}
    @SpirePatch(clz=ShopScreen.class,method="init")
    public static class Shop { @SpirePostfixPatch public static void after(ShopScreen __instance){if(HeirMod.isHeir()&&has("shop"))__instance.applyDiscount(1-amount("shop")/100f,false);}}
    @SpirePatch(clz=TreasureRoom.class,method="onPlayerEntry")
    public static class Chest { @SpirePostfixPatch public static void after(){if(HeirMod.isHeir()&&has("chest"))AbstractDungeon.player.damage(new DamageInfo(null,amount("chest"),DamageInfo.DamageType.HP_LOSS));}}
    @SpirePatch(clz=AbstractMonster.class,method="die",paramtypez={boolean.class})
    public static class EnemyDeath { @SpirePrefixPatch public static void before(AbstractMonster __instance){if(HeirMod.isHeir()&&!__instance.isDying&&!__instance.halfDead){if(has("diva"))AbstractDungeon.player.gainGold(amount("diva"));if(has("explosions"))AbstractDungeon.player.damage(new DamageInfo(null,amount("explosions"),DamageInfo.DamageType.HP_LOSS));}}}
}
