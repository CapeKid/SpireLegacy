import heir.*;
import java.nio.file.*;
import java.util.*;
public class ProfileChecks {
    static int checks=0;
    static void check(boolean condition,String message){checks++;if(!condition)throw new AssertionError(message);}
    public static void main(String[] args)throws Exception {
        Profile p=new Profile();p.generateOffers(42);
        check(p.offers.size()==3,"three offers");Set<String> classes=new HashSet<>();
        for(Profile.Heir h:p.offers){classes.add(h.classId);check(h.traits.size()==2&&!h.traits.get(0).equals(h.traits.get(1)),"two distinct traits");}
        check(classes.size()==3,"one offer per class");p.selected=p.offers.get(1);String selected=p.selected.classId;
        Profile.Run r=p.begin(123);check(r.quick,"first run quick start");check(r.heir.classId.equals(selected),"selected heir retained");
        int earned=p.settle(r.id,16,100,true);check(earned>0&&p.generation==1,"victory settlement");int crowns=p.crowns;
        check(p.settle(r.id,16,100,true)==0&&p.crowns==crowns,"duplicate end callback cannot pay twice");
        check(!p.purchase("smith"),"prerequisite enforced");check(p.purchase("vitality"),"valid purchase");check(p.bonus("hp")==4,"upgrade stat");
        int before=p.crowns;p.begin(55);check(!p.active.quick,"later run not quick");check(!p.purchase("vault")&&p.crowns==before,"no purchases during active run");
        check(p.active.bonuses.get("hp")==4,"run snapshots upgrades");p.settle(p.active.id,3,10,false);
        Path path=Paths.get(args[0]);p.save(path);Profile loaded=Profile.load(path);check(loaded.generation==2&&loaded.crowns==p.crowns,"round-trip save");
        p.crowns+=1;p.save(path);Files.write(path,"broken".getBytes());Profile recovered=Profile.load(path);check(recovered.crowns==loaded.crowns,"backup recovery");
        Set<String> seen=new HashSet<>();
        for(int seed=0;seed<2000;seed++){Profile sample=new Profile();sample.generateOffers(seed);for(Profile.Heir h:sample.offers){Data.Row a=Data.row("traits",h.traits.get(0)),b=Data.row("traits",h.traits.get(1));check(a.b("enabled")&&b.b("enabled"),"disabled traits excluded");check(Profile.compatible(a,b),"incompatibilities respected");seen.addAll(h.traits);}}
        for(Data.Row t:Data.rows("traits"))if(t.b("enabled"))check(seen.contains(t.s("id")),"every eligible trait reachable: "+t.s("id"));
        check(!Profile.compatible(Data.row("traits","large"),Data.row("traits","small")),"size incompatibility");
        System.out.println("Passed "+checks+" progression checks.");
    }
}
