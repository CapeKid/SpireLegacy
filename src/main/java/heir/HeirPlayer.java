package heir;
import basemod.abstracts.CustomPlayer;
import basemod.animations.AbstractAnimation;
import com.badlogic.gdx.graphics.*;
import com.badlogic.gdx.graphics.g2d.*;
import com.megacrit.cardcrawl.actions.AbstractGameAction.AttackEffect;
import com.megacrit.cardcrawl.cards.AbstractCard;
import com.megacrit.cardcrawl.characters.AbstractPlayer;
import com.megacrit.cardcrawl.core.*;
import com.megacrit.cardcrawl.helpers.*;
import com.megacrit.cardcrawl.screens.CharSelectInfo;
import com.megacrit.cardcrawl.ui.panels.energyorb.EnergyOrbRed;
import java.util.*;

public final class HeirPlayer extends CustomPlayer {
    public HeirPlayer(){
        super("The Heir",HeirMod.Enums.HEIR,new EnergyOrbRed(),new AbstractAnimation(){public Type type(){return Type.NONE;}});
        initializeClass(HeirMod.asset("hero.png"),"images/characters/ironclad/shoulder2.png","images/characters/ironclad/shoulder.png","images/characters/ironclad/corpse.png",getLoadout(),0,0,150,200,new EnergyManager(3));
        dialogX=drawX;dialogY=drawY+220*Settings.scale;
    }
    public ArrayList<String> getStartingDeck(){ArrayList<String> d=new ArrayList<>();for(String key:TraitRules.deck(HeirMod.heir().classId,Settings.seed==null?0L:Settings.seed))d.add("heir:"+key);return d;}
    public ArrayList<String> getStartingRelics(){return new ArrayList<>();}
    public CharSelectInfo getLoadout(){int hp=HeirMod.maxHp();return new CharSelectInfo("The Heir","A new life. A lasting legacy. NL Three classes, inherited traits and a growing family manor.",hp,hp,0,99,HeirMod.handSize(),this,getStartingRelics(),getStartingDeck(),false);}
    public String getTitle(PlayerClass cls){return "the Heir";}
    public AbstractCard.CardColor getCardColor(){return HeirMod.Enums.HEIR_COLOR;}
    public Color getCardRenderColor(){return new Color(.7f,.55f,.3f,1);}
    public Color getCardTrailColor(){return getCardRenderColor();}
    public Color getSlashAttackColor(){return getCardRenderColor();}
    public int getAscensionMaxHPLoss(){return 5;}
    public BitmapFont getEnergyNumFont(){return FontHelper.energyNumFontRed;}
    public Texture getEnergyImage(){return ImageMaster.RED_ORB_FLASH_VFX;}
    public String getLocalizedCharacterName(){return "The Heir";}
    public AbstractPlayer newInstance(){return new HeirPlayer();}
    public String getSpireHeartText(){return "The family will remember this climb.";}
    public AttackEffect[] getSpireHeartSlashEffect(){return new AttackEffect[]{AttackEffect.SLASH_DIAGONAL,AttackEffect.SLASH_HORIZONTAL};}
    public String getVampireText(){return "The bloodline recognizes a familiar hunger.";}
    public AbstractCard getStartCardForEvent(){return new HeirCard(Data.row("classes",HeirMod.heir().classId).s("signature"));}
    public String getCustomModeCharacterButtonSoundKey(){return "ATTACK_HEAVY";}
    public void doCharSelectScreenSelectEffect(){CardCrawlGame.sound.play("ATTACK_HEAVY");}
    public ArrayList<AbstractCard> getCardPool(ArrayList<AbstractCard> pool){for(Data.Row r:Data.rows("cards"))if(!r.s("rarity").equals("BASIC")&&r.s("classId").equals(HeirMod.heir().classId))pool.add(new HeirCard(r.s("id")));return pool;}
    public void renderPlayerImage(SpriteBatch sb){
        Texture hero=HeirMod.texture("hero.png");float scale=Settings.scale;
        for(String id:HeirMod.heir().traits)scale*=Data.row("traits",id).f("scale");
        sb.setColor(TraitRules.color(Manor.bannerColor()));sb.draw(hero,drawX-hero.getWidth()*scale/2,drawY,hero.getWidth()*scale,hero.getHeight()*scale);sb.setColor(Color.WHITE);
        Texture badge=HeirMod.texture(Data.row("classes",HeirMod.heir().classId).s("asset")+".png");sb.draw(badge,drawX-26*Settings.scale,drawY+190*Settings.scale,52*Settings.scale,52*Settings.scale);
    }
}
