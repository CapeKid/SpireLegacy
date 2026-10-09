package heir;

import basemod.ReflectionHacks;
import com.evacipated.cardcrawl.modthespire.lib.*;
import com.megacrit.cardcrawl.cards.AbstractCard;
import com.megacrit.cardcrawl.cards.CardGroup;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.helpers.ModHelper;
import com.megacrit.cardcrawl.relics.AbstractRelic;
import java.util.*;

/** Finite reward selection for the heir's small class pools. */
@SpirePatch(clz=AbstractDungeon.class,method="getRewardCards")
public final class RewardPools {
    public static SpireReturn<ArrayList<AbstractCard>> Prefix(){
        if(!HeirMod.isHeir()||AbstractDungeon.player.hasRelic("PrismaticShard"))return SpireReturn.Continue();
        ArrayList<AbstractCard> rewards=new ArrayList<>();Set<String> used=new HashSet<>();
        int count=Data.row("systems","reward_choices").i("amount");
        for(AbstractRelic relic:AbstractDungeon.player.relics)count=relic.changeNumberOfCardsInReward(count);
        if(ModHelper.isModEnabled("Binary"))count--;
        for(int slot=0;slot<count;slot++){
            AbstractCard.CardRarity rarity=AbstractDungeon.rollRarity();
            if(rarity==AbstractCard.CardRarity.RARE)AbstractDungeon.cardBlizzRandomizer=AbstractDungeon.cardBlizzStartOffset;
            else if(rarity==AbstractCard.CardRarity.COMMON)AbstractDungeon.cardBlizzRandomizer=Math.max(AbstractDungeon.cardBlizzMaxOffset,AbstractDungeon.cardBlizzRandomizer-AbstractDungeon.cardBlizzGrowth);
            CardGroup pool=rarity==AbstractCard.CardRarity.RARE?AbstractDungeon.rareCardPool:rarity==AbstractCard.CardRarity.UNCOMMON?AbstractDungeon.uncommonCardPool:AbstractDungeon.commonCardPool;
            LinkedHashMap<String,AbstractCard> choices=new LinkedHashMap<>();
            for(AbstractCard card:pool.group)if(!used.contains(card.cardID))choices.put(card.cardID,card);
            // Preserve every relic-granted choice and boss rarity. Repeats are
            // needed only after all distinct cards in this rarity were offered.
            if(choices.isEmpty())for(AbstractCard card:pool.group)choices.put(card.cardID,card);
            if(choices.isEmpty())throw new IllegalStateException("Spire Legacy has no "+rarity+" reward cards for "+HeirMod.heir().classId);
            ArrayList<AbstractCard> candidates=new ArrayList<>(choices.values());
            AbstractCard selected=candidates.get(AbstractDungeon.cardRng.random(candidates.size()-1));
            used.add(selected.cardID);rewards.add(selected.makeCopy());
        }
        float upgradeChance=ReflectionHacks.getPrivateStatic(AbstractDungeon.class,"cardUpgradedChance");
        for(AbstractCard card:rewards){
            if(card.rarity!=AbstractCard.CardRarity.RARE&&AbstractDungeon.cardRng.randomBoolean(upgradeChance)&&card.canUpgrade())card.upgrade();
            else for(AbstractRelic relic:AbstractDungeon.player.relics)relic.onPreviewObtainCard(card);
        }
        return SpireReturn.Return(rewards);
    }
}
