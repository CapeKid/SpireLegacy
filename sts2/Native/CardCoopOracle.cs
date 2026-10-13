using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Prepare identical native scopes on each peer; only the real owner submits play.
public static class CardCoopOracle
{
    private static readonly PileType[] Piles=[PileType.Draw,PileType.Hand,PileType.Discard,PileType.Exhaust,PileType.Play];
    public static async Task<object> Prepare(RunState run,string key,bool upgraded,bool replay)
    {
        foreach(var player in run.Players.OrderBy(p=>p.NetId)) {
            var card=player.Creature.CombatState!.CreateCard(NativeCards.Get(key),player);
            if(upgraded)CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            await PlayerCmd.GainEnergy(20,player);
            if(replay)await PowerCmd.Apply<BurstPower>(new BlockingPlayerChoiceContext(),player.Creature,1,player.Creature,null);
        }
        return new {key,upgraded,replay,players=run.Players.Count};
    }
    public static async Task<object> Play(RunState run,string key)
    {
        var player=PlayerBloodlines.LocalPlayer!;
        var card=player.PlayerCombatState!.Hand.Cards.OfType<LegacyCard>().Single(c=>c.Key==key);
        var other=run.Players.Where(p=>p!=player).Select(p=>new {player=p,energy=p.PlayerCombatState!.Energy,block=p.Creature.Block,hand=p.PlayerCombatState.Hand.Cards.Count}).ToArray();
        var count=CardPile.GetCards(player,Piles).Count();var energy=player.PlayerCombatState.Energy;var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);var failure=CardPlayDiagnostics.LastFailure;
        if(!card.TryManualPlay(null))throw new InvalidDataException("Shared co-op paid play refused.");
        var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+20000;
        while(card.Pile?.Type is PileType.Hand or PileType.Play||NCard.FindOnTable(card)!=null||RunManager.Instance.ActionExecutor.IsRunning) {
            if(Time.GetTicksMsec()>until)throw new TimeoutException("Shared co-op card unresolved.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        if(card.Pile?.Type!=PileType.Discard||player.PlayerCombatState.Energy!=energy-cost||CardPile.GetCards(player,Piles).Count()!=count||CardPlayDiagnostics.LastFailure!=failure)throw new InvalidDataException("Shared co-op paid play lost card/resources.");
        if(other.Any(o=>o.player.PlayerCombatState!.Energy!=o.energy||o.player.Creature.Block!=o.block||o.player.PlayerCombatState.Hand.Cards.Count!=o.hand))throw new InvalidDataException("Shared card affected another owner's resources.");
        return new {key,owner=player.NetId,cost,pile="Discard",ownerOnly=true,conserved=true,tableNodePresent=false};
    }
}
