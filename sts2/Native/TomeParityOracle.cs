using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Opt-in diagnostic only; no production reward or starting-deck patch.
public static class TomeParityOracle
{
    private static void Require(bool value,string text) {if(!value)throw new InvalidDataException(text);}
    public static async Task<object> Check(RunState run,bool removeBasics,string? forced,bool inspect)
    {
        var player=run.Players.Single();
        var allowed=player.Character.CardPool.GetUnlockedCards(player.UnlockState,run.CardMultiplayerConstraint)
            .Where(c=>c.Rarity==CardRarity.Ancient&&!ArchaicTooth.TranscendenceCards.Contains(c)).Select(c=>c.Id).ToHashSet();
        Require(allowed.Count>0,"No native Dusty Tome choices.");
        if(inspect) {
            var saved=player.Relics.OfType<DustyTome>().Single();
            Require(player.Deck.Cards.Count(c=>c.Id==saved.AncientCard&&c.IsUpgraded)==1,"Saved Tome reward lost upgraded state.");
            return new {acquired=saved.AncientCard!.ToString(),upgraded=true,saved=true};
        }
        var starting=player.Deck.Cards.Select(c=>c.Id.ToString()).Distinct().ToArray();
        void Offers()
        {
            for(var i=0;i<30;i++) {
                var sample=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();sample.SetupForPlayer(player);
                Require(allowed.Contains(sample.AncientCard!),"Tome selected outside the owner's Ancient pool.");
                Require(sample.HoverTips.OfType<CardHoverTip>().Single(t=>t.Card.Id==sample.AncientCard).Card.IsUpgraded,"Native Tome preview is not upgraded.");
            }
        }
        Offers();
        if(removeBasics)await CardPileCmd.RemoveFromDeck(player.Deck.Cards.Where(c=>c.Rarity==CardRarity.Basic).ToArray(),showPreview:false);
        Offers();
        var tome=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();tome.SetupForPlayer(player);
        if(forced!=null) {var card=NativeCards.Get(forced);Require(allowed.Contains(card.Id),"Forced fixture card is not a legitimate Tome option.");tome.AncientCard=card.Id;}
        Require(tome.HoverTips.OfType<CardHoverTip>().Single(t=>t.Card.Id==tome.AncientCard).Card.IsUpgraded,"Final Tome preview differs.");
        var restored=(DustyTome)RelicModel.FromSerializable(tome.ToSerializable());
        Require(restored.AncientCard==tome.AncientCard&&restored.HoverTips.OfType<CardHoverTip>().Single(t=>t.Card.Id==tome.AncientCard).Card.IsUpgraded,"Serialized Tome preview differs.");
        var before=player.Deck.Cards.Count;await RelicCmd.Obtain(tome,player);
        Require(player.Deck.Cards.Count==before+1&&player.Deck.Cards.Count(c=>c.Id==tome.AncientCard&&c.IsUpgraded)==1,"Tome did not grant exactly one upgraded card.");
        return new {startingIds=starting,removeBasics,offerChecks=60,allowed=allowed.Select(id=>id.ToString()).ToArray(),acquired=tome.AncientCard!.ToString(),upgraded=true,forcedFixture=forced!=null};
    }
}
