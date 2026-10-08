package heir;
import com.megacrit.cardcrawl.powers.*;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
/** Sheet-driven upgrades to a class's persistent combat loop. */
public final class HeirEnginePower extends AbstractPower {
    public HeirEnginePower(String effect,int value){owner=AbstractDungeon.player;ID="heir:"+effect;amount=value;type=PowerType.BUFF;name=effect.equals("reservoir")?"Arcane Reservoir":"Endless Quiver";loadRegion(effect.equals("reservoir")?"strength":"dexterity");updateDescription();}
    public void updateDescription(){description=ID.equals("heir:reservoir")?"Each Arcane Charge adds "+amount+" extra attack damage.":"Hunter Rhythm draws "+amount+" additional card(s).";}
}
