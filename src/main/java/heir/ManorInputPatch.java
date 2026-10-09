package heir;
import com.evacipated.cardcrawl.modthespire.lib.*;
import com.megacrit.cardcrawl.screens.charSelect.CharacterSelectScreen;

/** Modal manor controls must not also activate the character screen underneath. */
@SpirePatch(clz=CharacterSelectScreen.class,method="update")
public final class ManorInputPatch {
    @SpirePrefixPatch public static SpireReturn<Void> before(){return Manor.open?SpireReturn.Return(null):SpireReturn.Continue();}
}
