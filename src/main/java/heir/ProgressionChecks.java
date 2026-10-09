package heir;
import java.util.*;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.Input;
import com.megacrit.cardcrawl.core.Settings;
import com.megacrit.cardcrawl.helpers.controller.CInputActionSet;
import com.megacrit.cardcrawl.helpers.CardLibrary;

/** Disposable native UI/input, atomic-cart, crown and baseline checks. */
public final class ProgressionChecks {
    private static int checks;
    private static void check(boolean ok,String why){checks++;if(!ok)throw new AssertionError(why);}
    private static void enter(String value){for(int i=0;i<24;i++)Gdx.input.getInputProcessor().keyTyped('\b');for(char c:value.toCharArray())Gdx.input.getInputProcessor().keyTyped(c);}
    private static int earnings(int floors,int gold,boolean win){Profile p=new Profile();p.generateOffers(11);p.selected=new Profile.Heir("Test","knight",new ArrayList<String>());Profile.Run run=p.begin(5);return p.settle(run.id,floors,gold,win);}
    public static void run()throws Exception{
        Profile original=HeirMod.profile;Path originalPath=HeirMod.profilePath;boolean controller=Settings.isControllerMode;
        try{
            checks=0;HeirMod.profile=new Profile();HeirMod.profile.generateOffers(1);HeirMod.profilePath=HeirMod.root.resolve("ui-check-family.json");Manor.openSelection();Manor.open=true;Manor.settingsOnly=false;Manor.cancelPurchases();
            Manor.beginEditor("family");enter("Cape House");check(HeirMod.profile.family.equals("Ashford"),"Typing does not save a name early");
            Gdx.input.getInputProcessor().keyDown(Input.Keys.ENTER);Manor.update();check(HeirMod.profile.family.equals("Cape House"),"Native text-input Enter saves family name");check(Profile.load(HeirMod.profilePath).family.equals("Cape House"),"Family name persisted");
            Manor.beginEditor("family");enter("Cancelled");Gdx.input.getInputProcessor().keyDown(Input.Keys.ESCAPE);Manor.update();check(HeirMod.profile.family.equals("Cape House"),"Cancel preserves family name");
            Manor.beginEditor("family");enter(" ");check(!Manor.confirmEditor(),"Blank family name rejected");Manor.closeEditor();
            Manor.beginEditor("crowns");enter("1000");check(Manor.confirmEditor(),"Crown editor accepts valid value");check(HeirMod.profile.crowns==1000&&Profile.load(HeirMod.profilePath).crowns==1000,"Crowns persist only on Save");
            Manor.beginEditor("crowns");enter("2147483648");check(!Manor.confirmEditor()&&HeirMod.profile.crowns==1000,"Overflow crown input rejected without mutation");Manor.closeEditor();
            check(Manor.stage("vitality")&&Manor.stage("smith"),"Staged prerequisites satisfy later staged levels");check(HeirMod.profile.crowns==1000&&HeirMod.profile.level("vitality")==0,"Selections spend no crowns and give no bonus");check(Manor.pendingCost()==200,"Cart uses sequential native upgrade prices");
            Manor.beginEditor("purchase");Manor.closeEditor();check(Manor.pendingCount()==2&&HeirMod.profile.crowns==1000,"Cancel confirmation preserves unspent cart");
            Manor.beginEditor("purchase");check(Manor.confirmEditor(),"Explicit confirmation commits purchases");check(HeirMod.profile.crowns==800&&HeirMod.profile.level("vitality")==1&&HeirMod.profile.level("smith")==1,"Batch levels and total applied once");check(Manor.pendingCount()==0,"Cart cleared after Save");check(Profile.load(HeirMod.profilePath).crowns==800,"Batch persisted together");
            check(Manor.stage("garden"),"Second cart staged");Manor.cancelPurchases();check(HeirMod.profile.crowns==800&&HeirMod.profile.level("garden")==0,"Clear discards all pending purchases");
            Profile p=new Profile();p.crowns=1000;check(p.previewPurchases(Arrays.asList("smith"))==null,"Missing prerequisite rejects whole batch");p.crowns=1;check(p.previewPurchases(Arrays.asList("vitality"))==null&&p.crowns==1,"Unaffordable batch has no partial effects");
            p.crowns=1000;Path blocked=HeirMod.root.resolve("blocked-profile");Files.createDirectories(blocked);Files.write(blocked.resolve("keep.txt"),new byte[]{1});boolean failed=false;try{p.commitPurchases(Arrays.asList("vitality"),blocked);}catch(RuntimeException expected){failed=true;}check(failed&&p.crowns==1000&&p.level("vitality")==0,"Failed persistence cannot spend crowns or add levels");
            check(earnings(8,80,false)==19,"Early failed run earnings");check(earnings(17,100,false)==38,"Act-one failed run earnings");check(earnings(50,200,true)==128,"Completed run earnings");
            p=new Profile();p.crowns=128;check(p.purchase("vitality"),"One completed run can buy an entry upgrade");check(!p.purchase("vault")&&!p.purchase("vitality"),"One normal completed run cannot buy multiple entry upgrades");
            p=new Profile();p.generateOffers(3);p.selected=new Profile.Heir("Test","knight",new ArrayList<String>());Profile.Run run=p.begin(4);check(p.settle(run.id,50,200,true)==128&&p.settle(run.id,50,200,true)==0,"Settlement remains idempotent");
            p=new Profile();p.generateOffers(3);p.selected=new Profile.Heir("Test","knight",new ArrayList<String>());p.crowns=Integer.MAX_VALUE;run=p.begin(4);p.settle(run.id,50,200,true);check(p.crowns==Integer.MAX_VALUE,"Playtest maximum crowns cannot overflow settlement");
            HeirMod.profile.selected=new Profile.Heir("Test","knight",new ArrayList<String>());
            for(String cls:Arrays.asList("knight","mage","ranger")){
                HeirMod.profile.selected.classId=cls;HeirCard strike=new HeirCard(cls+"_strike"),guard=new HeirCard(cls+"_guard");check(strike.baseDamage==5&&strike.baseBlock==0,"Plain weaker Strike: "+cls);check(guard.baseBlock==4&&!guard.rawDescription.contains("Draw"),"Plain weaker Defend: "+cls);strike.upgrade();guard.upgrade();check(strike.baseDamage==7&&guard.baseBlock==6,"Starter upgrades remain below native 9/8");
                int nativeHp=cls.equals("knight")?80:cls.equals("mage")?75:70;check(Data.row("classes",cls).i("hp")<nativeHp,"Low no-manor HP");check(Data.row("classes",cls).i("strength")==0&&Data.row("classes",cls).i("dexterity")==0&&Data.row("classes",cls).i("heal")==0,"No free stat/heal stacking");
                int fullHp=Data.row("classes",cls).i("hp")+20;check(fullHp>=70,"Living Quarters meaningfully restore health");
            }
            check(CardLibrary.getCard("Strike_R").baseDamage==6&&CardLibrary.getCard("Defend_R").baseBlock==5,"Owned native starting-card reference");
            HeirCard cold=new HeirCard("mage_cold_read");cold.upgrade();check(cold.cost==0&&cold.rawDescription.contains("Draw 2")&&cold.rawDescription.contains("Discard 2"),"Cold Read prints balanced upgraded exchange");
            check(new HeirCard("ranger_master_plan").rawDescription.contains("cannot draw"),"Free-hand draw limitation is visible");
            Settings.isControllerMode=true;java.lang.reflect.Field focus=Manor.class.getDeclaredField("focus");focus.setAccessible(true);focus.setInt(null,0);CInputActionSet.right.justPressed=true;Manor.update();check(focus.getInt(null)==1,"Controller navigation reaches banner");CInputActionSet.select.justPressed=true;int banner=HeirMod.profile.banner;Manor.update();check(HeirMod.profile.banner==(banner+1)%4,"Controller activates banner selection");
            Files.write(HeirMod.root.resolve("progression-checks.json"),("{\"assertions\":"+checks+",\"earlyDeathCrowns\":19,\"actOneDeathCrowns\":38,\"victoryCrowns\":128,\"result\":\"passed\"}").getBytes(StandardCharsets.UTF_8));
        }finally{Settings.isControllerMode=controller;Manor.closeEditor();Manor.cancelPurchases();Manor.open=false;HeirMod.profile=original;HeirMod.profilePath=originalPath;}
    }
}
