using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Separate durable deck card and combat copy: retention must never buff the deck.
public static class CardFlushOracle
{
    public static async Task<object> Check(RunState run,string key,bool upgraded)
    {
        var player=run.Players.Single();var combat=player.Creature.CombatState!;
        var original=(LegacyCard)run.CreateCard(NativeCards.Get(key),player);if(upgraded)CardCmd.Upgrade(original);
        await CardPileCmd.Add(original,PileType.Deck,skipVisuals:true);
        var card=(LegacyCard)combat.CloneCard(original);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
        var row=Design.Get("cards",key);var turns=row.Flag("retain")&&!row.Flag("ethereal")?2:1;
        for(var i=1;i<=turns;i++) {
            await BalanceOracle.End(player,true);
            var expected=row.Flag("ethereal")?PileType.Exhaust:row.Flag("retain")?PileType.Hand:PileType.Discard;
            if(card.Pile?.Type!=expected)throw new InvalidDataException("Flush result differs: "+key);
            var growth=row.Text("special") is "retain_damage" or "retain_block"?i*card.DesignCard.SpecialAmount:0;
            if(card.RetainedGrowth!=growth||original.RetainedGrowth!=0)throw new InvalidDataException("Retention growth leaked to the deck: "+key);
            var loaded=(LegacyCard)run.LoadCard(card.ToSerializable(),player);
            if(loaded.RetainedGrowth!=card.RetainedGrowth||loaded.DynamicVars.Damage.BaseValue!=card.DynamicVars.Damage.BaseValue||loaded.DynamicVars.Block.BaseValue!=card.DynamicVars.Block.BaseValue)throw new InvalidDataException("Retained combat copy did not serialize: "+key);
            run.RemoveCard(loaded);
        }
        return new {key,upgraded,turns,pile=card.Pile?.Type.ToString(),growth=card.RetainedGrowth,deckGrowth=original.RetainedGrowth,deckPreserved=true};
    }
    public static object NextCombat(RunState run,string key)
    {
        var player=run.Players.Single();var cards=CardPile.GetCards(player,PileType.Draw,PileType.Hand,PileType.Discard,PileType.Exhaust).OfType<LegacyCard>().Where(c=>c.Key==key).ToArray();
        if(cards.Length!=1||cards[0].RetainedGrowth!=0||cards[0].DynamicVars.Damage.BaseValue!=cards[0].DesignCard.Damage||cards[0].DynamicVars.Block.BaseValue!=cards[0].DesignCard.Block)throw new InvalidDataException("Combat-only growth survived the next native combat: "+key);
        return new {key,reset=true};
    }
}
