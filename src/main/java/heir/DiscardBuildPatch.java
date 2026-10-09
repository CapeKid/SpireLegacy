package heir;
import com.evacipated.cardcrawl.modthespire.lib.*;
import com.megacrit.cardcrawl.actions.GameActionManager;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.powers.AbstractPower;
/** Native manual-discard notification also covers non-Heir cards under Prismatic Shard. */
@SpirePatch(clz=GameActionManager.class,method="incrementDiscard",paramtypez={boolean.class})
public final class DiscardBuildPatch {
    @SpirePostfixPatch public static void after(boolean endOfTurn){
        if(!endOfTurn&&AbstractDungeon.player!=null&&!AbstractDungeon.actionManager.turnHasEnded)
            for(AbstractPower power:AbstractDungeon.player.powers)if(power instanceof BuildPower)((BuildPower)power).onManualDiscard();
    }
}
