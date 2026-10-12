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
            if(kind is "potion-danger" or "potion-healthy" or "potion-covered" or "potion-disabled" or "potion-campaign"){
                var potion=ModelDb.Potion<MegaCrit.Sts2.Core.Models.Potions.BlockPotion>().ToMutable();
                if(!(await PotionCmd.TryToProcure(potion,player)).success)throw new InvalidDataException("Fixture potion procurement failed");
                for(var frame=0;frame<4;frame++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                player.Creature.SetCurrentHpInternal(kind=="potion-healthy"?80:1);
                if(kind=="potion-covered")await CreatureCmd.GainBlock(player.Creature,12,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
                var energy=player.PlayerCombatState!.Energy;var hp=player.Creature.CurrentHp;
                if(kind=="potion-disabled"){
                    var permission=HarmonyLib.AccessTools.Property(typeof(Player),"CanUseOrRemovePotions")??HarmonyLib.AccessTools.Property(typeof(Player),"CanRemovePotions");
                    permission!.SetValue(player,false);
                    try{if(await PilotPotions.EmergencyBlock(player,12)!=null)throw new InvalidDataException("Pilot bypassed native potion permission");}
                    finally{permission.SetValue(player,true);}
                    return new{kind,used=false,consumed=!player.Potions.Contains(potion),block=player.Creature.Block,energySpent=energy-player.PlayerCombatState.Energy,hpLost=hp-player.Creature.CurrentHp};
                }
                if(kind=="potion-campaign"){
                    var incoming=TacticalPilot.Incoming(player);
                    if(incoming<=0||incoming>12)throw new InvalidDataException("Fixture incoming is outside Block Potion rescue range");
                    var result=System.Text.Json.JsonSerializer.SerializeToElement(await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"deck"));
                    if(result.GetProperty("action").GetString()!="combat-turn"||result.GetProperty("detail").GetProperty("potions").GetArrayLength()!=1)throw new InvalidDataException("Campaign did not journal emergency potion");
                    return new{kind,consumed=!player.Potions.Contains(potion),alive=player.Creature.IsAlive,hp=player.Creature.CurrentHp,incoming,journaled=true};
                }
                var used=await PilotPotions.EmergencyBlock(player,12);
                return new{kind,used=used!=null,consumed=!player.Potions.Contains(potion),block=player.Creature.Block,energySpent=energy-player.PlayerCombatState.Energy,hpLost=hp-player.Creature.CurrentHp};
            }
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
            if(kind is "avoid-offering" or "avoid-bloodletting" or "avoid-hemokinesis" or "safe-offering" or "only-fatal"){
                var safe=kind=="safe-offering";
                var loss=kind=="avoid-bloodletting"?3:kind=="avoid-hemokinesis"?2:6;
                player.Creature.SetCurrentHpInternal(safe?80:loss);
                // A large Block stack must not make an unblockable cost seem affordable.
                await CreatureCmd.GainBlock(player.Creature,50,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
                var paid=kind=="avoid-bloodletting"?await Add<Bloodletting>():kind=="avoid-hemokinesis"?await Add<Hemokinesis>():await Add<Offering>();
                CardModel? strike=kind=="only-fatal"?null:await Add<StrikeIronclad>();
                var decision=TacticalPilot.Choose(player,0);
                if(kind=="only-fatal"){
                    if(decision!=null)throw new InvalidDataException("Pilot selected its only fatal card");
                    return new{kind,declined=true,hp=player.Creature.CurrentHp};
                }
                if(decision==null||decision.Card!=(safe?paid:strike))throw new InvalidDataException("HP-cost pilot choice incorrect");
                var hp=player.Creature.CurrentHp;await BalanceOracle.Play(decision.Card,decision.Target);
                return new{kind,alive=player.Creature.IsAlive,hpLost=hp-player.Creature.CurrentHp,paidSelected=decision.Card==paid};
            }
            if(kind=="lethal-cost"){
                player.Creature.SetCurrentHpInternal(5);var offering=await Add<Offering>();
                await BalanceOracle.Play(offering);
                return new{kind,dead=!player.Creature.IsAlive,gameOver=run.IsGameOver};
            }
            if(kind is "skip" or "force-present" or "force-absent"){
                var rewardPolicy=kind=="skip"?"deck":"defensive";
                var count=player.Deck.Cards.Count;
                var options=Enumerable.Range(0,3).Select(_=>run.CreateCard(ModelDb.Card<StrikeIronclad>(),player)).ToArray();
                var reward=new CardReward(options,CardCreationSource.Encounter,player,CardCreationOptions.ForRoom(player,run.CurrentRoom!.RoomType));
                var set=new RewardsSet(player);set.Rewards.Add(reward);var offering=set.Offer();
                for(var frame=0;frame<20&&NOverlayStack.Instance?.Peek() is not NRewardsScreen;frame++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"deck");
                var choice=kind=="force-present"?"CARD.STRIKE_IRONCLAD":kind=="force-absent"?"CARD.DOES_NOT_EXIST":null;
                var skipped=System.Text.Json.JsonSerializer.SerializeToElement(await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),rewardPolicy,choice,run.TotalFloor));
                var took=kind=="force-present";
                if(skipped.GetProperty("detail").GetProperty("skip").GetBoolean()==took)throw new InvalidDataException("Reward intervention/skip result incorrect");
                if(took&&!skipped.GetProperty("detail").GetProperty("intervention").GetBoolean())throw new InvalidDataException("Legal intervention not journaled");
                await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"deck");
                await offering.WaitAsync(TimeSpan.FromSeconds(10));
                var replaySkipped=false;
                if(took){
                    var more=Enumerable.Range(0,3).Select(_=>run.CreateCard(ModelDb.Card<StrikeIronclad>(),player)).ToArray();
                    var replay=new CardReward(more,CardCreationSource.Encounter,player,CardCreationOptions.ForRoom(player,run.CurrentRoom!.RoomType));
                    var secondSet=new RewardsSet(player);secondSet.Rewards.Add(replay);var secondOffer=secondSet.Offer();
                    for(var frame=0;frame<20&&NOverlayStack.Instance?.Peek() is not NRewardsScreen;frame++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                    await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),rewardPolicy,choice,run.TotalFloor);
                    var second=System.Text.Json.JsonSerializer.SerializeToElement(await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),rewardPolicy,choice,run.TotalFloor));
                    replaySkipped=second.GetProperty("detail").GetProperty("skip").GetBoolean();
                    if(!replaySkipped)throw new InvalidDataException("One-off intervention replayed on another reward");
                    await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"deck");await secondOffer.WaitAsync(TimeSpan.FromSeconds(10));
                }
                return new{kind,rewardPolicy,unchangedDeck=player.Deck.Cards.Count==count,added=player.Deck.Cards.Count-count,successfullySelected=reward.SuccessfullySelected,replaySkipped,closed=NOverlayStack.Instance?.Peek() is not NRewardsScreen};
            }
            throw new ArgumentException("Unknown control fixture.");
        }
    }
}
