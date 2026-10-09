package heir;
import com.megacrit.cardcrawl.cards.AbstractCard;
import com.megacrit.cardcrawl.helpers.CardLibrary;
import java.util.*;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;

/** Read the owned game library as the balancing reference, including upgrades. */
public final class BalanceAudit {
    private static Map<String,Object> stats(AbstractCard card){Map<String,Object> row=new LinkedHashMap<>();row.put("id",card.cardID);row.put("name",card.name);row.put("rarity",card.rarity);row.put("cost",card.cost);row.put("damage",card.baseDamage);row.put("block",card.baseBlock);row.put("magic",card.baseMagicNumber);row.put("exhaust",card.exhaust);row.put("rules",card.rawDescription);return row;}
    public static void run()throws Exception{
        List<Map<String,Object>> rows=new ArrayList<>();
        for(AbstractCard card:CardLibrary.cards.values()){
            AbstractCard copy=card.makeCopy();Map<String,Object> row=new LinkedHashMap<>();row.put("base",stats(copy));copy.upgrade();row.put("upgrade",stats(copy));rows.add(row);
        }
        Files.write(HeirMod.root.resolve("balance-audit.json"),Data.GSON.toJson(rows).getBytes(StandardCharsets.UTF_8));
    }
}
