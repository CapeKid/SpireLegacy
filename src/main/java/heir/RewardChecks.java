package heir;

import java.util.*;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import com.megacrit.cardcrawl.cards.AbstractCard;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.relics.*;
import com.megacrit.cardcrawl.rooms.*;
import com.megacrit.cardcrawl.monsters.*;

/** Opt-in regression checks calling the patched native reward entry point. */
public final class RewardChecks {
    public static void run()throws Exception {
        Profile.Heir heir=HeirMod.heir();String originalClass=heir.classId;
        List<String> traits=new ArrayList<>(heir.traits);ArrayList<AbstractRelic> relics=new ArrayList<>(AbstractDungeon.player.relics);
        AbstractRoom originalRoom=AbstractDungeon.currMapNode.room;com.megacrit.cardcrawl.random.Random rng=AbstractDungeon.cardRng;
        int blizzard=AbstractDungeon.cardBlizzRandomizer,checks=0;
        try{
            heir.traits.clear();
            for(Data.Row cls:Data.rows("classes")){
                heir.classId=cls.s("id");com.megacrit.cardcrawl.core.CardCrawlGame.dungeon.initializeCardPools();
                for(boolean boss:new boolean[]{false,true})for(int relicSet=0;relicSet<5;relicSet++)for(int seed=0;seed<20;seed++){
                    AbstractRoom room=boss?new MonsterRoomBoss():new MonsterRoom();room.monsters=new MonsterGroup(new AbstractMonster[0]);AbstractDungeon.currMapNode.room=room;
                    AbstractDungeon.player.relics.clear();int expected=3;
                    if(relicSet==1||relicSet==3||relicSet==4){AbstractDungeon.player.relics.add(new QuestionCard());expected++;}
                    if(relicSet==2||relicSet==3){AbstractDungeon.player.relics.add(new BustedCrown());expected-=2;}
                    if(relicSet==4)AbstractDungeon.player.relics.add(new PrismaticShard());
                    AbstractDungeon.cardRng=new com.megacrit.cardcrawl.random.Random((long)seed);AbstractDungeon.cardBlizzRandomizer=AbstractDungeon.cardBlizzStartOffset;
                    ArrayList<AbstractCard> result=AbstractDungeon.getRewardCards();
                    if(result.size()!=expected)throw new AssertionError("Relic reward count: "+cls.s("id")+" / "+relicSet);checks++;
                    Set<String> seen=new HashSet<>();Map<AbstractCard.CardRarity,Set<String>> byRarity=new HashMap<>();
                    for(AbstractCard card:result){
                        if(boss&&card.rarity!=AbstractCard.CardRarity.RARE)throw new AssertionError("Boss reward rarity");checks++;
                        if(relicSet==4)continue;
                        if(!(card instanceof HeirCard)||!Data.row("cards",((HeirCard)card).key).s("classId").equals(cls.s("id")))throw new AssertionError("Wrong heir reward pool");checks++;
                        Set<String> same=byRarity.get(card.rarity);if(same==null){same=new HashSet<>();byRarity.put(card.rarity,same);}
                        int distinct=0;for(Data.Row row:Data.rows("cards"))if(row.s("classId").equals(cls.s("id"))&&row.s("rarity").equals(card.rarity.toString()))distinct++;
                        if(seen.contains(card.cardID)&&same.size()<distinct)throw new AssertionError("Duplicate before rarity exhaustion");checks++;
                        seen.add(card.cardID);same.add(card.cardID);
                    }
                }
            }
            Files.write(HeirMod.root.resolve("reward-checks.json"),("{\"rewardAssertions\":"+checks+",\"nativeRewardCalls\":600}").getBytes(StandardCharsets.UTF_8));
        }finally{
            heir.classId=originalClass;heir.traits.clear();heir.traits.addAll(traits);AbstractDungeon.player.relics.clear();AbstractDungeon.player.relics.addAll(relics);AbstractDungeon.currMapNode.room=originalRoom;AbstractDungeon.cardRng=rng;AbstractDungeon.cardBlizzRandomizer=blizzard;com.megacrit.cardcrawl.core.CardCrawlGame.dungeon.initializeCardPools();
        }
    }
}
