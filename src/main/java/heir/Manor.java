package heir;
import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.Input;
import com.badlogic.gdx.graphics.*;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import com.megacrit.cardcrawl.core.*;
import com.megacrit.cardcrawl.helpers.FontHelper;
import com.megacrit.cardcrawl.helpers.input.InputHelper;
import com.megacrit.cardcrawl.screens.mainMenu.MainMenuScreen;
import java.util.*;

/** Family UI is available between runs; purchases never change an active heir. */
public final class Manor {
    public static boolean open=false,autoOpen=false;
    private static Texture pixel;
    public static String message="";
    private static final Color[] COLORS={new Color(.9f,.73f,.4f,1),new Color(.47f,.76f,.85f,1),new Color(.8f,.5f,.62f,1),new Color(.57f,.8f,.59f,1)};
    public static Color bannerColor(){return COLORS[HeirMod.profile.banner%COLORS.length];}
    private static boolean menu(){return CardCrawlGame.mode==CardCrawlGame.GameMode.CHAR_SELECT&&CardCrawlGame.mainMenuScreen!=null&&CardCrawlGame.mainMenuScreen.screen==MainMenuScreen.CurScreen.MAIN_MENU;}
    private static boolean hit(float x,float y,float w,float h){float s=Settings.scale;return InputHelper.mX>=x*s&&InputHelper.mX<=(x+w)*s&&InputHelper.mY>=y*s&&InputHelper.mY<=(y+h)*s;}
    public static void update(){
        if(!menu()){open=false;return;}
        if(autoOpen){open=true;autoOpen=false;}
        if(!open){if(HeirMod.profile.generation>0&&InputHelper.justClickedLeft&&hit(1390,790,390,72)){open=true;InputHelper.justClickedLeft=false;}return;}
        if(InputHelper.pressedEscape){open=false;InputHelper.pressedEscape=false;return;}
        if(!InputHelper.justClickedLeft)return;
        if(hit(1510,135,220,60)){open=false;}
        else if(hit(260,800,240,54)){
            Gdx.input.getTextInput(new Input.TextInputListener(){public void input(String text){String t=text.trim();if(t.length()>0){HeirMod.profile.family=t.substring(0,Math.min(24,t.length()));HeirMod.profile.save(HeirMod.profilePath);}}public void canceled(){}},"Family name",HeirMod.profile.family,"Up to 24 characters");
        }else if(hit(520,800,240,54)){HeirMod.profile.banner=(HeirMod.profile.banner+1)%COLORS.length;HeirMod.profile.save(HeirMod.profilePath);}
        else{
            for(int i=0;i<HeirMod.profile.offers.size();i++)if(hit(260+i*465,475,435,280)){
                if(HeirMod.profile.active==null){HeirMod.profile.selected=HeirMod.profile.offers.get(i);message="Next heir: "+HeirMod.profile.selected.name;HeirMod.profile.save(HeirMod.profilePath);}else message="Finish or abandon the current climb first.";
            }
            int i=0;for(Data.Row row:Data.rows("manor")){float x=260+(i%3)*465,y=345-(i/3)*125;if(hit(x,y,435,105)){if(HeirMod.profile.purchase(row.s("id"))){message=row.s("name")+" upgraded.";HeirMod.profile.save(HeirMod.profilePath);}else message="Need crowns, prerequisite or an available upgrade level.";}i++;}
        }
        InputHelper.justClickedLeft=false;
    }
    private static void rect(SpriteBatch sb,float x,float y,float w,float h,Color color){if(pixel==null){Pixmap p=new Pixmap(1,1,Pixmap.Format.RGBA8888);p.setColor(Color.WHITE);p.fill();pixel=new Texture(p);p.dispose();}sb.setColor(color);sb.draw(pixel,x*Settings.scale,y*Settings.scale,w*Settings.scale,h*Settings.scale);sb.setColor(Color.WHITE);}
    private static void text(SpriteBatch sb,String str,float x,float y,Color color){FontHelper.renderFontLeftTopAligned(sb,FontHelper.tipBodyFont,str,x*Settings.scale,y*Settings.scale,color);}
    private static void button(SpriteBatch sb,String str,float x,float y,float w,float h){rect(sb,x,y,w,h,new Color(.15f,.2f,.29f,1));text(sb,str,x+18,y+h-15,bannerColor());}
    public static void render(SpriteBatch sb){
        if(!menu())return;
        if(!open){if(HeirMod.profile.generation>0)button(sb,"Family Manor  |  "+HeirMod.profile.crowns+" crowns",1390,790,390,72);return;}
        rect(sb,0,0,1920,1080,new Color(.035f,.055f,.09f,.97f));rect(sb,225,105,1470,870,new Color(.075f,.105f,.16f,1));rect(sb,225,970,1470,5,bannerColor());
        FontHelper.renderFontLeftTopAligned(sb,FontHelper.panelNameFont,"HOUSE "+HeirMod.profile.family.toUpperCase(),260*Settings.scale,940*Settings.scale,bannerColor());
        text(sb,"Generation "+HeirMod.profile.generation+"  |  "+HeirMod.profile.crowns+" legacy crowns  |  Last climb +"+HeirMod.profile.lastEarned,260,880,Color.WHITE);
        button(sb,"Name your family",260,800,240,54);button(sb,"Change banner",520,800,240,54);
        int i=0;for(Profile.Heir h:HeirMod.profile.offers){float x=260+i*465;boolean selected=HeirMod.profile.selected!=null&&HeirMod.profile.selected.name.equals(h.name)&&HeirMod.profile.selected.classId.equals(h.classId);
            rect(sb,x,475,435,280,selected?new Color(.2f,.23f,.24f,1):new Color(.11f,.15f,.22f,1));Data.Row cls=Data.row("classes",h.classId);sb.setColor(Color.WHITE);sb.draw(HeirMod.texture(cls.s("asset")+".png"),(x+20)*Settings.scale,680*Settings.scale,54*Settings.scale,54*Settings.scale);
            text(sb,h.name+" "+HeirMod.profile.family,x+88,727,bannerColor());text(sb,cls.s("name")+(selected?"  [CHOSEN]":"  [SELECT]"),x+88,688,Color.WHITE);
            int j=0;for(String id:h.traits){Data.Row t=Data.row("traits",id);sb.draw(HeirMod.texture(t.s("asset")+".png"),(x+20)*Settings.scale,(610-j*64)*Settings.scale,32*Settings.scale,32*Settings.scale);text(sb,t.s("name"),x+62,642-j*64,Color.WHITE);text(sb,t.s("summary"),x+20,616-j*64,Color.LIGHT_GRAY);TraitTips.hover(id,(x+16)*Settings.scale,(587-j*64)*Settings.scale,403*Settings.scale,55*Settings.scale);j++;}
            text(sb,"HP "+heirHp(h)+"  |  +"+goldBonus(h)+"% legacy earnings",x+20,510,bannerColor());i++;
        }
        i=0;for(Data.Row row:Data.rows("manor")){float x=260+(i%3)*465,y=345-(i/3)*125;rect(sb,x,y,435,105,new Color(.11f,.15f,.22f,1));int lv=HeirMod.profile.level(row.s("id"));text(sb,row.s("name")+"  "+lv+"/"+row.i("maxLevel"),x+16,y+87,bannerColor());text(sb,row.s("summary"),x+16,y+56,Color.WHITE);String status=lv==row.i("maxLevel")?"MAX":HeirMod.profile.cost(row)+" crowns";if(!row.s("requires").equals("none")&&HeirMod.profile.level(row.s("requires"))==0)status="Requires "+Data.row("manor",row.s("requires")).s("name");text(sb,status,x+16,y+26,Color.LIGHT_GRAY);i++;}
        text(sb,message.isEmpty()?"Select an heir and buy upgrades. Then start a new run as The Heir.":message,260,167,Color.LIGHT_GRAY);button(sb,"Return",1510,135,150,60);
    }
    public static int heirHp(Profile.Heir h){int hp=Data.row("classes",h.classId).i("hp")+HeirMod.profile.bonus("hp");for(String t:h.traits)hp+=Data.row("traits",t).i("hp");return Math.max(20,hp);}
    private static int goldBonus(Profile.Heir h){int n=HeirMod.profile.bonus("gold");for(String t:h.traits)n+=Data.row("traits",t).i("goldBonus");return n;}
}
