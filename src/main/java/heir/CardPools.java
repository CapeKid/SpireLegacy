package heir;
import java.util.*;

/** Draft membership is independent of a card's historical authoring class. */
public final class CardPools {
    private static final Map<String,Set<String>> pools=new HashMap<>();
    private static final Set<String> shared=new HashSet<>();
    static {
        for(Data.Row row:Data.rows("card_pools")){
            String cls=row.s("classId");
            if(!pools.containsKey(cls))pools.put(cls,new LinkedHashSet<String>());
            pools.get(cls).add(row.s("cardId"));
            if(row.s("scope").equals("shared"))shared.add(row.s("cardId"));
        }
    }
    public static boolean contains(String cls,String key){return pools.containsKey(cls)&&pools.get(cls).contains(key);}
    public static boolean shared(String key){return shared.contains(key);}
    public static boolean active(String key){for(Set<String> pool:pools.values())if(pool.contains(key))return true;return false;}
}
