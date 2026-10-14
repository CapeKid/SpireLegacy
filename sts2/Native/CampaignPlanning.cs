using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace SpireLegacy;

// Opt-in measurement policy, not an expert/human win-rate model.
public static class CampaignPlanning
{
    public static double Score(CardModel card,IReadOnlyList<PilotRewards.Card> deck)=>PilotRewards.Score(PilotCardInfo.Describe(card),deck,true);
    public static double UpgradeValue(CardModel card,IReadOnlyList<PilotRewards.Card> deck)
    {
        if(!card.IsUpgradable)return double.NegativeInfinity;
        var clone=(CardModel)card.MutableClone();clone.UpgradeInternal();clone.FinalizeUpgradeInternal();
        return Score(clone,deck)-Score(card,deck);
    }
    public sealed class Selector(bool upgrade):ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options,int minSelect,int maxSelect)
        {
            var cards=options.ToArray();var deck=cards.FirstOrDefault()?.Owner?.Deck.Cards.Select(PilotCardInfo.Describe).ToArray()??[];
            var sorted=upgrade?cards.OrderByDescending(c=>UpgradeValue(c,deck)):cards.OrderBy(c=>c.Type==MegaCrit.Sts2.Core.Entities.Cards.CardType.Curse?-100:Score(c,deck));
            return Task.FromResult(sorted.ThenBy(c=>c.Id.ToString(),StringComparer.Ordinal).Take(Math.Max(minSelect,Math.Min(1,maxSelect))).AsEnumerable());
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,IReadOnlyList<CardRewardAlternative> alternatives)=>new(){card=options.FirstOrDefault()?.Card};
    }
    public static IDisposable Select(bool upgrade)
    {
        var method=AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardSelectCmd),"UseSelector",[typeof(ICardSelector),typeof(bool)])
            ??AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardSelectCmd),"UseSelector",[typeof(ICardSelector)]);
        return (IDisposable)method.Invoke(null,method.GetParameters().Length==2?[new Selector(upgrade),false]:[new Selector(upgrade)])!;
    }
    public static async Task<object> Shop(RunState run)
    {
        var player=run.Players.Single();var room=(MegaCrit.Sts2.Core.Rooms.MerchantRoom)run.CurrentRoom!;
        var inventory=room.GetLocalInventory();var deck=player.Deck.Cards.Select(PilotCardInfo.Describe).ToArray();
        double Value(MerchantEntry e)=>e switch {
            MerchantCardEntry c when c.CreationResult!=null=>Score(c.CreationResult.Card,deck)>=10?Score(c.CreationResult.Card,deck)*8:0,
            MerchantCardRemovalEntry when deck.Length>10=>40,
            MerchantPotionEntry p when player.HasOpenPotionSlots&&p.Model?.GetType().Name is "BlockPotion" or "FirePotion" or "EnergyPotion"=>35,
            MerchantRelicEntry r when r.Model?.GetType().Name is "Vajra" or "Anchor" or "BagOfPreparation" or "OddlySmoothStone"=>90,
            _=>0
        };
        string Id(MerchantEntry e)=>e switch {MerchantCardEntry c=>c.CreationResult?.Card.Id.ToString()??"sold",MerchantRelicEntry r=>r.Model?.Id.ToString()??"sold",MerchantPotionEntry p=>p.Model?.Id.ToString()??"sold",_=>"card-removal"};
        var entries=inventory.AllEntries.Where(e=>e.IsStocked).ToArray();
        var offers=entries.Select(e=>new{id=Id(e),cost=e.Cost,affordable=e.EnoughGold,score=Value(e),kind=e.GetType().Name}).ToArray();
        var chosen=entries.Where(e=>e.EnoughGold&&Value(e)>0).OrderByDescending(e=>Value(e)/Math.Max(1,e.Cost)).ThenBy(Id,StringComparer.Ordinal).FirstOrDefault();
        if(chosen==null)return new{action="shop-done",offers};
        var gold=player.Gold;var id=Id(chosen);var cost=chosen.Cost;var before=player.Deck.Cards.Select(c=>c.Id.ToString()).ToArray();
        using(Select(false))if(!await chosen.OnTryPurchaseWrapper(inventory))throw new InvalidDataException("Native planned shop purchase failed.");
        // Native acquisition hooks (for example Lucky Fysh) can grant gold
        // during a purchase. Net balance change is not the displayed price.
        return new{action="shop-purchase",offers,picked=id,cost,goldBefore=gold,goldAfter=player.Gold,netGoldSpent=gold-player.Gold,goldAdjustment=player.Gold-(gold-cost),relics=player.Relics.Select(r=>r.Id.ToString()).ToArray(),deckBefore=before,deckAfter=player.Deck.Cards.Select(c=>c.Id.ToString()).ToArray()};
    }
}
