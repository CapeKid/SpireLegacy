using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

public static class ControlPilotOracle
{
    private sealed class BorrowedSelector : IDisposable { public void Dispose(){} }
    public static async Task<object> Probe(RunState run,string kind)
    {
        using(TacticalPilot.Selector()){
            var player=run.Players.Single();
            if(player.Character is not MegaCrit.Sts2.Core.Models.Characters.Ironclad)throw new InvalidDataException("Control fixture requires actual Ironclad.");
            await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
            RunManager.Instance.ActionExecutor.Unpause();var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+30000;
            while(player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play){if(Time.GetTicksMsec()>until)throw new TimeoutException("Control fixture play phase");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
            var hand=PileType.Hand.GetPile(player);
            foreach(var card in hand.Cards.ToArray())await CardPileCmd.Add(card,PileType.Draw,skipVisuals:true);
            var enemies=player.Creature.CombatState!.HittableEnemies.ToArray();foreach(var enemy in enemies){enemy.SetMaxHpInternal(1000);enemy.SetCurrentHpInternal(1000);}
            async Task<CardModel> Add<T>() where T:CardModel {var card=player.Creature.CombatState.CreateCard(ModelDb.Card<T>(),player);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);return card;}
            if(kind=="bash"){
                var bash=await Add<Bash>();var strike=await Add<StrikeIronclad>();await Add<DefendIronclad>();
                var first=TacticalPilot.Choose(player,0)!;if(first.Card!=bash)throw new InvalidDataException("Native Bash setup not recognized");
                await BalanceOracle.Play(bash,first.Target);var hp=first.Target!.CurrentHp;
                var next=TacticalPilot.Choose(player,0)!;if(next.Card!=strike||next.Target!=first.Target)throw new InvalidDataException("Native marked follow-up not recognized");
                await BalanceOracle.Play(strike,next.Target);return new{kind,damage=hp-first.Target.CurrentHp,vulnerable=first.Target.GetPowerAmount<VulnerablePower>()};
            }
            if(kind=="shrug"){
                var shrug=await Add<ShrugItOff>();await Add<DefendIronclad>();var count=hand.Cards.Count;
                if(TacticalPilot.Choose(player,20)!.Card!=shrug)throw new InvalidDataException("Native Block/draw utility not recognized");
                await BalanceOracle.Play(shrug);return new{kind,block=player.Creature.Block,draw=PilotCardInfo.Amount(shrug,"draw"),handCount=hand.Cards.Count,previousCount=count};
            }
            if(kind=="repeat"){
                var twin=await Add<TwinStrike>();await Add<StrikeIronclad>();var decision=TacticalPilot.Choose(player,0)!;
                if(decision.Card!=twin)throw new InvalidDataException("Native repeat damage not recognized");
                var hp=decision.Target!.CurrentHp;await BalanceOracle.Play(twin,decision.Target);return new{kind,damage=hp-decision.Target.CurrentHp,hits=PilotCardInfo.Hits(twin)};
            }
            if(kind=="lethal-cost"){
                player.Creature.SetCurrentHpInternal(5);var offering=await Add<Offering>();
                await BalanceOracle.Play(offering);
                return new{kind,dead=!player.Creature.IsAlive,gameOver=run.IsGameOver};
            }
            if(kind=="skip"){
                var count=player.Deck.Cards.Count;
                var options=Enumerable.Range(0,3).Select(_=>run.CreateCard(ModelDb.Card<StrikeIronclad>(),player)).ToArray();
                var reward=new CardReward(options,CardCreationSource.Encounter,player,CardCreationOptions.ForRoom(player,run.CurrentRoom!.RoomType));
                var set=new RewardsSet(player);set.Rewards.Add(reward);var offering=set.Offer();
                for(var frame=0;frame<20&&NOverlayStack.Instance?.Peek() is not NRewardsScreen;frame++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"deck");
                var skipped=System.Text.Json.JsonSerializer.SerializeToElement(await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"deck"));
                if(!skipped.GetProperty("detail").GetProperty("skip").GetBoolean())throw new InvalidDataException("Weak duplicate reward did not skip");
                await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"deck");
                await offering.WaitAsync(TimeSpan.FromSeconds(10));
                return new{kind,unchangedDeck=player.Deck.Cards.Count==count,successfullySelected=reward.SuccessfullySelected,closed=NOverlayStack.Instance?.Peek() is not NRewardsScreen};
            }
            throw new ArgumentException("Unknown control fixture.");
        }
    }
}
