package heir;

import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Persistent family state. Run settlement is idempotent and every write is atomic. */
public final class Profile {
    public String family="Ashford";
    public int banner=0, generation=0, crowns=0, lastEarned=0;
    public Map<String,Integer> manor=new LinkedHashMap<>();
    public List<Heir> offers=new ArrayList<>();
    public Heir selected;
    public Run active;
    public String settledId="";
    public static final class Heir {
        public String name,classId;
        public List<String> traits=new ArrayList<>();
        public Heir(){}
        public Heir(String n,String c,List<String> t){name=n;classId=c;traits.addAll(t);}
    }
    public static final class Run {
        public String id;
        public Heir heir;
        public int floors=0;
        public Map<String,Integer> bonuses=new LinkedHashMap<>();
        public boolean quick=false;
    }
    private static final String[] NAMES={"Ada","Rowan","Mira","Alden","Kit","Iris","Bram","Sage","Wren","Theo","June","Vale"};
    public int level(String id){return manor.containsKey(id)?manor.get(id):0;}
    public int bonus(String effect){int n=0;for(Data.Row r:Data.rows("manor"))if(r.s("effect").equals(effect))n+=level(r.s("id"))*r.i("perLevel");return n;}
    public void generateOffers(long seed){
        Random rng=new Random(seed); offers.clear(); List<Data.Row> classes=new ArrayList<>(Data.rows("classes")); List<Data.Row> traits=new ArrayList<>();
        for(Data.Row trait:Data.rows("traits"))if(trait.b("enabled"))traits.add(trait);
        Collections.shuffle(classes,rng);
        for(Data.Row cls:classes){Collections.shuffle(traits,rng);Data.Row first=traits.get(0),second=null;for(Data.Row candidate:traits)if(compatible(first,candidate)){second=candidate;break;}if(second==null)throw new IllegalStateException("No compatible trait pair");offers.add(new Heir(NAMES[rng.nextInt(NAMES.length)],cls.s("id"),Arrays.asList(first.s("id"),second.s("id"))));}
    }
    public static boolean compatible(Data.Row a,Data.Row b){return !a.s("id").equals(b.s("id"))&&!a.list("excludes").contains(b.s("id"))&&!b.list("excludes").contains(a.s("id"));}
    public Run begin(long seed){
        if(active!=null && !active.id.equals(settledId))throw new IllegalStateException("An unfinished heir already exists. Resume or abandon it before starting another.");
        if(offers.isEmpty())generateOffers(seed);
        Run run=new Run();run.id=UUID.randomUUID().toString();run.heir=selected!=null?selected:offers.get(new Random(seed).nextInt(offers.size()));run.quick=generation==0;
        for(String effect:Arrays.asList("hp","strength","dexterity","draw","gold","heal"))run.bonuses.put(effect,bonus(effect));
        selected=null;active=run;return run;
    }
    public int settle(String runId,int floors,int gold,boolean victory){
        if(active==null || !active.id.equals(runId) || settledId.equals(runId))return 0;
        int percent=100+active.bonuses.get("gold");
        for(String id:active.heir.traits)percent+=Data.row("traits",id).i("goldBonus");
        int base=Math.max(0,floors)*Data.row("systems","legacy").i("amount")+Math.max(0,gold)/Data.row("systems","gold_conversion").i("amount")+(victory?Data.row("systems","victory").i("amount"):0);
        int earned=(int)Math.min(Integer.MAX_VALUE,(long)base*Math.min(200,Math.max(0,percent))/100);crowns=(int)Math.min(Integer.MAX_VALUE,(long)crowns+earned);lastEarned=earned;generation++;settledId=runId;active=null;
        generateOffers(generation*1009L+runId.hashCode());return earned;
    }
    public int cost(Data.Row row){return row.i("baseCost")+level(row.s("id"))*row.i("costStep");}
    public boolean purchase(String id){
        Data.Row r=Data.row("manor",id);String prerequisite=r.s("requires");int price=cost(r);
        if(active!=null || level(id)>=r.i("maxLevel") || crowns<price || (!prerequisite.equals("none") && level(prerequisite)==0))return false;
        crowns-=price;manor.put(id,level(id)+1);return true;
    }
    public Profile previewPurchases(List<String> ids){
        Profile trial=Data.GSON.fromJson(Data.GSON.toJson(this),Profile.class);
        for(String id:ids)if(!trial.purchase(id))return null;
        return trial;
    }
    /** Validate the whole cart and persist it before changing live balances. */
    public boolean commitPurchases(List<String> ids,Path path){
        Profile trial=previewPurchases(ids);if(trial==null||ids.isEmpty())return false;
        trial.save(path);crowns=trial.crowns;manor=new LinkedHashMap<>(trial.manor);return true;
    }
    public static Profile load(Path path){
        if(!Files.exists(path))return new Profile();
        try {return read(path);}catch(Exception first){
            try {Profile p=read(Paths.get(path+".bak")); System.err.println("Recovered family backup: "+first); return p;}
            catch(Exception second){throw new IllegalStateException("Family save is damaged; preserved both files. Restore the backup before continuing.",second);}
        }
    }
    private static Profile read(Path path)throws Exception{
        Profile p=Data.GSON.fromJson(new String(Files.readAllBytes(path),StandardCharsets.UTF_8),Profile.class);
        if(p==null||p.family==null||p.manor==null||p.offers==null||p.crowns<0)throw new IllegalStateException("Invalid profile");
        return p;
    }
    public void save(Path path){
        try{
            Files.createDirectories(path.getParent());Path tmp=Paths.get(path+".tmp");
            Files.write(tmp,Data.GSON.toJson(this).getBytes(StandardCharsets.UTF_8));
            if(Files.exists(path))Files.copy(path,Paths.get(path+".bak"),StandardCopyOption.REPLACE_EXISTING);
            try{Files.move(tmp,path,StandardCopyOption.REPLACE_EXISTING,StandardCopyOption.ATOMIC_MOVE);}catch(AtomicMoveNotSupportedException e){Files.move(tmp,path,StandardCopyOption.REPLACE_EXISTING);}
        }catch(Exception e){throw new IllegalStateException("Could not save family progression",e);}
    }
}
