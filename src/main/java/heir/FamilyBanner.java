package heir;
import com.badlogic.gdx.graphics.*;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import com.megacrit.cardcrawl.core.Settings;

/** A family standard with a distinct emblem, visible independently of trait tint. */
public final class FamilyBanner {
    private static Texture pixel;
    private static void bar(SpriteBatch sb,float x,float y,float w,float h,Color color){sb.setColor(color);sb.draw(pixel,x,y,w*Settings.scale,h*Settings.scale);}
    public static void render(SpriteBatch sb,float x,float y){
        if(pixel==null){Pixmap p=new Pixmap(1,1,Pixmap.Format.RGBA8888);p.setColor(Color.WHITE);p.fill();pixel=new Texture(p);p.dispose();}
        float s=Settings.scale;Color ink=new Color(.16f,.12f,.09f,1),gold=new Color(.93f,.78f,.46f,1);
        bar(sb,x,y,5,140,ink);bar(sb,x+s,y,2,140,gold);
        bar(sb,x+5*s,y+67*s,58,65,ink);bar(sb,x+8*s,y+70*s,52,59,Manor.bannerColor());
        int emblem=Math.floorMod(HeirMod.profile.banner,4);float cx=x+32*s,cy=y+98*s;
        if(emblem==0){bar(sb,cx-6*s,cy-6*s,12,12,gold);bar(sb,cx-2*s,cy-20*s,4,40,gold);bar(sb,cx-20*s,cy-2*s,40,4,gold);}
        if(emblem==1){bar(sb,cx-13*s,cy-17*s,19,34,gold);bar(sb,cx-5*s,cy-12*s,18,24,Manor.bannerColor());}
        if(emblem==2){bar(sb,cx-7*s,cy-18*s,14,36,gold);bar(sb,cx-18*s,cy-7*s,36,14,gold);bar(sb,cx-4*s,cy-4*s,8,8,ink);}
        if(emblem==3){bar(sb,cx-3*s,cy-22*s,6,25,gold);bar(sb,cx-17*s,cy-2*s,34,20,gold);bar(sb,cx-10*s,cy+15*s,20,9,gold);}
        sb.setColor(Color.WHITE);
    }
}
