package heir;
import com.badlogic.gdx.*;
import com.badlogic.gdx.graphics.*;
import com.badlogic.gdx.graphics.g2d.*;
import com.megacrit.cardcrawl.core.*;
import com.megacrit.cardcrawl.helpers.FontHelper;
import com.megacrit.cardcrawl.helpers.input.InputHelper;
import com.megacrit.cardcrawl.helpers.controller.*;
import com.megacrit.cardcrawl.screens.mainMenu.MainMenuScreen;
import java.util.*;

/** Between-run editor: purchases are a preview until explicitly saved. */
public final class Manor {
    public static boolean open=false,autoOpen=false,settingsOnly=false;
    private static int focus=0,modalFocus=0,detail=-1,scroll=0;
    private static boolean keyboardFocus=false,confirmInput=false,cancelInput=false;
    private static Texture pixel;
    private static String editor="",value="";
    private static InputProcessor previousInput;
    public static String message="";
    private static final List<String> staged=new ArrayList<>();
    private static final Color[] COLORS={new Color(.9f,.73f,.4f,1),new Color(.47f,.76f,.85f,1),new Color(.8f,.5f,.62f,1),new Color(.57f,.8f,.59f,1)};
    public static Color bannerColor(){return COLORS[Math.floorMod(HeirMod.profile.banner,COLORS.length)];}
    public static String bannerName(){return new String[]{"Sun","Moon","Rose","Oak"}[Math.floorMod(HeirMod.profile.banner,4)];}
    private static boolean menu(){
        if(CardCrawlGame.mode!=CardCrawlGame.GameMode.CHAR_SELECT||CardCrawlGame.mainMenuScreen==null||CardCrawlGame.mainMenuScreen.screen!=MainMenuScreen.CurScreen.CHAR_SELECT)return false;
        for(com.megacrit.cardcrawl.screens.charSelect.CharacterOption option:CardCrawlGame.mainMenuScreen.charSelectScreen.options)if(option.selected&&option.c instanceof HeirPlayer)return true;
        return false;
    }
    public static void openSelection(){
        CardCrawlGame.mainMenuScreen.charSelectScreen.open(false);
        for(com.megacrit.cardcrawl.screens.charSelect.CharacterOption option:CardCrawlGame.mainMenuScreen.charSelectScreen.options){option.selected=option.c instanceof HeirPlayer;if(option.selected){CardCrawlGame.chosenCharacter=HeirMod.Enums.HEIR;option.locked=false;}}
        CardCrawlGame.mainMenuScreen.charSelectScreen.justSelected();
        CardCrawlGame.mainMenuScreen.charSelectScreen.bgCharImg=com.megacrit.cardcrawl.helpers.ImageMaster.loadImage(Data.row("ui_art","select_bg").s("path"));
    }
    private static boolean hit(float x,float y,float w,float h){float s=Settings.scale;return InputHelper.mX>=x*s&&InputHelper.mX<=(x+w)*s&&InputHelper.mY>=y*s&&InputHelper.mY<=(y+h)*s;}
    private static boolean key(int code,CInputAction action){boolean pressed=Gdx.input.isKeyJustPressed(code)||(action!=null&&action.isJustPressed());if(pressed&&action!=null)action.unpress();return pressed;}
    public static int pendingCost(){Profile trial=HeirMod.profile.previewPurchases(staged);return trial==null?0:HeirMod.profile.crowns-trial.crowns;}
    public static int pendingCount(){return staged.size();}
    public static boolean stage(String id){List<String> next=new ArrayList<>(staged);next.add(id);if(HeirMod.profile.previewPurchases(next)==null){message="Need crowns, prerequisite or an available level.";return false;}staged.add(id);message="Upgrade selected. Save purchases to confirm.";return true;}
    public static void cancelPurchases(){staged.clear();message="Selections cleared. No crowns spent.";}
    public static boolean savePurchases(){boolean saved;try{saved=HeirMod.profile.commitPurchases(staged,HeirMod.profilePath);}catch(RuntimeException e){message="Save failed. No crowns spent; see game log.";e.printStackTrace();return false;}if(!saved){message="No valid purchases to save.";return false;}staged.clear();message="Purchases saved.";return true;}
    public static void beginEditor(String kind){
        if(kind.equals("crowns")&&!Data.row("systems","playtest_crowns").b("enabled"))return;editor=kind;value=kind.equals("family")?HeirMod.profile.family:kind.equals("crowns")?Integer.toString(HeirMod.profile.crowns):"";modalFocus=0;confirmInput=cancelInput=false;
        previousInput=Gdx.input.getInputProcessor();
        Gdx.input.setInputProcessor(new InputAdapter(){
            public boolean keyDown(int code){if(code==Input.Keys.A&&(Gdx.input.isKeyPressed(Input.Keys.CONTROL_LEFT)||Gdx.input.isKeyPressed(Input.Keys.CONTROL_RIGHT)))value="";if(code==Input.Keys.ENTER)confirmInput=true;if(code==Input.Keys.ESCAPE)cancelInput=true;return true;}
            public boolean keyTyped(char c){type(c);return true;}
        });
    }
    public static void type(char c){if(!editor.equals("family")&&!editor.equals("crowns"))return;if(c=='\b'){if(!value.isEmpty())value=value.substring(0,value.length()-1);return;}if(editor.equals("crowns")){if(Character.isDigit(c)&&value.length()<10)value+=c;}else if((Character.isLetterOrDigit(c)||c==' '||c=='-'||c=='_')&&value.length()<24)value+=c;}
    public static String editorValue(){return value;}
    public static void closeEditor(){if(!editor.isEmpty()){Gdx.input.setInputProcessor(previousInput);previousInput=null;}editor="";confirmInput=cancelInput=false;}
    public static boolean confirmEditor(){
        if(editor.equals("family")){String name=value.trim();if(name.isEmpty()){message="Enter a family name.";return false;}String old=HeirMod.profile.family;HeirMod.profile.family=name;try{HeirMod.profile.save(HeirMod.profilePath);}catch(RuntimeException e){HeirMod.profile.family=old;throw e;}message="Family name saved.";}
        else if(editor.equals("crowns")){try{int crowns=Integer.parseInt(value);if(crowns<0)throw new NumberFormatException();int old=HeirMod.profile.crowns;HeirMod.profile.crowns=crowns;try{HeirMod.profile.save(HeirMod.profilePath);}catch(RuntimeException e){HeirMod.profile.crowns=old;throw e;}staged.clear();message="Playtest crowns set to "+crowns+".";}catch(NumberFormatException e){message="Enter a whole number from 0 to 2147483647.";return false;}}
        else if(editor.equals("purchase")){if(!savePurchases())return false;}
        else if(editor.equals("discard")){cancelPurchases();open=false;}
        closeEditor();return true;
    }
    private static String keys(){return editor.equals("crowns")?"0123456789":"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";}
    private static int editButtons(){return (editor.equals("family")||editor.equals("crowns"))?keys().length()+5:2;}
    private static float[] editPoint(int index){int n=keys().length();if(!editor.equals("family")&&!editor.equals("crowns"))return index==0?new float[]{1140,250}:new float[]{1370,250};if(index<n)return new float[]{500+(index%10)*90,590-(index/10)*62};if(index==n)return new float[]{545,290};if(index==n+1)return new float[]{735,290};if(index==n+2)return new float[]{925,290};return index==n+3?new float[]{1140,250}:new float[]{1370,250};}
    private static void updateEditor(){
        if(cancelInput||InputHelper.pressedEscape||key(Input.Keys.ESCAPE,CInputActionSet.cancel)){closeEditor();InputHelper.pressedEscape=false;return;}
        int count=editButtons();
        if(key(Input.Keys.RIGHT,CInputActionSet.right)||key(Input.Keys.DOWN,CInputActionSet.down)||Gdx.input.isKeyJustPressed(Input.Keys.TAB))modalFocus=(modalFocus+1)%count;
        if(key(Input.Keys.LEFT,CInputActionSet.left)||key(Input.Keys.UP,CInputActionSet.up))modalFocus=(modalFocus+count-1)%count;
        if(confirmInput){confirmInput=false;confirmEditor();return;}
        if(key(Input.Keys.F11,CInputActionSet.select)||key(Input.Keys.F12,CInputActionSet.proceed)){float[] point=editPoint(modalFocus);InputHelper.mX=(int)(point[0]*Settings.scale);InputHelper.mY=(int)(point[1]*Settings.scale);InputHelper.justClickedLeft=true;}
        if(!InputHelper.justClickedLeft)return;
        if(hit(1040,220,200,60))confirmEditor();else if(hit(1260,220,200,60))closeEditor();
        else if(editor.equals("family")||editor.equals("crowns")){String chars=keys();for(int i=0;i<chars.length();i++){float[] point=editPoint(i);if(hit(point[0]-40,point[1]-25,80,50))type(chars.charAt(i));}if(hit(460,265,170,50))type(' ');if(hit(640,265,190,50))type('\b');if(hit(850,265,150,50))value="";}
        InputHelper.justClickedLeft=false;
    }
    public static void showDetails(int index){detail=index;scroll=0;}
    public static void update(){
        if(autoOpen&&CardCrawlGame.mode==CardCrawlGame.GameMode.CHAR_SELECT&&CardCrawlGame.mainMenuScreen!=null&&CardCrawlGame.mainMenuScreen.screen==MainMenuScreen.CurScreen.MAIN_MENU){openSelection();open=true;settingsOnly=false;autoOpen=false;}
        if(!menu()){open=false;detail=-1;closeEditor();staged.clear();return;}
        if(!open){if(Gdx.input.isKeyJustPressed(Input.Keys.M)||Gdx.input.isKeyJustPressed(Input.Keys.I)){settingsOnly=Gdx.input.isKeyJustPressed(Input.Keys.I);open=true;focus=0;keyboardFocus=true;}else if(InputHelper.justClickedLeft&&(hit(170,420,330,50)||hit(170,355,330,50))){settingsOnly=hit(170,355,330,50);open=true;InputHelper.justClickedLeft=false;}return;}
        if(!editor.isEmpty()){updateEditor();return;}
        if(detail>=0){int max=Math.max(0,detailLines().size()-20);if(InputHelper.scrolledDown||key(Input.Keys.DOWN,CInputActionSet.down))scroll=Math.min(max,scroll+1);if(InputHelper.scrolledUp||key(Input.Keys.UP,CInputActionSet.up))scroll=Math.max(0,scroll-1);if(InputHelper.pressedEscape||key(Input.Keys.ENTER,CInputActionSet.select)||key(Input.Keys.ESCAPE,CInputActionSet.cancel)||(InputHelper.justClickedLeft&&hit(1510,135,150,60))){detail=-1;InputHelper.pressedEscape=false;InputHelper.justClickedLeft=false;}return;}
        if(InputHelper.pressedEscape||key(Input.Keys.ESCAPE,CInputActionSet.cancel)){if(staged.isEmpty())open=false;else beginEditor("discard");InputHelper.pressedEscape=false;return;}
        int count=settingsOnly?4:18;
        if(key(Input.Keys.RIGHT,CInputActionSet.right)||key(Input.Keys.DOWN,CInputActionSet.down)||Gdx.input.isKeyJustPressed(Input.Keys.TAB)){focus=(focus+1)%count;keyboardFocus=true;}
        if(key(Input.Keys.LEFT,CInputActionSet.left)||key(Input.Keys.UP,CInputActionSet.up)){focus=(focus+count-1)%count;keyboardFocus=true;}
        if(key(Input.Keys.ENTER,CInputActionSet.select)||key(Input.Keys.F12,CInputActionSet.proceed)){float[] point=focusPoint();InputHelper.mX=(int)(point[0]*Settings.scale);InputHelper.mY=(int)(point[1]*Settings.scale);InputHelper.justClickedLeft=true;}
        if(!InputHelper.justClickedLeft)return;
        if(hit(1510,135,150,60)){if(staged.isEmpty())open=false;else beginEditor("discard");}
        else if(hit(260,800,240,54))beginEditor("family");
        else if(hit(520,800,240,54)){HeirMod.profile.banner=(HeirMod.profile.banner+1)%4;HeirMod.profile.save(HeirMod.profilePath);message="Banner: "+bannerName()+". Shown on your heir during combat.";}
        else if(Data.row("systems","playtest_crowns").b("enabled")&&hit(780,800,330,54))beginEditor("crowns");
        else if(!settingsOnly){
            for(int i=0;i<HeirMod.profile.offers.size();i++)if(hit(260+i*465,475,435,280)){
                if(hit(260+i*465,520,435,140)){showDetails(i);break;}
                if(HeirMod.profile.active==null){HeirMod.profile.selected=HeirMod.profile.offers.get(i);message="Next heir: "+HeirMod.profile.selected.name;HeirMod.profile.save(HeirMod.profilePath);}else message="Finish or abandon the current climb first.";
            }
            int i=0;for(Data.Row row:Data.rows("manor")){float x=260+(i%3)*465,y=345-(i/3)*125;if(hit(x,y,435,105))stage(row.s("id"));i++;}
            if(hit(1080,135,260,60)&&!staged.isEmpty())beginEditor("purchase");
            if(hit(820,135,240,60))cancelPurchases();
        }
        InputHelper.justClickedLeft=false;
    }
    private static void rect(SpriteBatch sb,float x,float y,float w,float h,Color color){if(pixel==null){Pixmap p=new Pixmap(1,1,Pixmap.Format.RGBA8888);p.setColor(Color.WHITE);p.fill();pixel=new Texture(p);p.dispose();}sb.setColor(color);sb.draw(pixel,x*Settings.scale,y*Settings.scale,w*Settings.scale,h*Settings.scale);sb.setColor(Color.WHITE);}
    private static void text(SpriteBatch sb,String str,float x,float y,Color color){FontHelper.renderFontLeftTopAligned(sb,FontHelper.tipBodyFont,str,x*Settings.scale,y*Settings.scale,color);}
    private static void button(SpriteBatch sb,String str,float x,float y,float w,float h){rect(sb,x,y,w,h,new Color(.15f,.2f,.29f,1));text(sb,str,x+18,y+h-15,bannerColor());}
    private static float[] focusPoint(){if(focus<3)return new float[]{380+focus*260,825};if(settingsOnly||focus==17)return new float[]{1585,165};if(focus<9){int n=focus-3;return new float[]{475+(n/2)*465,n%2==0?700:580};}if(focus<15){int n=focus-9;return new float[]{475+(n%3)*465,395-(n/3)*125};}return focus==15?new float[]{1200,165}:new float[]{940,165};}
    private static List<String> wrap(String text,float width){List<String> lines=new ArrayList<>();String part="";for(String word:text.split(" ")){String next=part.isEmpty()?word:part+" "+word;if(new GlyphLayout(FontHelper.tipBodyFont,next).width>width*Settings.scale&&!part.isEmpty()){lines.add(part);part=word;}else part=next;}if(!part.isEmpty())lines.add(part);return lines;}
    private static List<String> detailLines(){List<String> lines=new ArrayList<>();Profile.Heir h=HeirMod.profile.offers.get(detail);for(String id:h.traits){Data.Row row=Data.row("traits",id);lines.add(row.s("name"));lines.addAll(wrap(row.s("summary"),1250));for(String effect:TraitTips.effects(id))if(!row.s("summary").contains(effect))lines.addAll(wrap(effect,1250));lines.add("");}return lines;}
    private static void renderModal(SpriteBatch sb){
        if(detail>=0){rect(sb,225,105,1470,870,new Color(.035f,.055f,.09f,1));text(sb,HeirMod.profile.offers.get(detail).name+" - full trait details",260,930,bannerColor());List<String> lines=detailLines();float y=860;for(int i=scroll;i<Math.min(lines.size(),scroll+20);i++){text(sb,lines.get(i),280,y,Color.WHITE);y-=31;}text(sb,"Scroll / Up / Down to read. Enter / Back to close.",260,205,Color.LIGHT_GRAY);button(sb,"Return",1510,135,150,60);return;}
        if(editor.isEmpty())return;
        rect(sb,225,105,1470,870,new Color(.035f,.055f,.09f,1));String title=editor.equals("family")?"Name your family":editor.equals("crowns")?"PLAYTEST - Set crowns":editor.equals("purchase")?"Confirm manor purchases":"Discard pending purchases?";text(sb,title,460,900,bannerColor());
        if(editor.equals("family")||editor.equals("crowns")){rect(sb,460,740,1000,70,new Color(.15f,.2f,.29f,1));text(sb,value+"_",480,785,Color.WHITE);text(sb,editor.equals("family")?"Type, use Steam+X, or choose letters below. Save to confirm.":"Whole number: 0 to 2147483647. Temporary progression test control.",460,700,Color.LIGHT_GRAY);String chars=keys();for(int i=0;i<chars.length();i++){float[] point=editPoint(i);button(sb,chars.substring(i,i+1),point[0]-40,point[1]-25,80,50);}button(sb,"Space",460,265,170,50);button(sb,"Backspace",640,265,190,50);button(sb,"Clear",850,265,150,50);}
        else {text(sb,"Selected levels: "+staged.size()+"   Total: "+pendingCost()+" crowns   Remaining: "+(HeirMod.profile.crowns-pendingCost()),460,810,Color.WHITE);Map<String,Integer> counts=new LinkedHashMap<>();for(String id:staged)counts.put(id,counts.containsKey(id)?counts.get(id)+1:1);float y=740;for(String id:counts.keySet()){text(sb,Data.row("manor",id).s("name")+" +"+counts.get(id),460,y,Color.WHITE);y-=45;}text(sb,editor.equals("purchase")?"No crowns are spent until Confirm save.":"Discarding spends no crowns.",460,380,Color.LIGHT_GRAY);}
        text(sb,message,460,185,Color.LIGHT_GRAY);button(sb,editor.equals("purchase")?"Confirm save":editor.equals("discard")?"Discard":"Save",1040,220,200,60);button(sb,"Cancel",1260,220,200,60);float[] point=editPoint(modalFocus);rect(sb,point[0]-5,point[1]-5,10,10,bannerColor());
    }
    public static void render(SpriteBatch sb){
        if(!menu())return;
        if(!open){button(sb,"Family Manor  |  "+HeirMod.profile.crowns+" crowns",170,420,330,50);button(sb,"Heir Settings",170,355,330,50);return;}
        rect(sb,0,0,1920,1080,new Color(.035f,.055f,.09f,.97f));rect(sb,225,105,1470,870,new Color(.075f,.105f,.16f,1));rect(sb,225,970,1470,5,bannerColor());
        FontHelper.renderFontLeftTopAligned(sb,FontHelper.panelNameFont,"HOUSE "+HeirMod.profile.family.toUpperCase(),260*Settings.scale,940*Settings.scale,bannerColor());
        text(sb,"Generation "+HeirMod.profile.generation+"  |  "+HeirMod.profile.crowns+" crowns  |  Last climb +"+HeirMod.profile.lastEarned+"  |  Banner: "+bannerName(),260,880,Color.WHITE);
        button(sb,"Name your family",260,800,240,54);button(sb,"Change banner",520,800,240,54);if(Data.row("systems","playtest_crowns").b("enabled"))button(sb,"PLAYTEST: Set crowns",780,800,330,54);
        if(settingsOnly){text(sb,"Family identity appears on your heir and banner during combat.",260,740,Color.WHITE);text(sb,"Use arrows / D-pad to select, Enter / A to activate.",260,680,Color.LIGHT_GRAY);button(sb,"Return",1510,135,150,60);}
        else {
            int i=0;for(Profile.Heir h:HeirMod.profile.offers){float x=260+i*465;boolean selected=HeirMod.profile.selected!=null&&HeirMod.profile.selected.name.equals(h.name)&&HeirMod.profile.selected.classId.equals(h.classId)&&HeirMod.profile.selected.traits.equals(h.traits);
                rect(sb,x,475,435,280,selected?new Color(.2f,.23f,.24f,1):new Color(.11f,.15f,.22f,1));Data.Row cls=Data.row("classes",h.classId);sb.setColor(Color.WHITE);sb.draw(HeirMod.texture(cls.s("asset")+".png"),(x+20)*Settings.scale,680*Settings.scale,54*Settings.scale,54*Settings.scale);
                text(sb,h.name+" "+HeirMod.profile.family,x+88,727,bannerColor());text(sb,cls.s("name")+(selected?"  [CHOSEN]":"  [SELECT]"),x+88,688,Color.WHITE);
                int j=0;for(String id:h.traits){Data.Row t=Data.row("traits",id);sb.draw(HeirMod.texture(t.s("asset")+".png"),(x+20)*Settings.scale,(608-j*55)*Settings.scale,32*Settings.scale,32*Settings.scale);text(sb,t.s("name"),x+62,638-j*55,Color.WHITE);TraitTips.hover(id,(x+16)*Settings.scale,(587-j*55)*Settings.scale,403*Settings.scale,55*Settings.scale);j++;}
                text(sb,"Read full traits  [click / A]",x+20,535,Color.LIGHT_GRAY);text(sb,"HP "+heirHp(h)+"  |  +"+goldBonus(h)+"% legacy",x+20,495,bannerColor());i++;
            }
            Profile preview=HeirMod.profile.previewPurchases(staged);if(preview==null)preview=HeirMod.profile;
            i=0;for(Data.Row row:Data.rows("manor")){float x=260+(i%3)*465,y=345-(i/3)*125;rect(sb,x,y,435,105,new Color(.11f,.15f,.22f,1));int lv=preview.level(row.s("id"));text(sb,row.s("name")+" "+lv+"/"+row.i("maxLevel")+(lv>HeirMod.profile.level(row.s("id"))?" [pending]":""),x+16,y+87,bannerColor());text(sb,row.s("summary"),x+16,y+56,Color.WHITE);String status=lv==row.i("maxLevel")?"MAX":preview.cost(row)+" crowns";if(!row.s("requires").equals("none")&&preview.level(row.s("requires"))==0)status="Requires "+Data.row("manor",row.s("requires")).s("name");text(sb,status,x+16,y+26,Color.LIGHT_GRAY);i++;}
            text(sb,"Pending: "+pendingCost()+" crowns  |  Left after Save: "+preview.crowns,260,180,Color.LIGHT_GRAY);text(sb,message.isEmpty()?"Select upgrades, then Save purchases.":message,260,140,Color.LIGHT_GRAY);button(sb,"Clear selections",820,135,240,60);button(sb,"Save purchases",1080,135,260,60);button(sb,"Return",1510,135,150,60);
        }
        if(keyboardFocus){float[] point=focusPoint();rect(sb,point[0]-6,point[1]-6,12,12,bannerColor());}
        if(detail>=0||!editor.isEmpty()){TraitTips.hovered=null;renderModal(sb);}
    }
    public static int heirHp(Profile.Heir h){int hp=Data.row("classes",h.classId).i("hp")+HeirMod.profile.bonus("hp");for(String t:h.traits)hp+=Data.row("traits",t).i("hp");for(String t:h.traits)if(Data.row("traits",t).s("effect").equals("fragile"))return 1;return Math.max(20,hp);}
    private static int goldBonus(Profile.Heir h){int n=HeirMod.profile.bonus("gold");for(String t:h.traits)n+=Data.row("traits",t).i("goldBonus");return n;}
}
