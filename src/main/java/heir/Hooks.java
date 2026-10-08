package heir;
import com.evacipated.cardcrawl.modthespire.lib.*;
import com.megacrit.cardcrawl.dungeons.*;
import com.megacrit.cardcrawl.characters.AbstractPlayer;
import com.megacrit.cardcrawl.map.MapRoomNode;
import com.megacrit.cardcrawl.rooms.MonsterRoom;
import com.megacrit.cardcrawl.monsters.MonsterGroup;
import com.megacrit.cardcrawl.screens.VictoryScreen;
import com.megacrit.cardcrawl.screens.mainMenu.MainMenuScreen;
import java.util.ArrayList;

public final class Hooks {
    @SpirePatch(clz=Exordium.class,method=SpirePatch.CONSTRUCTOR,paramtypez={AbstractPlayer.class,ArrayList.class})
    public static class QuickStart {
        public static void Postfix(Exordium __instance){
            if(HeirMod.isHeir()&&HeirMod.quickPending){
                System.out.println("HEIR: quick start awaits gameplay initialization");
            }
        }
    }
    @SpirePatch(clz=VictoryScreen.class,method=SpirePatch.CONSTRUCTOR,paramtypez={MonsterGroup.class})
    public static class Victory { public static void Postfix(VictoryScreen __instance){HeirMod.finish(true);} }
    @SpirePatch(clz=MainMenuScreen.class,method="update")
    public static class ManorInput {public static SpireReturn<Void> Prefix(MainMenuScreen __instance){return Manor.open?SpireReturn.Return(null):SpireReturn.Continue();}}
}
