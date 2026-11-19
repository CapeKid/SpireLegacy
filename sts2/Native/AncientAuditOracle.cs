using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace SpireLegacy;

// Disposable owned-game profiles only; exposed exclusively through the test bridge.
public static class AncientAuditOracle
{
    private static void Require(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
    private static async Task Wait(Func<bool> done){var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+25000;while(!done()){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Ancient audit native transition.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}}
    private static EventModel Event(RunState run)=>((EventRoom)run.CurrentRoom!).LocalMutableEvent;
    public static async Task<object> LeafyPoulticeChoice(RunState run)
    {
        var neow = (Neow)Event(run);
        await Wait(() => neow.CurrentOptions.Count > 0);
        var player = neow.Owner!;
        var startingMaxHp = player.Creature.MaxHp;
        var offered = neow.CurrentOptions.Any(option => option.Relic is LeafyPoultice);
        var strike = player.Deck.Cards.First(card => card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike));
        var defend = player.Deck.Cards.First(card => card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Defend));
        var deckCount = player.Deck.Cards.Count;
        // Use the native callback even if eligibility excluded it, as with a preselected or forced blessing.
        await (neow.CurrentOptions.FirstOrDefault(option => option.Relic is LeafyPoultice)
            ?? neow.AllPossibleOptions.Single(option => option.Relic is LeafyPoultice)).Chosen();
        var transformed = !player.Deck.Cards.Contains(strike) && !player.Deck.Cards.Contains(defend) && player.Deck.Cards.Count == deckCount;
        return new { offered, startingMaxHp, maxHp = player.Creature.MaxHp, hp = player.Creature.CurrentHp, transformed, finished = neow.IsFinished };
    }

    public static async Task SaveAncientHealthLock(RunState run)
    {
        var room = (EventRoom)run.CurrentRoom!;
        var player = room.LocalMutableEvent.Owner!;
        Require(room.CanonicalEvent is Neow or Vakuu && room.LocalMutableEvent.IsFinished && player.Creature.MaxHp == 1 &&
            player.Relics.Any(relic => relic is LeafyPoultice or PrecariousShears || relic.Id.Entry is "DISTINGUISHED_CAPE" or "SERE_TALON"),
            "Fixture requires a finished Ancient health-cost reward at 1 max HP.");
        player.Creature.SetCurrentHpInternal(0);
        await SaveManager.Instance.SaveRun(room);
    }

    public static async Task<object> PrecariousShearsChoice(RunState run, bool forceLowHp)
    {
        var neow = (Neow)Event(run);
        await Wait(() => neow.CurrentOptions.Count > 0);
        var player = neow.Owner!;
        if (forceLowHp) player.Creature.SetCurrentHpInternal(1);
        var hp = player.Creature.CurrentHp;
        var deck = player.Deck.Cards.Count;
        var completedRuns = SaveManager.Instance.Progress.NumberOfRuns;
        var eligible = ModelDb.Relic<PrecariousShears>().IsAllowedAtNeow(player);
        var offered = neow.CurrentOptions.FirstOrDefault(option => option.Relic is PrecariousShears);
        await (offered ?? neow.AllPossibleOptions.Single(option => option.Relic is PrecariousShears)).Chosen();
        var tree = (SceneTree)Engine.GetMainLoop();
        for (var i = 0; i < 10; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var save = SaveManager.Instance.LoadRunSave();
        return new { eligible, offered = offered != null, hpBefore = hp, hp = player.Creature.CurrentHp, removed = deck - player.Deck.Cards.Count,
            finished = neow.IsFinished, ended = SaveManager.Instance.Progress.NumberOfRuns > completedRuns, resumable = save.Success };
    }

    public static async Task<object> OtherMaxHpChoice(RunState run, bool vakuu)
    {
        EventModel canonical = vakuu ? ModelDb.Event<Vakuu>() : ModelDb.Event<DrowningBeacon>();
        await RunManager.Instance.EnterRoomDebug(RoomType.Event, model: canonical, showTransition: false);
        var model = Event(run);
        await Wait(() => model.CurrentOptions.Count > 0);
        var player = model.Owner!;
        var choice = vakuu
            ? ((Vakuu)model).AllPossibleOptions.Single(option => option.WillKillPlayer != null &&
                option.Relic?.Id.Entry is "DISTINGUISHED_CAPE" or "SERE_TALON")
            : model.CurrentOptions.Single(option => option.TextKey.EndsWith(".CLIMB", StringComparison.Ordinal));
        await choice.Chosen();
        return new { hp = player.Creature.CurrentHp, maxHp = player.Creature.MaxHp, finished = model.IsFinished,
            reward = player.Relics.Last().Id.Entry };
    }

    public static async Task<object> PersistentNeow(RunState run,int visits)
    {
        AncientDialogueOracle.Prepare("NEOW",visits);run.ExtraFields.StartedWithNeow=true;run.CurrentMapPoint!.PointType=MapPointType.Ancient;
        await RunManager.Instance.EnterMapPointInternal(run.ActFloor,MapPointType.Ancient,null,false);
        await Wait(()=>NativeTestBridge.Descendants(((SceneTree)Engine.GetMainLoop()).Root).OfType<NAncientEventLayout>().Any(l=>l.IsVisibleInTree()&&((List<AncientDialogueLine>)AccessTools.Field(typeof(NAncientEventLayout),"_dialogue").GetValue(l)!).Count==4));
        return AncientDialogueOracle.State(false);
    }
    public static object Fallbacks()
    {
        Require(PlayerBloodlines.LocalPlayer==null,"Fallback selectors require the menu.");var heirId=ModelDb.Character<HeirCharacter>().Id;var set=new AncientDialogueSet{FirstVisitEverDialogue=null,CharacterDialogues=new(),AgnosticDialogues=[]};
        Require(!set.GetValidDialogues(heirId,0,0,false).Any(),"Unregistered empty native set changed.");
        // Emulate a future native Ancient's getter registration without shipping or
        // adding a fake game model. Only this throwaway set receives the test identity.
        var type=typeof(HeirAncientConversations).GetNestedType("Identity",System.Reflection.BindingFlags.NonPublic)!;
        var identity=Activator.CreateInstance(type,["FUTURE_ANCIENT"])!;var table=AccessTools.Field(typeof(HeirAncientConversations),"Sets").GetValue(null)!;
        AccessTools.Method(table.GetType(),"Add").Invoke(table,[set,identity]);var original=Runtime.Heir.classId;var results=new List<object>();
        try{foreach(var cls in new[]{"knight","mage","ranger"})foreach(var visits in new[]{0,100}){Runtime.Heir.classId=cls;var dialogue=set.GetValidDialogues(heirId,visits,200,false).Single();Require(dialogue.Lines.Count==4&&dialogue.Lines.All(l=>l.LineText!.Exists()&&!string.IsNullOrWhiteSpace(l.LineText.GetFormattedText())),"Missing fallback localization.");results.Add(new{cls,visits,keys=dialogue.Lines.Select(l=>l.LineText!.LocEntryKey).ToArray()});}}
        finally{Runtime.Heir.classId=original;}
        Require(!set.GetValidDialogues(ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Ironclad>().Id,0,0,false).Any(),"Future set changed native empty behavior.");return new{emptyPreserved=true,future=results};
    }
    public static async Task<object> Advance(RunState run,bool controller)
    {
        var layout=NativeTestBridge.Descendants(((SceneTree)Engine.GetMainLoop()).Root).OfType<NAncientEventLayout>().Single(l=>l.IsVisibleInTree());
        var hitbox=(NAncientDialogueHitbox)AccessTools.Field(typeof(NAncientEventLayout),"_dialogueHitbox").GetValue(layout)!;
        var before=(int)AccessTools.Field(typeof(NAncientEventLayout),"_currentDialogueLine").GetValue(layout)!;
        Require(before<3,"Dialogue already finished.");
        if(controller){var hotkey=hitbox.GetHotkey()??throw new InvalidDataException("Missing native dialogue hotkey.");hitbox.GrabFocus();Input.ParseInputEvent(new InputEventAction{Action=hotkey,Pressed=true,Strength=1});await ((SceneTree)Engine.GetMainLoop()).ToSignal((SceneTree)Engine.GetMainLoop(),SceneTree.SignalName.ProcessFrame);Input.ParseInputEvent(new InputEventAction{Action=hotkey,Pressed=false});}
        else hitbox.EmitSignal(NClickableControl.SignalName.Released,hitbox);
        await Wait(()=>(int)AccessTools.Field(typeof(NAncientEventLayout),"_currentDialogueLine").GetValue(layout)! == before+1);
        return AncientDialogueOracle.State(false);
    }
    public static async Task<object> Choose(RunState run)
    {
        var model=Event(run);var option=model.CurrentOptions.First(o=>!o.IsLocked);var before=model.Owner!.Relics.Count();var deck=model.Owner.Deck.Cards.Count;
        var tree=(SceneTree)Engine.GetMainLoop();var button=NativeTestBridge.Descendants(tree.Root).OfType<NEventOptionButton>().Single(b=>b.Option==option&&b.IsVisibleInTree());Require(button.IsEnabled,"Reward button disabled.");
        button.ForceClick();var deadline=Time.GetTicksMsec()+25000;var rewardSelections=0;var bundleStages=new Dictionary<NChooseABundleSelectionScreen,int>();
        while(!model.IsFinished){
            if(Time.GetTicksMsec()>deadline){tree.Root.GetViewport().GetTexture().GetImage().SavePng(System.IO.Path.Combine(Runtime.DataDirectory,"ancient-choice-timeout.png"));throw new TimeoutException($"Ancient reward selection: {option.Relic?.Id}; chosen={option.WasChosen}; acquired={model.Owner.Relics.Count()-before}; visible={string.Join(',',NativeTestBridge.Descendants(tree.Root).OfType<Control>().Where(c=>c.IsVisibleInTree()).Select(c=>c.GetType().Name).Distinct())}");}
            var rewardScreen=NativeTestBridge.Descendants(tree.Root).OfType<NCardRewardSelectionScreen>().FirstOrDefault(n=>n.IsVisibleInTree());
            if(rewardScreen!=null){var holder=NativeTestBridge.Descendants(rewardScreen).OfType<NCardHolder>().FirstOrDefault(h=>h.CardModel!=null&&h.IsVisibleInTree());if(holder!=null){holder.EmitSignal(NCardHolder.SignalName.Pressed,holder);rewardSelections++;}}
            var bundleScreen=NativeTestBridge.Descendants(tree.Root).OfType<NChooseABundleSelectionScreen>().FirstOrDefault(n=>n.IsVisibleInTree());
            if(bundleScreen!=null){var stage=bundleStages.GetValueOrDefault(bundleScreen);var selected=AccessTools.Field(bundleScreen.GetType(),"_selectedBundle").GetValue(bundleScreen);
                if((stage==0||stage==2)&&selected==null){NativeTestBridge.Descendants(bundleScreen).OfType<NCardBundle>().First(n=>n.IsVisibleInTree()).Hitbox.ForceClick();bundleStages[bundleScreen]=stage+1;}
                else if(stage==1||stage==3){var control=(NButton)AccessTools.Field(bundleScreen.GetType(),stage==1?"_previewCancelButton":"_previewConfirmButton").GetValue(bundleScreen)!;if(control.IsEnabled){control.ForceClick();bundleStages[bundleScreen]=stage+1;}}
            }
            var rewards=NativeTestBridge.Descendants(tree.Root).OfType<NRewardsScreen>().FirstOrDefault(n=>n.IsVisibleInTree());
            if(rewards!=null&&rewardScreen==null&&bundleScreen==null){var reward=NativeTestBridge.Descendants(rewards).OfType<NRewardButton>().FirstOrDefault(b=>b.IsVisibleInTree()&&b.IsEnabled&&b.Reward!=null);if(reward!=null)reward.ForceClick();else NativeTestBridge.Descendants(rewards).OfType<NProceedButton>().FirstOrDefault(b=>b.IsVisibleInTree()&&b.IsEnabled)?.ForceClick();}
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        Require(option.WasChosen,"Native reward callback did not execute.");
        var after=model.Owner.Relics.Count();var acquired=model.Owner.Relics.Skip(before).Select(r=>r.Id.ToString()).ToArray();
        // Native EventOption guards repeated activation of this same choice.
        await option.Chosen();Require(model.Owner.Relics.Count()==after,"Reward callback acquired twice.");
        return new{key=option.TextKey,finished=model.IsFinished,acquired,deckDelta=model.Owner.Deck.Cards.Count-deck,rewardSelections,bundleCancelAndConfirm=bundleStages.Count,once=true,options=model.CurrentOptions.Select(o=>o.TextKey).ToArray()};
    }
    public static async Task<object> Tome(RunState run)
    {
        var player=run.Players.Single();var cls=PlayerBloodlines.Heir(player).classId;var seen=new HashSet<string>();
        for(var i=0;i<60;i++){var tome=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();tome.SetupForPlayer(player);var card=(LegacyCard)ModelDb.GetById<CardModel>(tome.AncientCard!);Require(card.DesignCard.Row.Text("classId")==cls&&card.Rarity==CardRarity.Ancient,"Tome selected another class/rarity.");seen.Add(card.Key);}
        Require(seen.Count==2,"Tome did not offer both class cards.");
        var reward=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();reward.SetupForPlayer(player);var before=player.Deck.Cards.Count;await RelicCmd.Obtain(reward,player);
        var gained=player.Deck.Cards.Last();Require(player.Deck.Cards.Count==before+1&&gained.IsUpgraded&&gained.Id==reward.AncientCard,"Tome acquisition identity/upgrade.");
        var restored=CardModel.FromSerializable(gained.ToSerializable());restored.Owner=player;Require(restored.Id==gained.Id&&restored.IsUpgraded&&restored.Owner==player,"Ancient card saved identity.");
        return new{cls,seen=seen.Order().ToArray(),acquired=gained.Id.ToString(),upgrade=gained.CurrentUpgradeLevel,saveRoundTrip=true};
    }
    public static async Task<object> Modifier(RunState run,string key,bool up,IDisposable selector)
    {
        var player=run.Players.Single();await PlayerCmd.GainEnergy(3,player);
        if(PlayerBloodlines.Traits(player).Has("pacifist")&&Design.Get("cards",key).Text("type")=="ATTACK"){
            using(selector){var combat=player.Creature.CombatState!;var card=combat.CreateCard(NativeCards.Get(key),player);if(up)CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);var energy=player.PlayerCombatState!.Energy;
                Require(!card.TryManualPlay(combat.GetOpponentsOf(player.Creature).First(e=>e.IsAlive))&&player.PlayerCombatState.Energy==energy,"Pacifist played a forbidden Ancient Attack.");
                var restored=run.LoadCard(card.ToSerializable(),player);Require(restored.Id==card.Id&&restored.IsUpgraded==up,"Restricted Ancient card save identity.");run.RemoveCard(restored);
                return new{key,upgrade=up,blocked=true,saveRoundTrip=true,tableNodePresent=false,pile="Hand",energyChange=0};}
        }
        return await PoolExpansionOracle.Play(run,key,up,selector);
    }
    public static async Task<object> EndingEnter(RunState run,int wins)
    {
        SaveManager.Instance.Progress.GetOrCreateCharacterStats(run.Players.Single().Character.Id).TotalWins=wins;
        run.CurrentActIndex=run.Acts.Count-1;
        await RunManager.Instance.EnterRoomDebug(RoomType.Event,model:ModelDb.Event<TheArchitect>(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();await Wait(()=>Event(run).CurrentOptions.Count>0);
        return new{act=run.CurrentActIndex,cls=PlayerBloodlines.Heir(run.Players.Single()).classId,lines=((AncientDialogue)AccessTools.Field(typeof(TheArchitect),"_dialogue").GetValue(Event(run))!).Lines.Select(l=>l.LineText!.LocEntryKey).ToArray()};
    }
    public static async Task<object> EndingAdvance(RunState run)
    {
        var model=Event(run);var before=(int)AccessTools.Field(typeof(TheArchitect),"_currentLineIndex").GetValue(model)!;
        Require(before<3,"Use final Proceed fixture.");await model.CurrentOptions.Single().Chosen();
        return new{index=(int)AccessTools.Field(typeof(TheArchitect),"_currentLineIndex").GetValue(model)!,options=model.CurrentOptions.Select(o=>o.Title.GetFormattedText()).ToArray()};
    }
    public static async Task<object> EndingWin(RunState run)
    {
        var model=Event(run);Require((int)AccessTools.Field(typeof(TheArchitect),"_currentLineIndex").GetValue(model)! == 3,"Not at final Proceed.");var option=model.CurrentOptions.Single();
        var id=Runtime.Profile.active!.id;var generation=Runtime.Profile.generation;
        await option.Chosen();await Wait(()=>Runtime.Profile.active==null&&Runtime.Profile.settledId==id);
        var after=Runtime.Profile.generation;string? repeatDiagnostic=null;
        try{await option.Chosen();}catch(Exception error){Require(run.IsGameOver&&model.IsFinished,"Unfinished ending repeat failed.");repeatDiagnostic=error.ToString();}
        Require(Runtime.Profile.generation==after&&after==generation+1,"Ending settled twice.");
        return new{nativeGameOver=run.IsGameOver,settled=id,generation=after,once=true,repeatDiagnostic,wins=Runtime.Profile.chronicle.classes.Values.Sum(c=>c.wins)};
    }
    public static async Task<object> Owners(RunState run)
    {
        var prior=PlayerBloodlines.EligibilityOwner.Value;var results=new List<object>();
        foreach(var player in run.Players){var model=(TheArchitect)ModelDb.Event<TheArchitect>().ToMutable();var begin=AccessTools.Method(typeof(EventModel),"BeginEvent");
            var synchronizer=AccessTools.Field(RunManager.Instance.EventSynchronizer.GetType(),"_combatSynchronizer")?.GetValue(RunManager.Instance.EventSynchronizer);
            await (Task)begin.Invoke(model,begin.GetParameters().Length==2?[player,false]:[player,synchronizer,false])!;
            var dialogue=(AncientDialogue)AccessTools.Field(typeof(TheArchitect),"_dialogue").GetValue(model)!;var ancientKeys=new List<string>();
            if(player.Character is HeirCharacter){var cls=PlayerBloodlines.Heir(player).classId;Require(dialogue.Lines.All(l=>l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY.ARCHITECT."+cls+".")),"Remote ending used another heir's class.");}
            else Require(dialogue.Lines.All(l=>!l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY")),"Native ending contaminated.");
            try{PlayerBloodlines.EligibilityOwner.Value=player;
                foreach(var ancient in ModelDb.AllAncients)foreach(var visits in new[]{0,1,100}){
                    var lines=ancient.DialogueSet.GetValidDialogues(player.Character.Id,visits,200,true).First().Lines;
                    if(player.Character is HeirCharacter)Require(lines.All(l=>l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY.ANCIENT."+ancient.Id.Entry+"."+PlayerBloodlines.Heir(player).classId+".")),"Remote Ancient selector used another heir.");
                    else Require(lines.All(l=>!l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY")),"Native Ancient selector contaminated.");ancientKeys.Add(lines[0].LineText!.LocEntryKey);
                }
            }finally{PlayerBloodlines.EligibilityOwner.Value=prior;}
            results.Add(new{owner=player.NetId,cls=player.Character.Id.ToString(),keys=dialogue.Lines.Select(l=>l.LineText!.LocEntryKey).ToArray(),ancientKeys});}
        Require(PlayerBloodlines.EligibilityOwner.Value==prior,"Dialogue owner scope leaked.");return results;
    }
}
