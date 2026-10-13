using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Bounded effect isolation, reachable only through the disposable test bridge.
public static class BalanceAuditOracle
{
    private static readonly PileType[] Piles=[PileType.Hand,PileType.Draw,PileType.Discard,PileType.Exhaust,PileType.Play];
    public static async Task<object> ResetLoop(RunState run,bool upgraded,bool native,IDisposable selector)
    {
        using(selector) {
            var player=PlayerBloodlines.LocalPlayer!;
            var combat=player.Creature.CombatState!;
            await CardPileCmd.RemoveFromCombat(CardPile.GetCards(player,Piles).ToArray(),skipVisuals:true);
            foreach(var enemy in combat.GetOpponentsOf(player.Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
            await CardEffects.Apply(new BlockingPlayerChoiceContext(),"discard_block",player.Creature,2,player.Creature,null);
            var copies=new List<CardModel>();
            foreach(var _ in Enumerable.Range(0,2)){
                var card=combat.CreateCard(native?ModelDb.Card<CalculatedGamble>():NativeCards.Get("ranger_trail_reset"),player);
                if(upgraded)CardCmd.Upgrade(card);
                await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);copies.Add(card);
            }
            // Native Reflex is a legal cross-color enabler. Its discard draw can
            // replenish the exchange; no injected Energy or forged reward hooks.
            await CardPileCmd.Add(combat.CreateCard(ModelDb.Card<Reflex>(),player),PileType.Hand,skipVisuals:true);
            var before=player.PlayerCombatState!.Energy;var block=player.Creature.Block;
            var peers=run.Players.Where(p=>p!=player).Select(p=>new{p.NetId,energy=p.PlayerCombatState!.Energy,block=p.Creature.Block,hand=PileType.Hand.GetPile(p).Cards.Count}).ToArray();
            var samples=new List<object>();
            for(var i=0;i<12;i++){
                var next=PileType.Hand.GetPile(player).Cards.FirstOrDefault(c=>copies.Contains(c)&&c.CanPlay());
                if(next==null)break;
                await BalanceOracle.Play(next);
                samples.Add(new{energy=player.PlayerCombatState.Energy,block=player.Creature.Block,hand=PileType.Hand.GetPile(player).Cards.Count,exhaust=copies.Count(c=>c.Pile?.Type==PileType.Exhaust)});
            }
            var isolated=peers.All(p=>{var peer=run.Players.Single(x=>x.NetId==p.NetId);return peer.PlayerCombatState!.Energy==p.energy&&peer.Creature.Block==p.block&&PileType.Hand.GetPile(peer).Cards.Count==p.hand;});
            var saved=copies.Select(c=>{var loaded=run.LoadCard(c.ToSerializable(),player);var result=loaded.Keywords.Contains(CardKeyword.Exhaust)==c.Keywords.Contains(CardKeyword.Exhaust);run.RemoveCard(loaded);return result;}).All(x=>x);
            return new{owner=player.NetId,upgraded,native,plays=samples.Count,energySpent=before-player.PlayerCombatState.Energy,blockGained=player.Creature.Block-block,exhausted=copies.Count(c=>c.Pile?.Type==PileType.Exhaust),roundTrip=saved,ownerIsolated=isolated,samples};
        }
    }
    public static object References()
    {
        var cards=new CardModel[]{ModelDb.Card<StrikeIronclad>(),ModelDb.Card<DefendIronclad>(),ModelDb.Card<Prepared>(),ModelDb.Card<Acrobatics>(),ModelDb.Card<Adrenaline>(),ModelDb.Card<CalculatedGamble>(),ModelDb.Card<Backflip>(),ModelDb.Card<FlashOfSteel>(),ModelDb.Card<Bloodletting>(),ModelDb.Card<Turbo>(),ModelDb.Card<WellLaidPlans>()};
        return cards.SelectMany(c=>new[]{false,true}.Select(up=>{var card=c.ToMutable();if(up)CardCmd.Upgrade(card);return new{id=card.Id.ToString(),upgraded=up,cost=card.EnergyCost.GetWithModifiers(CostModifiers.All),rarity=card.Rarity.ToString(),keywords=card.Keywords.Select(k=>k.ToString()).ToArray(),vars=card.DynamicVars.Select(v=>new{name=v.Key,value=v.Value.BaseValue}).ToArray()};})).ToArray();
    }
}
