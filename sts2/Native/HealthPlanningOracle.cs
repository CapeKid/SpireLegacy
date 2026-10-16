using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Synthetic planning calibration; no collected result is a natural campaign.
public static class HealthPlanningOracle
{
    private sealed class BorrowedSelector:IDisposable{public void Dispose(){}}
    public static async Task<object> Probe(RunState run,string kind)
    {
        if(kind is not ("models" or "reward-low" or "reward-high"))throw new ArgumentException("Unknown health planning calibration.");
        var player=run.Players.Single();var native=player.Character is MegaCrit.Sts2.Core.Models.Characters.Ironclad;
        var cls=native?"ironclad":PlayerBloodlines.Heir(player).classId;
        await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+30000;
        while(player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play){
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Planning fixture play phase.");
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        if(kind!="models"){
            if(cls!="knight")throw new InvalidDataException("Reward calibration requires Knight.");
            player.Creature.SetCurrentHpInternal(kind=="reward-low"?3:player.Creature.MaxHp);
            var options=new[]{"knight_bloodied_blade","knight_measured_cut"}.Select(key=>run.CreateCard(NativeCards.Get(key),player)).ToArray();
            var reward=new CardReward(options,CardCreationSource.Encounter,player,CardCreationOptions.ForRoom(player,run.CurrentRoom!.RoomType));
            var set=new RewardsSet(player);set.Rewards.Add(reward);var offering=set.Offer();
            for(var frame=0;frame<20&&NOverlayStack.Instance?.Peek() is not NRewardsScreen;frame++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"defensive");
            var choice=JsonSerializer.SerializeToElement(await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"defensive"));
            await CampaignOracle.Step(run,"tactical",new BorrowedSelector(),"defensive");
            await offering.WaitAsync(TimeSpan.FromSeconds(10));
            return new{cls,kind,hp=player.Creature.CurrentHp,choice,successfullySelected=reward.SuccessfullySelected,
                acquired=player.Deck.Cards.OfType<LegacyCard>().Where(c=>c.Key is "knight_bloodied_blade" or "knight_measured_cut").Select(c=>c.Key).ToArray()};
        }
        var deck=player.Deck.Cards.Select(PilotCardInfo.Describe).ToArray();
        var canonical=native?new CardModel[]{ModelDb.Card<Offering>(),ModelDb.Card<Bloodletting>(),ModelDb.Card<Hemokinesis>(),ModelDb.Card<Haunt>()}
            :Design.Rows("cards").Where(r=>r.Text("classId")==cls&&r.Number("hpLoss")>0).Select(r=>(CardModel)NativeCards.Get(r.Text("id"))).ToArray();
        var rows=new List<object>();
        foreach(var model in canonical)foreach(var up in new[]{false,true}){
            var card=run.CreateCard(model,player);if(up)CardCmd.Upgrade(card);
            var before=PilotCardInfo.Describe(card);var payment=PilotCardInfo.ImmediateHpLoss(card);
            if(before.HpLoss!=payment)throw new InvalidDataException("Reward descriptor omits HP payment.");
            var contexts=new List<object>();
            foreach(var hp in new[]{1,Math.Max(1,payment),payment+1,player.Creature.MaxHp}.Distinct()){
                player.Creature.SetCurrentHpInternal(hp);
                var score=CampaignPlanning.Score(card,deck);var expected=PilotRewards.Score(before,deck,true,hp);
                if(score!=expected)throw new InvalidDataException("Planning lost current HP context.");
                var gain=card.IsUpgradable?(double?)CampaignPlanning.UpgradeValue(card,deck):null;
                if(payment>0&&hp<=payment&&(score>=5||gain is {} value&&value!=0))throw new InvalidDataException("Fatal card planning bypass.");
                if(PilotCardInfo.Describe(card)!=before||card.IsUpgraded!=up)throw new InvalidDataException("Upgrade evaluation mutated source card.");
                contexts.Add(new{hp,score,upgradeValue=gain});
            }
            rows.Add(new{id=before.Id,upgraded=up,hpLoss=payment,contexts});
        }
        object? upgrade=null;
        if(cls=="knight"){
            var paid=run.CreateCard(NativeCards.Get("knight_blood_price"),player);var guard=run.CreateCard(NativeCards.Get("knight_guard"),player);
            var selector=new CampaignPlanning.Selector(true);
            player.Creature.SetCurrentHpInternal(3);var low=(await selector.GetSelectedCards([paid,guard],1,1)).Single();
            var lowPaid=CampaignPlanning.UpgradeValue(paid,deck);var lowGuard=CampaignPlanning.UpgradeValue(guard,deck);
            player.Creature.SetCurrentHpInternal(player.Creature.MaxHp);var high=(await selector.GetSelectedCards([paid,guard],1,1)).Single();
            var highPaid=CampaignPlanning.UpgradeValue(paid,deck);var highGuard=CampaignPlanning.UpgradeValue(guard,deck);
            CardCmd.Upgrade(low);
            upgrade=new{lowPicked=((LegacyCard)low).Key,highPicked=((LegacyCard)high).Key,lowPaid,lowGuard,highPaid,highGuard,
                guardUpgraded=guard.IsUpgraded,paidUnchanged=!paid.IsUpgraded};
        }
        return new{cls,kind,rows,upgrade};
    }
}
