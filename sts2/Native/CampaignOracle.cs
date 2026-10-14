using Godot;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using HarmonyLib;
using System.Runtime.CompilerServices;

namespace SpireLegacy;

// One bounded, auditable decision per request. Never invokes AutoSlay's buff/kill handlers.
// Only the opt-in isolated test bridge calls this; ordinary gameplay is untouched.
public static class CampaignOracle
{
    private static SceneTree Tree=>(SceneTree)Engine.GetMainLoop();
    private static string Key(CardModel c)=>c is LegacyCard lc?lc.Key:c.Id.ToString();
    private static readonly ConditionalWeakTable<RunState,HashSet<Reward>> skippedRewards=new();
    private static readonly ConditionalWeakTable<RunState,HashSet<string>> appliedInterventions=new();
    public static async Task<object> Step(RunState run,string policy,IDisposable selector,string rewardPolicy="standalone",string? forcedReward=null,int forcedRewardFloor=0)
    {
        using(selector){
            if(policy is not ("defense" or "attack" or "tactical" or "planned"))throw new ArgumentException("Unknown campaign policy.");
            var planned=policy=="planned";var tactical=policy is "tactical" or "planned";
            if(rewardPolicy is not ("standalone" or "deck" or "defensive"))throw new ArgumentException("Unknown reward policy.");
            if(forcedReward!=null&&(rewardPolicy=="standalone"||forcedRewardFloor<=0))throw new ArgumentException("Reward intervention requires deck policy and a positive floor.");
            var player=run.Players.Single();
            object Snapshot()=>new{character=player.Character.Id.ToString(),act=run.CurrentActIndex+1,floor=run.TotalFloor,room=run.CurrentRoom?.RoomType.ToString(),hp=player.Creature.CurrentHp,maxHp=player.Creature.MaxHp,gold=player.Gold,crowns=Runtime.Profile.crowns,lastEarned=Runtime.Profile.lastEarned,wins=player.Character is HeirCharacter?Runtime.Profile.chronicle.classes.Values.Sum(h=>h.wins):run.IsGameOver&&run.CurrentRoom is MegaCrit.Sts2.Core.Rooms.EventRoom e&&e.LocalMutableEvent is MegaCrit.Sts2.Core.Models.Events.TheArchitect?1:0,manor=Runtime.Profile.manor,deck=player.Deck.Cards.Select(Key).ToArray(),relics=player.Relics.Select(r=>r.Id.ToString()).ToArray()};
            object Result(string action,object? detail=null)=>new{action,detail,state=Snapshot()};
            if(run.IsGameOver){await Task.Delay(500);return Result("finished");}
            if(NMapScreen.Instance?.IsOpen==true){
                var paths=run.CurrentMapPoint?.Children.ToArray()??[run.Map.StartingMapPoint];
                double Route(MegaCrit.Sts2.Core.Map.MapPoint p)=>p.PointType.ToString() switch {"RestSite"=>player.Creature.CurrentHp<player.Creature.MaxHp*.6?100:55,"Shop"=>player.Gold>=120?90:5,"Treasure"=>80,"Elite"=>player.Creature.CurrentHp>player.Creature.MaxHp*.8&&player.Deck.Cards.Count>15?60:0,"Monster"=>40,_=>30};
                var next=planned?paths.OrderByDescending(Route).ThenBy(p=>p.coord.col).FirstOrDefault():paths.FirstOrDefault();
                if(next==null)return Result("unsupported-map");
                await RunManager.Instance.EnterMapCoord(next.coord);
                return Result("map",new{row=next.coord.row,col=next.coord.col,type=next.PointType.ToString(),policy,offers=paths.Select(p=>new{p.coord.row,p.coord.col,type=p.PointType.ToString(),score=Route(p)}).ToArray()});
            }
            var overlay=NOverlayStack.Instance?.Peek();
            if(overlay is NCardRewardSelectionScreen cards){
                var holders=UiHelper.FindAll<NCardHolder>(cards).Where(h=>h.CardModel!=null).ToArray();
                if(holders.Length==0)return Result("wait");
                var deck=player.Deck.Cards.Select(PilotCardInfo.Describe).ToArray();
                double Score(CardModel c)=>rewardPolicy!="standalone"?PilotRewards.Score(PilotCardInfo.Describe(c),deck,rewardPolicy=="defensive"):BalanceOracle.Score(c,0);
                var offers=holders.Select(h=>new{id=Key(h.CardModel!),upgraded=h.CardModel!.IsUpgraded,score=Score(h.CardModel!)}).ToArray();
                var interventionKey=$"{forcedRewardFloor}:{forcedReward}";
                var forced=forcedReward!=null&&run.TotalFloor==forcedRewardFloor&&!appliedInterventions.GetOrCreateValue(run).Contains(interventionKey)?holders.FirstOrDefault(h=>Key(h.CardModel!)==forcedReward):null;
                var index=forced!=null?Array.IndexOf(holders,forced):rewardPolicy!="standalone"?PilotRewards.Pick(holders.Select(h=>PilotCardInfo.Describe(h.CardModel!)).ToArray(),deck,rewardPolicy=="defensive"):0;
                var parent=NativeTestBridge.Descendants(Tree.Root).OfType<NRewardButton>().Select(b=>b.Reward).OfType<CardReward>().FirstOrDefault(r=>r.Cards.Any(c=>c==holders[0].CardModel));
                if(rewardPolicy!="standalone"&&index==null&&parent?.CanSkip==true){
                    // Resolve the same nullable choice consumed by native cancellation. Let CardReward close its own screen.
                    var pending=(TaskCompletionSource<int?>)AccessTools.Field(cards.GetType(),"_completionSource").GetValue(cards)!;
                    if(!pending.TrySetResult(null))return Result("wait");
                    skippedRewards.GetOrCreateValue(run).Add(parent);await Task.Delay(300);
                    return Result("card-reward",new{offers,picked=(string?)null,skip=true,rewardPolicy});
                }
                var choice=forced??holders.OrderByDescending(h=>Score(h.CardModel!)).ThenBy(h=>Key(h.CardModel!),StringComparer.Ordinal).First();
                var picked=Key(choice.CardModel!);
                choice.EmitSignal(NCardHolder.SignalName.Pressed,choice);
                if(forced!=null)appliedInterventions.GetOrCreateValue(run).Add(interventionKey);
                await Task.Delay(300);
                return Result("card-reward",new{offers,picked,skip=false,rewardPolicy,intervention=forced!=null});
            }
            if(overlay is NRewardsScreen rewards){
                var reward=UiHelper.FindAll<NRewardButton>(rewards).FirstOrDefault(b=>b.IsEnabled&&b.IsVisibleInTree()&&b.Reward!=null&&!skippedRewards.GetOrCreateValue(run).Contains(b.Reward)&&(b.Reward is not PotionReward||player.HasOpenPotionSlots));
                if(reward!=null){var kind=reward.Reward?.GetType().Name;await UiHelper.Click(reward,500);return Result("collect-rewards",new{kind});}
                var leave=UiHelper.FindAll<NProceedButton>(rewards).FirstOrDefault(b=>b.IsEnabled&&b.IsVisibleInTree());
                if(leave!=null){await UiHelper.Click(leave,500);return Result("reward-proceed");}
                return Result("wait");
            }
            if(overlay!=null)return Result("unsupported-overlay",new{type=overlay.GetType().Name});
            if(CombatManager.Instance.IsInProgress){
                if(player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play)return Result("wait");
                var combat=player.Creature.CombatState!;var turn=player.PlayerCombatState.TurnNumber;
                if(turn>40)return Result("turn-cap",new{turn});
                var entries=new Dictionary<string,int>();var present=new HashSet<CardModel>();var plays=new List<object>();
                // Observe every frame and after resolution, including cards drawn mid-turn.
                // These are hand-entry counts, not Draw hook counts (retrieval also enters hand).
                void Observe(){
                    if(player.PlayerCombatState==null)return;
                    var hand=PileType.Hand.GetPile(player).Cards.ToHashSet();
                    foreach(var c in hand.Except(present)){var key=Key(c);entries[key]=entries.GetValueOrDefault(key)+1;}
                    present=hand;
                }
                var oldScale=Engine.TimeScale;Engine.TimeScale=6;Tree.ProcessFrame+=Observe;var observing=true;
                try{
                    Observe();
                    for(var i=0;i<50;i++){
                        if(!player.Creature.IsAlive||CombatManager.Instance.IsEnding||!combat.GetOpponentsOf(player.Creature).Any(e=>e.IsAlive))break;
                        var incoming=TacticalPilot.Incoming(player);
                        var hand=PileType.Hand.GetPile(player);
                        var decision=tactical?TacticalPilot.Choose(player,incoming):null;
                        var next=tactical?decision?.Card:hand.Cards.Where(c=>c.CanPlay()).OrderByDescending(c=>policy=="attack"&&c.Type==CardType.Attack?200+BalanceOracle.Score(c,0):BalanceOracle.Score(c,incoming-player.Creature.Block)).ThenBy(Key,StringComparer.Ordinal).FirstOrDefault();
                        if(next==null)break;
                        if(i==49)return Result("play-cap",new{turn,entries,plays});
                        var energy=player.PlayerCombatState.Energy;var hp=player.Creature.CurrentHp;
                        await BalanceOracle.Play(next,decision?.Target);Observe();
                        plays.Add(new{id=Key(next),upgraded=next.IsUpgraded,netEnergy=player.PlayerCombatState is {} remaining?(int?)(energy-remaining.Energy):null,netHpLost=hp-player.Creature.CurrentHp,reason=decision?.Reason,target=decision?.Target?.Monster?.Id.ToString()});
                    }
                    var potions=new List<PilotPotions.Used>();
                    if(tactical){
                        var limit=player.Potions.Count();
                        for(var p=0;p<limit&&player.Creature.IsAlive&&!CombatManager.Instance.IsEnding&&combat.GetOpponentsOf(player.Creature).Any(e=>e.IsAlive);p++){
                            var used=await PilotPotions.EmergencyBlock(player,TacticalPilot.Incoming(player));
                            if(used==null)break;potions.Add(used);Observe();
                        }
                    }
                    var unplayed=player.PlayerCombatState==null?[]:PileType.Hand.GetPile(player).Cards.Select(Key).ToArray();
                    // Next turn's draw belongs to the next decision, not this turn's sightings.
                    Tree.ProcessFrame-=Observe;
                    observing=false;
                    var victory=!combat.GetOpponentsOf(player.Creature).Any(e=>e.IsAlive);
                    if(!victory&&player.Creature.IsAlive&&!CombatManager.Instance.IsEnding)await BalanceOracle.End(player,false);
                    return Result("combat-turn",new{turn,entries,plays,potions,unplayed,victory=!combat.GetOpponentsOf(player.Creature).Any(e=>e.IsAlive)});
                }finally{if(observing)Tree.ProcessFrame-=Observe;Engine.TimeScale=oldScale;}
            }
            var nodes=NativeTestBridge.Descendants(Tree.Root).ToArray();
            if(planned&&run.CurrentRoom is MegaCrit.Sts2.Core.Rooms.MerchantRoom){
                var shopNodes=nodes.OfType<NMerchantRoom>().FirstOrDefault();
                if(shopNodes!=null){
                    if(!shopNodes.Inventory.IsOpen){shopNodes.OpenInventory();return Result("shop-open");}
                    var purchase=await CampaignPlanning.Shop(run);
                    var action=(string)purchase.GetType().GetProperty("action")!.GetValue(purchase)!;
                    if(action=="shop-purchase")return Result(action,purchase);
                    shopNodes.Inventory.Close();
                    var exit=nodes.OfType<NProceedButton>().FirstOrDefault(b=>b.IsVisibleInTree()&&b.IsEnabled);
                    if(exit!=null)await UiHelper.Click(exit);
                    return Result(action,purchase);
                }
            }
            var options=nodes.OfType<NEventOptionButton>().Where(b=>b.IsVisibleInTree()&&b.IsEnabled&&!b.Option.IsLocked).ToArray();
            if(options.Length>0){
                var choice=options.FirstOrDefault(b=>b.Option.WillKillPlayer==null||!b.Option.WillKillPlayer(player))??options[0];
                var title=choice.Option.Title.GetFormattedText();await UiHelper.Click(choice);
                return Result("event",new{choice=title});
            }
            var dialogue=nodes.OfType<NAncientEventLayout>().Select(n=>n.GetNodeOrNull<NButton>("%DialogueHitbox")).FirstOrDefault(b=>b!=null&&b.IsVisibleInTree()&&b.IsEnabled);
            if(dialogue!=null){await UiHelper.Click(dialogue);return Result("dialogue");}
            var rest=nodes.OfType<NRestSiteButton>().Where(b=>b.IsVisibleInTree()&&b.IsEnabled&&b.Option.IsEnabled).ToArray();
            if(rest.Length>0){
                var heal=rest.FirstOrDefault(b=>b.Option.GetType().Name=="HealRestSiteOption");
                var smith=rest.FirstOrDefault(b=>b.Option.GetType().Name=="SmithRestSiteOption");
                var needsHeal=player.Creature.CurrentHp<player.Creature.MaxHp*.55&&PlayerBloodlines.Traits(player).Healing(30)>0;
                var choice=planned?(needsHeal?heal??smith:smith??heal)??rest[0]:heal??rest[0];
                var before=player.Deck.Cards.ToDictionary(c=>c,c=>c.CurrentUpgradeLevel);var hp=player.Creature.CurrentHp;
                if(planned&&choice==smith){using(CampaignPlanning.Select(true))await (Task)AccessTools.Method(choice.GetType(),"SelectOption").Invoke(choice,[choice.Option])!;}
                else await UiHelper.Click(choice);
                return Result("rest",new{choice=choice.Option.GetType().Name,hpBefore=hp,hpAfter=player.Creature.CurrentHp,upgraded=before.Where(p=>p.Key.CurrentUpgradeLevel>p.Value).Select(p=>Key(p.Key)).ToArray()});
            }
            var relic=nodes.OfType<NTreasureRoomRelicHolder>().FirstOrDefault(b=>b.IsVisibleInTree()&&b.IsEnabled);
            if(relic!=null){await UiHelper.Click(relic);return Result("treasure-relic");}
            var proceed=nodes.OfType<NProceedButton>().FirstOrDefault(b=>b.IsVisibleInTree()&&b.IsEnabled);
            if(proceed!=null){await UiHelper.Click(proceed);return Result("proceed");}
            var chest=nodes.OfType<NTreasureRoom>().FirstOrDefault()?.GetNodeOrNull<NClickableControl>("Chest");
            if(chest!=null&&chest.IsVisibleInTree()&&chest.IsEnabled){await UiHelper.Click(chest);return Result("chest");}
            return Result("wait");
        }
    }
}
