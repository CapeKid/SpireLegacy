using Godot;
using System.Text.Json;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Injected native event/health controls; never count these as campaigns.
public static class EventPilotOracle
{
    private sealed class BorrowedSelector:IDisposable{public void Dispose(){}}
    public static async Task<object> Probe(RunState run,string kind)
    {
        if(kind is not ("healthy" or "injured" or "upgraded" or "repeat" or "late"))throw new ArgumentException("Unknown event fixture.");
        var player=run.Players.Single();var tree=(SceneTree)Engine.GetMainLoop();
        player.Creature.SetMaxHpInternal(80);player.Creature.SetCurrentHpInternal(kind=="injured"?44:80);
        if(kind=="upgraded")foreach(var card in player.Deck.Cards.Where(c=>c.IsUpgradable))CardCmd.Upgrade(card);
        await RunManager.Instance.EnterRoomDebug(RoomType.Event,model:ModelDb.Event<TabletOfTruth>(),showTransition:false);
        var model=((EventRoom)run.CurrentRoom!).LocalMutableEvent;
        async Task<NEventOptionButton[]> Buttons(){
            var deadline=Time.GetTicksMsec()+10000;
            while(true){
                var buttons=NativeTestBridge.Descendants(tree.Root).OfType<NEventOptionButton>().Where(b=>b.IsVisibleInTree()&&b.IsEnabled&&!b.Option.IsLocked).ToArray();
                if(buttons.Length>0)return buttons;
                if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Event fixture options.");
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            }
        }
        // Force earlier payments only in these synthetic boundary controls.
        var forced=kind=="late"?3:kind=="repeat"?1:0;
        for(var i=0;i<forced;i++){
            var buttons=await Buttons();var option=buttons.First(b=>b.Option.TextKey.Contains(".options.DECIPHER",StringComparison.Ordinal));
            await UiHelper.Click(option);
        }
        var ready=await Buttons();
        var hpBefore=player.Creature.CurrentHp;var maxHpBefore=player.Creature.MaxHp;
        var cost=model.DynamicVars["DecipherMaxHpLoss"].BaseValue;
        var decision=PilotEvents.Choose(model,ready.Select(b=>b.Option).ToArray(),player,true)??throw new InvalidDataException("No event choice.");
        var expected=kind=="healthy"?".DECIPHER_1":forced>0?".GIVE_UP":".SMASH";
        if(!decision.Option.TextKey.EndsWith(expected,StringComparison.Ordinal))throw new InvalidDataException("Wrong native event choice.");
        var fatal=new EventOption(model,()=>Task.CompletedTask,"TABLET_OF_TRUTH.pages.INITIAL.options.DECIPHER_1").ThatWillKillPlayerIf(_=>true);
        var locked=new EventOption(model,null,"TABLET_OF_TRUTH.pages.INITIAL.options.DECIPHER_1");
        var safe=new EventOption(model,()=>Task.CompletedTask,"TABLET_OF_TRUTH.pages.INITIAL.options.SMASH");
        if(PilotEvents.Choose(null,[locked,fatal,safe],player,true)?.Option!=safe||PilotEvents.Choose(null,[locked,fatal],player,true)!=null)throw new InvalidDataException("Unsafe generic event fallback.");
        var choice=JsonSerializer.SerializeToElement(await CampaignOracle.Step(run,"planned",new BorrowedSelector(),"defensive"));
        if(choice.GetProperty("action").GetString()!="event"||choice.GetProperty("detail").GetProperty("key").GetString()!=decision.Option.TextKey)throw new InvalidDataException("Campaign event path diverged.");
        // Click completion can precede command animation/callback completion.
        var deadlineEnd=Time.GetTicksMsec()+10000;
        while(kind=="healthy"?model.DynamicVars["DecipherMaxHpLoss"].BaseValue!=6:!model.IsFinished){
            if(Time.GetTicksMsec()>deadlineEnd)throw new TimeoutException("Event fixture completion.");
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        var hp=player.Creature.CurrentHp;var maxHp=player.Creature.MaxHp;
        if(kind=="healthy"?maxHp!=77:kind=="injured"?hp!=64||maxHp!=80:hp!=hpBefore||maxHp!=maxHpBefore)throw new InvalidDataException("Wrong native event health effect.");
        return new{kind,forced,cost,hpBefore,maxHpBefore,hp,maxHp,choice,finished=model.IsFinished,genericSafeControl=true,allFatalRejected=true};
    }
}
