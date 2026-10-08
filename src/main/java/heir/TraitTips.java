package heir;

import com.badlogic.gdx.graphics.*;
import com.badlogic.gdx.graphics.g2d.*;
import com.megacrit.cardcrawl.core.Settings;
import com.megacrit.cardcrawl.helpers.FontHelper;
import com.megacrit.cardcrawl.helpers.input.InputHelper;
import java.util.*;

/** Exact trait effects come from the same rows used by combat and progression. */
public final class TraitTips {
    public static String hovered;
    private static Texture pixel;
    public static void begin(){hovered=null;}
    public static void hover(String id,float x,float y,float width,float height){
        if(InputHelper.mX>=x&&InputHelper.mX<=x+width&&InputHelper.mY>=y&&InputHelper.mY<=y+height)hovered=id;
    }
    private static String signed(int n){return (n>0?"+":"")+n;}
    public static List<String> effects(String id){
        Data.Row row=Data.row("traits",id);List<String> lines=new ArrayList<>();
        if(!row.s("effect").equals("none"))lines.addAll(Arrays.asList(row.s("summary").split("(?<=\\.) ")));
        if(row.i("hp")!=0)lines.add(signed(row.i("hp"))+" maximum HP.");
        if(row.i("strength")!=0)lines.add(signed(row.i("strength"))+" Strength (attack damage).");
        if(row.i("dexterity")!=0)lines.add(signed(row.i("dexterity"))+" Dexterity (Block from cards).");
        if(row.i("draw")!=0)lines.add("Draw "+row.i("draw")+" extra card each turn.");
        if(row.i("heal")!=0)lines.add("Heal "+row.i("heal")+" extra HP after combat.");
        lines.add("+"+row.i("goldBonus")+"% legacy crowns earned.");
        if(row.f("scale")!=1f)lines.add(Math.round(row.f("scale")*100)+"% character size (appearance only).");
        lines.add("Stacks with class, manor and other traits.");
        return lines;
    }
    public static void render(SpriteBatch sb){
        if(hovered==null||!Data.row("systems","trait_tooltips").b("enabled"))return;
        float s=Settings.scale,w=Data.row("systems","trait_tooltips").i("amount")*s;
        List<String> lines=new ArrayList<>();for(String line:effects(hovered)){String part="";for(String word:line.split(" ")){String next=part.isEmpty()?word:part+" "+word;if(new GlyphLayout(FontHelper.tipBodyFont,next).width>w-36*s&&!part.isEmpty()){lines.add(part);part=word;}else part=next;}if(!part.isEmpty())lines.add(part);}float h=(70+lines.size()*30)*s;
        float x=Math.max(12*s,Math.min(InputHelper.mX+24*s,Settings.WIDTH-w-12*s));
        float top=Math.max(h+12*s,Math.min(InputHelper.mY-16*s,Settings.HEIGHT-12*s));
        if(pixel==null){Pixmap p=new Pixmap(1,1,Pixmap.Format.RGBA8888);p.setColor(Color.WHITE);p.fill();pixel=new Texture(p);p.dispose();}
        sb.setColor(new Color(.02f,.03f,.05f,.98f));sb.draw(pixel,x,top-h,w,h);
        sb.setColor(Manor.bannerColor());sb.draw(pixel,x,top-3*s,w,3*s);sb.setColor(Color.WHITE);
        FontHelper.renderFontLeftTopAligned(sb,FontHelper.tipHeaderFont,Data.row("traits",hovered).s("name"),x+18*s,top-18*s,Manor.bannerColor());
        float y=top-55*s;for(String line:lines){FontHelper.renderFontLeftTopAligned(sb,FontHelper.tipBodyFont,line,x+18*s,y,Color.WHITE);y-=30*s;}
    }
}
