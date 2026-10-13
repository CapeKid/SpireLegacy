using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;

namespace SpireLegacy;

/// <summary>Opt-in local owned-game oracle, absent unless HEIR_TEST_MODE=1.</summary>
public static class NativeTestBridge
{
    private static bool busy;
    private static readonly TestRequestLedger receipts=new();
    private static string? requestId;
    private static string? commandName;
    private static string? dllHash;
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();
    public static void Initialize()
    {
        if (System.Environment.GetEnvironmentVariable("HEIR_TEST_MODE") != "1") return;
        Tree.ProcessFrame += Poll;
    }
    public static void Shutdown()=>Tree.ProcessFrame-=Poll;
    public static IEnumerable<Node> Descendants(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren()) foreach (var descendant in Descendants(child)) yield return descendant;
    }
    private static async void Poll()
    {
        var path = Path.Combine(Runtime.DataDirectory,"request.json");
        if (busy || !File.Exists(path) || NGame.Instance == null || (NGame.Instance.MainMenu == null && NRun.Instance == null)) return;
        busy = true;
        requestId=null;commandName=null;
        try
        {
            string serialized;
            // Windows can briefly lock the atomically delivered request during a file scan.
            // Retry before executing any command, so a transient lock cannot duplicate an action.
            try {serialized=File.ReadAllText(path);File.Delete(path);}catch(IOException){return;}
            using var doc = JsonDocument.Parse(serialized);
            var request = doc.RootElement;
            requestId=request.TryGetProperty("requestId",out var id)?id.GetString():Guid.NewGuid().ToString("N");
            commandName=request.GetProperty("command").GetString();
            receipts.Claim(requestId!,Path.Combine(Runtime.DataDirectory,"oracle-receipts.txt"));
            object result;
            switch (commandName)
            {
                case "balance-control-probe":result=await ControlPilotOracle.Probe(RunState(),request.GetProperty("kind").GetString()!);break;
                case "balance-tactical-probe":await WaitForPlay(RunState().Players.Single());result=await TacticalPilot.Probe(RunState(),request.GetProperty("kind").GetString()!);break;
                case "balance-campaign-step":
                    var pilotPolicy=request.GetProperty("policy").GetString()!;
                    result=await CampaignOracle.Step(RunState(),pilotPolicy,pilotPolicy=="tactical"?TacticalPilot.Selector():UseTestSelector(),request.TryGetProperty("rewardPolicy",out var rewardsPolicy)?rewardsPolicy.GetString()!:"standalone",request.TryGetProperty("forcedReward",out var forcedReward)?forcedReward.GetString():null,request.TryGetProperty("forcedRewardFloor",out var forcedFloor)?forcedFloor.GetInt32():0);break;
                case "balance-probe":await WaitForPlay(RunState().Players.Single());var probeKey=request.GetProperty("card").GetString()!;result=await BalanceOracle.Probe(RunState(),probeKey,request.GetProperty("upgrade").GetBoolean(),probeKey=="mage_eternal_bookmark"?BalanceOracle.MaxChoices():UseTestSelector(),request.TryGetProperty("supported",out var supported)&&supported.GetBoolean());break;
                case "balance-loop":await WaitForPlay(RunState().Players.Single());result=await BalanceOracle.RecoveryLoop(RunState(),UseTestSelector(),request.TryGetProperty("kind",out var recoveryKind)&&recoveryKind.GetString()=="discard");break;
                case "balance-retention":await WaitForPlay(RunState().Players.Single());result=await BalanceOracle.RetentionSnapshots(RunState());break;
                case "crown-state":result=CrownOracle.Inspect(RunState());break;
                case "crown-play":await WaitForPlay(RunState().Players.Single());result=await CrownOracle.Play(RunState(),request.GetProperty("upgrade").GetBoolean(),request.GetProperty("win").GetBoolean());break;
                case "crown-gold":result=await CrownOracle.Gold(RunState(),request.GetProperty("gained").GetInt32(),request.GetProperty("spent").GetInt32(),request.GetProperty("lost").GetInt32(),request.GetProperty("stolen").GetInt32(),request.GetProperty("returned").GetInt32());break;
                case "power-class":await WaitForPlay(RunState().Players.Single());result=await PowerAuditOracle.Class(RunState(),request.GetProperty("mode").GetString()!,request.GetProperty("upgrade").GetBoolean(),UseTestSelector());break;
                case "power-rhythm":await WaitForPlay(RunState().Players.Single());result=await PowerAuditOracle.Rhythm(RunState(),request.GetProperty("upgrade").GetBoolean(),request.GetProperty("mode").GetString()!,UseTestSelector());break;
                case "power-barrier":await WaitForPlay(RunState().Players.Single());result=await PowerAuditOracle.Barrier(RunState(),request.GetProperty("upgrade").GetBoolean(),request.GetProperty("native").GetBoolean());break;
                case "power-engines":await WaitForPlay(RunState().Players.Single());result=await PowerAuditOracle.Engines(RunState(),request.GetProperty("mode").GetString()!,request.GetProperty("upgrade").GetBoolean(),UseTestSelector());break;
                case "power-copies":await WaitForPlay(RunState().Players.Single());result=await PowerAuditOracle.EnchantedCopies(RunState(),request.GetProperty("mode").GetString()!,request.GetProperty("upgrade").GetBoolean());break;
                case "power-snapshot":result=PowerAuditOracle.Snapshot(RunState());break;
                case "power-retention-edges":await WaitForPlay(RunState().Players.Single());result=await PowerAuditOracle.RetentionEdges(RunState(),request.GetProperty("empty").GetBoolean());break;
                case "power-coop-prepare":result=await PowerAuditOracle.CoopPrepare(RunState(),request.GetProperty("upgrade").GetBoolean());break;
                case "power-coop-play":result=await PowerAuditOracle.CoopPlay(RunState(),request.GetProperty("upgrade").GetBoolean());break;
                case "power-coop-checkpoint":result=await PowerAuditOracle.CoopCheckpoint(request.GetProperty("minimum").GetUInt32());break;
                case "balance-encounter":await WaitForPlay(RunState().Players.Single());result=await BalanceOracle.Encounter(RunState(),request.GetProperty("build").GetString()!,UseTestSelector());break;
                case "persistence-state":result=PersistenceOracle.Snapshot(RunState());break;
                case "persistence-menu":result=PersistenceOracle.ChangeMenu(RunState());break;
                case "persistence-adoption":result=PersistenceOracle.Adoption(RunState());break;
                case "compat-reinitialize":result=CompatibilityOracle.Reinitialize();break;
                case "compat-rollback":result=CompatibilityOracle.Rollback();break;
                case "compat-scopes":result=CompatibilityOracle.Scopes(RunState().Players.Single());break;
                case "compat-control":result=await CompatibilityOracle.NativeControl(request.GetProperty("patches").GetBoolean(),request.GetProperty("upgrade").GetBoolean());break;
                case "compat-export-player":result=await CompatibilityOracle.ExportPlayer(RunState().Players.Single());break;
                case "compat-import-player":result=CompatibilityOracle.ImportPlayer();break;
                case "harness-failure":await HarnessFailure();result=new{unexpected=true};break;
                case "harness-facts":result=new{completed=true,gameStore=AccessTools.Field(typeof(MegaCrit.Sts2.Core.Saves.SaveManager),"_saveStore").GetValue(MegaCrit.Sts2.Core.Saves.SaveManager.Instance)?.GetType().Name,userDirectory=OS.GetUserDataDir(),familyPath=Runtime.FamilyPath};break;
                case "harness-production-gate":
                    var originalMode=System.Environment.GetEnvironmentVariable("HEIR_TEST_MODE");
                    var subscriptions=Tree.GetSignalConnectionList(SceneTree.SignalName.ProcessFrame).Count;
                    try {foreach(var mode in new string?[]{null,"0"}){System.Environment.SetEnvironmentVariable("HEIR_TEST_MODE",mode);Initialize();if(Tree.GetSignalConnectionList(SceneTree.SignalName.ProcessFrame).Count!=subscriptions)throw new InvalidDataException("Production exposed the test bridge.");}}
                    finally{System.Environment.SetEnvironmentVariable("HEIR_TEST_MODE",originalMode);}
                    result=new{productionSubscribed=false};break;
                case "harness-owner-diagnostic":
                    var diagnosticOwner=RunState().Players.Single(p=>p.NetId==request.GetProperty("owner").GetUInt64());
                    var originalFailure=new InvalidOperationException("Synthetic owner diagnostic",new ArgumentException("Inner owner diagnostic"));
                    try{await CardPlayDiagnostics.Observe(Task.FromException(originalFailure),"harness-fixture",diagnosticOwner);throw new InvalidDataException("Failure was swallowed.");}
                    catch(InvalidOperationException failure){result=new{originalPreserved=ReferenceEquals(failure,originalFailure),text=CardPlayDiagnostics.LastFailure};}
                    break;
                case "coop-host":result=await CoopOracle.Lobby(request,true);break;
                case "coop-join":result=await CoopOracle.Lobby(request,false);break;
                case "coop-ready":CoopBloodlines.Lobby!.SetReady(request.TryGetProperty("ready",out var coopReady)?coopReady.GetBoolean():true);result=CoopOracle.State();break;
                case "coop-state":result=CoopOracle.State();break;
                case "coop-vote":result=CoopOracle.Vote();break;
                case "coop-play":result=await CoopOracle.Play(request.TryGetProperty("upgrade",out var coopUpgrade)&&coopUpgrade.GetBoolean());break;
                case "coop-upgrade":result=CoopOracle.UpgradeOwnedCard(request.GetProperty("owner").GetUInt64());break;
                case "coop-roundtrip":result=CoopOracle.RoundTrip();break;
                case "coop-lobby-checks":result=CoopAdversarialOracle.LobbyChecks();break;
                case "coop-wire-checks":result=CoopAdversarialOracle.Wire();break;
                case "coop-replay":result=CoopAdversarialOracle.Replay();break;
                case "coop-contexts":result=CoopAdversarialOracle.Scopes();break;
                case "coop-owner-checks":result=CoopAdversarialOracle.SavedOwners(RunState());break;
                case "coop-revival-checks":result=await CoopAdversarialOracle.Revival(RunState());break;
                case "coop-random-seed":result=CoopAdversarialOracle.RandomHeirSeed();break;
                case "coop-hold":CoopBloodlines.TestHoldSelections=request.GetProperty("hold").GetBoolean();if(!CoopBloodlines.TestHoldSelections)CoopBloodlines.Resend();result=new{holding=CoopBloodlines.TestHoldSelections};break;
                case "coop-turn":MegaCrit.Sts2.Core.Combat.CombatManager.Instance.SetReadyToEndTurn(PlayerBloodlines.LocalPlayer!,false);result=CoopOracle.State();break;
                case "coop-save-quit":await MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SaveRun(null);await NGame.Instance!.ReturnToMainMenu();result=new{saved=true};break;
                case "coop-resume-host":result=await CoopOracle.Resume(request,true);break;
                case "coop-resume-join":result=await CoopOracle.Resume(request,false);break;
                case "coop-resume-ready":CoopOracle.SavedLobby!.SetReady(true);result=new{ready=true};break;
                case "coop-abandon":RunManager.Instance.Abandon();result=new{abandoning=true};break;
                case "coop-leave":NGame.Instance!.MainMenu!.SubmenuStack.Pop();result=new{left=true};break;
                case "standalone-assets":
                    if(File.Exists(Path.Combine(Runtime.ModDirectory,"content-reader.zip")) || Directory.Exists(Path.Combine(Runtime.DataDirectory,"cache")) || Directory.Exists(Path.Combine(Runtime.DataDirectory,"reader")))throw new InvalidDataException("Standalone lab contains old reader/cache content.");
                    var iconFolder=Path.Combine(Runtime.DataDirectory,"icon-preview");Directory.CreateDirectory(iconFolder);
                    var iconChecks=new List<object>();
                    foreach(var icon in Design.Rows("icon_art")) {
                        var iconImage=Runtime.Texture(icon.Text("path")).GetImage();
                        if(iconImage.GetWidth()!=128 || iconImage.GetHeight()!=128 || iconImage.IsEmpty())throw new InvalidDataException("Invalid original icon: "+icon.Text("id"));
                        iconImage.SavePng(Path.Combine(iconFolder,icon.Text("id")+".png"));
                        iconChecks.Add(new{id=icon.Text("id"),path=icon.Text("path")});
                    }
                    result=new{count=iconChecks.Count,icons=iconChecks,readerPresent=false,cachePresent=false,rl2Override=System.Environment.GetEnvironmentVariable("HEIR_RL2_DIR"),Runtime.Profile.family,Runtime.Profile.crowns};break;
                case "inspect":
                    result = new { userData = OS.GetUserDataDir(), character = ModelDb.Character<HeirCharacter>().Id.ToString(), cards = Design.Rows("cards").Select(r => NativeCards.Get(r.Text("id")).Id.ToString()).ToArray(), pools = new[] { ModelDb.CardPool<KnightCardPool>().AllCards.Count(), ModelDb.CardPool<MageCardPool>().AllCards.Count(), ModelDb.CardPool<RangerCardPool>().AllCards.Count() }, labels = Descendants(Tree.Root).OfType<Label>().Where(n=>n.IsVisibleInTree()).Select(n=>n.Text).ToArray() };
                    break;
                case "select":
                    if(NRun.Instance != null)await NGame.Instance.ReturnToMainMenu();
                    NModalContainer.Instance?.Clear();
                    var menu = NGame.Instance.MainMenu!;
                    var screen = menu.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
                    screen.InitializeSingleplayer(); menu.SubmenuStack.Push(screen);
                    var button = Descendants(screen).OfType<NCharacterSelectButton>().Single(b => b.Character is HeirCharacter);
                    if(request.TryGetProperty("portraitPreview",out var preview) && preview.GetBoolean())
                        foreach(var portraitButton in Descendants(screen).OfType<NCharacterSelectButton>()) {
                            portraitButton.DebugUnlock();
                            if(portraitButton.Character is HeirCharacter)continue;
                            var reference=(TextureRect)AccessTools.Field(portraitButton.GetType(),"_icon").GetValue(portraitButton)!;
                            reference.Texture.GetImage().SavePng(Path.Combine(Runtime.DataDirectory,"portrait-reference-"+portraitButton.Character.Id.Entry+".png"));
                        }
                    screen.SelectCharacter(button,button.Character);
                    button.GrabFocus();
                    if(request.TryGetProperty("previewAscension",out var previewAscension)) {
                        var panel=(NAscensionPanel)AccessTools.Field(screen.GetType(),"_ascensionPanel").GetValue(screen)!;
                        panel.SetMaxAscension(10);panel.SetAscensionLevel(previewAscension.GetInt32());panel.AnimIn();
                    }
                    result = new { selected = true }; break;
                case "select-state":
                    var picker=Descendants(Tree.Root).OfType<NCharacterSelectScreen>().Single(s=>s.IsVisibleInTree());
                    result=new{focus=Tree.Root.GuiGetFocusOwner()?.Name.ToString(),controls=Descendants(picker).OfType<Control>().Where(c=>c.IsVisibleInTree() && (c is NCharacterSelectButton || c.Name=="SpireLegacyManor" || c.Name=="BackButton" || c is NAscensionPanel || c.Name=="InfoPanel")).Select(c=>new{name=c.Name.ToString(),rect=c.GetGlobalRect().ToString(),portrait=c is NCharacterSelectButton hb && hb.Character is HeirCharacter ? ((TextureRect)AccessTools.Field(hb.GetType(),"_icon").GetValue(hb)!).Texture==Runtime.Texture("heir/ui/heir-portrait.png") : false}).ToArray()};break;
                case "heal":
                    var healingPlayer=RunState().Players.Single();
                    healingPlayer.Creature.SetCurrentHpInternal(healingPlayer.Creature.MaxHp-10);
                    await CreatureCmd.Heal(healingPlayer.Creature,10,false);
                    result=StateSummary();break;
                case "trait-audit":result=await TraitAuditOracle.Probe(RunState());break;
                case "trait-transfer":result=TraitAuditOracle.Transfer(RunState());break;
                case "trait-redraw":result=await TraitAuditOracle.Redraw(RunState(),request.TryGetProperty("reuse",out var reuseDraw)&&reuseDraw.GetBoolean());break;
                case "trait-triggers":result=await TraitAuditOracle.Triggers(RunState());break;
                case "trait-restrictions":result=await TraitAuditOracle.Restrictions(RunState());break;
                case "trait-transforms":result=await TraitAuditOracle.Transforms(RunState());break;
                case "trait-speed":
                    var auditSpeed=request.GetProperty("value").GetDouble();if(auditSpeed<1||auditSpeed>8)throw new ArgumentOutOfRangeException("value");
                    Engine.TimeScale=auditSpeed;result=new{speed=auditSpeed};break;
                case "manor": ManorUi.Open(); result = new { open = true }; break;
                case "chronicle-fixture":result=ChronicleOracle.Fixture(request.GetProperty("unlocked").GetBoolean());break;
                case "chronicle-state":result=ChronicleOracle.State();break;
                case "lifecycle-state":result=LifecycleOracle.Inspect();break;
                case "lifecycle-complete":result=LifecycleOracle.Completion(RunState(),request.GetProperty("victory").GetBoolean(),request.GetProperty("fault").GetString()!);break;
                case "lifecycle-owners":result=LifecycleOracle.OwnerIsolation(RunState());break;
                case "chronicle-eligibility":result=ChronicleOracle.RunEligibility(RunState());break;
                case "manor-state":
                    var manor=Descendants(Tree.Root).OfType<ManorInput>().Single();
                    var manorScroll=Descendants(manor).OfType<ScrollContainer>().Single();
                    var focus=Tree.Root.GuiGetFocusOwner();
                    result=new{open=ManorUi.IsOpen,paused=Tree.Paused,scroll=manorScroll.ScrollVertical,maxScroll=manorScroll.GetVScrollBar().MaxValue-manorScroll.GetVScrollBar().Page,focus=focus?.Name.ToString(),focusText=focus is Button focusButton?focusButton.Text:focus is Label focusLabel?focusLabel.Text:null,focusableLabels=Descendants(manor).OfType<Label>().Count(l=>l.FocusMode!=Control.FocusModeEnum.None),buttons=Descendants(manor).OfType<Button>().Select(b=>new{text=b.Text,disabled=b.Disabled,focused=b.HasFocus(),rect=b.GetGlobalRect().ToString(),visible=b.IsVisibleInTree()}).ToArray()};break;
                case "close": ManorUi.Close(); result = new { open = false }; break;
                case "dismiss":
                    foreach (var ftue in Descendants(Tree.Root).OfType<MegaCrit.Sts2.Core.Nodes.Ftue.NCombatRulesFtue>().ToArray())
                        for (var page=0;page<3;page++) AccessTools.Method(ftue.GetType(),"ToggleRight").Invoke(ftue,[null]);
                    result = new { dismissed=true }; break;
                case "press":
                    var name = request.GetProperty("text").GetString();
                    var candidates = Descendants(Tree.Root).OfType<Button>().Where(b => b.IsVisibleInTree() && b.Text == name).ToArray();
                    var found = candidates[request.TryGetProperty("index",out var index) ? index.GetInt32() : 0];
                    if (found.Disabled) throw new InvalidOperationException("The button is disabled.");
                    found.EmitSignal(Button.SignalName.Pressed); result = new { pressed = name }; break;
                case "edit":
                    Descendants(Tree.Root).OfType<LineEdit>().Single(b=>b.IsVisibleInTree()).Text = request.GetProperty("value").GetString();
                    result = new { edited=true }; break;
                case "start":
                    NModalContainer.Instance?.Clear(); ManorUi.Close();
                    if(NRun.Instance != null) {
                        if(Runtime.Profile.active != null) await (Task)AccessTools.Method(typeof(RunManager),"AbandonInternal").Invoke(RunManager.Instance,[])!;
                        if(RunState().IsGameOver && Runtime.Profile.active is {} testOrphan)Runtime.Profile.Settle(testOrphan.id,testOrphan.floors,0,false);
                        await NGame.Instance.ReturnToMainMenu();
                    }
                    if (Runtime.Profile.active is {} orphan) Runtime.Profile.Settle(orphan.id,orphan.floors,0,false);
                    if(request.TryGetProperty("manorTier",out var balanceTier)) {
                        var tier=balanceTier.GetInt32();if(tier is <0 or >2)throw new ArgumentOutOfRangeException("manorTier");
                        MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SetFtuesEnabled(false);
                        MegaCrit.Sts2.Core.Saves.SaveManager.Instance.ObtainEpochOverride(MegaCrit.Sts2.Core.Timeline.EpochModel.GetId<MegaCrit.Sts2.Core.Timeline.Epochs.NeowEpoch>(),MegaCrit.Sts2.Core.Saves.EpochState.Revealed);
                        Runtime.Profile.manor=Design.Rows("manor").ToDictionary(r=>r.Text("id"),r=>tier==2?r.Number("maxLevel"):tier==1?r.Number("maxLevel")/2:0);
                    }
                    var cls = request.GetProperty("class").GetString()!;
                    if(cls is not ("knight" or "mage" or "ranger" or "ironclad"))throw new ArgumentException("Unknown campaign character.");
                    if(cls=="ironclad"&&(Runtime.Profile.manor.Values.Any(v=>v!=0)||request.TryGetProperty("traits",out var nativeTraits)&&nativeTraits.GetArrayLength()>0))throw new ArgumentException("Vanilla control cannot receive manor or traits.");
                    if(cls=="ironclad"){
                        var saves=MegaCrit.Sts2.Core.Saves.SaveManager.Instance;
                        saves.ObtainEpochOverride(MegaCrit.Sts2.Core.Timeline.EpochModel.GetId<MegaCrit.Sts2.Core.Timeline.Epochs.Ironclad2Epoch>(),MegaCrit.Sts2.Core.Saves.EpochState.Revealed);
                        saves.ObtainEpochOverride(MegaCrit.Sts2.Core.Timeline.EpochModel.GetId<MegaCrit.Sts2.Core.Timeline.Epochs.Ironclad5Epoch>(),MegaCrit.Sts2.Core.Saves.EpochState.Revealed);
                        saves.ObtainEpochOverride(MegaCrit.Sts2.Core.Timeline.EpochModel.GetId<MegaCrit.Sts2.Core.Timeline.Epochs.Ironclad7Epoch>(),MegaCrit.Sts2.Core.Saves.EpochState.Revealed);
                    }
                    Runtime.Profile.selected = cls=="ironclad"?null:new FamilyProfile.Heir { name="PortTester",classId=cls,traits=request.TryGetProperty("traits",out var traits) ? traits.EnumerateArray().Select(t=>t.GetString()!).ToList() : [] };
                    if(request.TryGetProperty("quick",out var quick) && Runtime.Profile.active==null)Runtime.Profile.generation=quick.GetBoolean()?0:3;
                    await NGame.Instance.StartNewSingleplayerRun(cls=="ironclad"?ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Ironclad>():ModelDb.Character<HeirCharacter>(),true,ModelDb.ActsByIndex.Select(a=>a.First()).ToArray(),[],request.TryGetProperty("seed",out var balanceSeed)?balanceSeed.GetString()!:"SPIRELEGACY-PORT-TEST",GameMode.Standard,request.TryGetProperty("ascension",out var ascension)?ascension.GetInt32():0);
                    result = StateSummary(); break;
                case "state": result = StateSummary(); break;
                case "relic-info": result=HeirRelicOracle.Inspect(RunState());break;
                case "relic-obtain": result=await HeirRelicOracle.Obtain(RunState(),request.GetProperty("relic").GetString()!,request.TryGetProperty("shop",out var shopRelic)&&shopRelic.GetBoolean());break;
                case "relic-counters": result=HeirRelicOracle.Counters(RunState());break;
                case "relic-roundtrip": result=await HeirRelicOracle.RoundTrip(RunState());break;
                case "relic-clear":
                    foreach(var drop in RunState().Players.Single().Relics.OfType<FamilyDropRelic>().ToArray())await RelicCmd.Remove(drop);
                    result=HeirRelicOracle.Counters(RunState());break;
                case "relic-focus":
                    var focusKey=request.GetProperty("relic").GetString()!;
                    var holder=Descendants(Tree.Root).OfType<MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder>().First(h=>Descendants(h).OfType<MegaCrit.Sts2.Core.Nodes.Relics.NRelic>().Any(n=>n.Model is FamilyDropRelic r&&r.Key==focusKey));
                    holder.GrabFocus();await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
                    result=new{focused=holder.HasFocus(),focusMode=holder.FocusMode.ToString(),right=holder.FocusNeighborRight.ToString()};break;
                case "potion-info":result=HeirPotionOracle.Inspect(RunState());break;
                case "potion-obtain":result=await HeirPotionOracle.Obtain(RunState(),request.GetProperty("potion").GetString()!,request.TryGetProperty("shop",out var potionShop)&&potionShop.GetBoolean());break;
                case "potion-use":await WaitForPlay(RunState().Players.Single());result=await HeirPotionOracle.Use(RunState(),request.GetProperty("potion").GetString()!);break;
                case "potion-controller-use":await WaitForPlay(RunState().Players.Single());result=await HeirPotionOracle.Use(RunState(),request.GetProperty("potion").GetString()!,true);break;
                case "potion-focus":result=await HeirPotionOracle.Focus(request.GetProperty("potion").GetString()!);break;
                case "potion-clear":foreach(var potion in RunState().Players.Single().Potions.ToArray())await MegaCrit.Sts2.Core.Commands.PotionCmd.Discard(potion);result=new{cleared=true};break;
                case "potion-slots":result=RunState().Players.Single().PotionSlots.Select(p=>p?.Id.ToString()).ToArray();break;
                case "vigor-room":
                    await WaitForPlay(RunState().Players.Single());
                    await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
                    RunManager.Instance.ActionExecutor.Unpause();
                    await WaitForPlay(RunState().Players.Single());result=new{ready=true};break;
                case "vigor-check":
                    await WaitForPlay(RunState().Players.Single());
                    result=await VigorOracle.Check(RunState(),request.GetProperty("card").GetString()!,request.GetProperty("upgrade").GetBoolean(),UseTestSelector());break;
                case "expansion-play":
                    await WaitForPlay(RunState().Players.Single());
                    result=await PoolExpansionOracle.Play(RunState(),request.GetProperty("card").GetString()!,request.GetProperty("upgrade").GetBoolean(),UseTestSelector());break;
                case "card-resolution":
                    await WaitForPlay(RunState().Players.Single());
                    result=await CardResolutionOracle.Play(RunState(),request.GetProperty("card").GetString()!,request.GetProperty("upgrade").GetBoolean(),request.TryGetProperty("mode",out var resolutionMode)?resolutionMode.GetString()!:"normal",UseTestSelector());break;
                case "card-choice-boundary":
                    await WaitForPlay(RunState().Players.Single());
                    result=await CardChoiceBoundaryOracle.Check(RunState(),request.GetProperty("card").GetString()!,request.GetProperty("upgrade").GetBoolean(),request.TryGetProperty("decline",out var declineChoice)&&declineChoice.GetBoolean());break;
                case "card-deck-identity":result=RunState().Players.Single().Deck.Cards.Select(c=>c.Id.ToString()+":"+c.CurrentUpgradeLevel).ToArray();break;
                case "card-flush":await WaitForPlay(RunState().Players.Single());result=await CardFlushOracle.Check(RunState(),request.GetProperty("card").GetString()!,request.GetProperty("upgrade").GetBoolean());break;
                case "card-flush-next":await WaitForPlay(RunState().Players.Single());result=CardFlushOracle.NextCombat(RunState(),request.GetProperty("card").GetString()!);break;
                case "card-coop-prepare":result=await CardCoopOracle.Prepare(RunState(),request.GetProperty("card").GetString()!,request.GetProperty("upgrade").GetBoolean(),request.GetProperty("replay").GetBoolean());break;
                case "card-coop-play":result=await CardCoopOracle.Play(RunState(),request.GetProperty("card").GetString()!);break;
                case "nimble-eligibility":result=NimbleOracle.Eligibility(RunState());break;
                case "nimble-play":
                    await WaitForPlay(RunState().Players.Single());
                    result=await NimbleOracle.Play(RunState(),request.GetProperty("card").GetString()!,request.GetProperty("upgrade").GetBoolean(),UseTestSelector());break;
                case "pool-parity":
                    object PoolCounts(CardPoolModel pool) {var cards=pool.AllCards.ToArray();return new {pool=pool.Id.ToString(),total=cards.Length,unique=cards.Select(c=>c.Id).Distinct().Count(),ordinary=cards.Count(c=>c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare),common=cards.Count(c=>c.Rarity==CardRarity.Common),uncommon=cards.Count(c=>c.Rarity==CardRarity.Uncommon),rare=cards.Count(c=>c.Rarity==CardRarity.Rare),basic=cards.Count(c=>c.Rarity==CardRarity.Basic),ancient=cards.Count(c=>c.Rarity==CardRarity.Ancient)};}
                    result=new{native=ModelDb.AllCharacters.Where(c=>c is not HeirCharacter).Select(c=>PoolCounts(c.CardPool)).ToArray(),heirs=new[]{PoolCounts(ModelDb.CardPool<KnightCardPool>()),PoolCounts(ModelDb.CardPool<MageCardPool>()),PoolCounts(ModelDb.CardPool<RangerCardPool>())}};break;
                case "pool-availability":
                    var poolPlayer=RunState().Players.Single();var available=poolPlayer.Character.CardPool.GetUnlockedCards(poolPlayer.UnlockState,poolPlayer.RunState.CardMultiplayerConstraint).OfType<LegacyCard>().ToArray();
                    result=new{expanded=NativePoolParity.Expanded,ids=available.Select(c=>c.Key).ToArray(),compendium=available.All(c=>ModelDb.AllCards.Any(m=>m.Id==c.Id))};break;
                case "visual": result = HeirVisuals.Diagnostic(); break;
                case "presentation":result=await HeirPresentationOracle.Inspect(RunState());break;
                case "presentation-rest":result=await HeirPresentationOracle.Rest(RunState());break;
                case "presentation-transition":
                    var transitionPath=HeirPresentation.TransitionPath;
                    await NGame.Instance.Transition.FadeOut(.15f,transitionPath);
                    await NGame.Instance.Transition.FadeIn(.15f,transitionPath);
                    result=new{path=transitionPath,complete=!NGame.Instance.Transition.InTransition};break;
                case "ending-visual":result=HeirEndingVisuals.Diagnostic();break;
                case "trait-appearance": result = Descendants(Tree.Root).OfType<TraitAppearance>().Where(a=>a.IsVisibleInTree()).Select(a=>a.Diagnostic()).ToArray(); break;
                case "room-visual":
                    result = Descendants(Tree.Root).OfType<HeirSprite>().Where(s=>s.Name=="SpireLegacyRoomHeir" && s.IsVisibleInTree()).Select(s=>new {classId=s.ClassId,pose=s.Animation.ToString(),events=s.Events.ToArray(),position=s.GlobalPosition.ToString(),scale=s.GlobalScale.ToString(),pixelHeight=Design.Get("character_art",s.ClassId).Number("height")*Math.Abs(s.GetGlobalTransformWithCanvas().Scale.Y * Tree.Root.GetFinalTransform().Scale.Y)}).ToArray();break;
                case "injure":
                    var injured = RunState().Players.Single().Creature;
                    await CreatureCmd.Damage(new BlockingPlayerChoiceContext(),injured,request.GetProperty("amount").GetInt32(),MegaCrit.Sts2.Core.ValueProps.ValueProp.Unblockable | MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,injured.CombatState!.GetOpponentsOf(injured).First());
                    result = StateSummary(); break;
                case "trait-hit":
                    var hitPlayer=RunState().Players.Single();
                    var traitEnemy=hitPlayer.Creature.CombatState!.GetOpponentsOf(hitPlayer.Creature).First(e=>e.IsAlive);
                    var hitProps=request.TryGetProperty("unpowered",out var unpowered) && unpowered.GetBoolean() ? MegaCrit.Sts2.Core.ValueProps.ValueProp.Unblockable | MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered : MegaCrit.Sts2.Core.ValueProps.ValueProp.Move;
                    await CreatureCmd.Damage(new BlockingPlayerChoiceContext(),hitPlayer.Creature,request.GetProperty("amount").GetInt32(),hitProps,traitEnemy);
                    result=StateSummary();break;
                case "trait-guard":
                    var guardPlayer=RunState().Players.Single();
                    var guardTargets=request.TryGetProperty("self",out var selfGuard) && selfGuard.GetBoolean() ? new[]{guardPlayer.Creature} : guardPlayer.Creature.CombatState!.GetOpponentsOf(guardPlayer.Creature).Where(e=>e.IsAlive).ToArray();
                    foreach(var guardTarget in guardTargets)await CreatureCmd.GainBlock(guardTarget,request.GetProperty("amount").GetInt32(),MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
                    result=StateSummary();break;
                case "manual":
                    var manualPlayer=RunState().Players.Single(); await WaitForPlay(manualPlayer);
                    var manualCard=manualPlayer.PlayerCombatState!.Hand.Cards.First(c=>c is LegacyCard lc && lc.Key==request.GetProperty("card").GetString());
                    var manualTarget=manualCard.TargetType==TargetType.AnyEnemy?manualPlayer.Creature.CombatState!.GetOpponentsOf(manualPlayer.Creature).First(e=>e.IsAlive):null;
                    var canPlay=manualCard.CanPlay(out var unplayable,out var preventer);
                    var accepted=manualCard.TryManualPlay(manualTarget);
                    var playDeadline=Time.GetTicksMsec()+8000;
                    while(manualCard.Pile?.Type is PileType.Hand or PileType.Play && Time.GetTicksMsec()<playDeadline)await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
                    var visualDeadline=Time.GetTicksMsec()+2000;
                    while(MegaCrit.Sts2.Core.Nodes.Cards.NCard.FindOnTable(manualCard)!=null && Time.GetTicksMsec()<visualDeadline)await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
                    result=new{canPlay,reason=unplayable.ToString(),preventer=preventer?.Id.ToString(),accepted,pile=manualCard.Pile?.Type.ToString(),tableNodePresent=MegaCrit.Sts2.Core.Nodes.Cards.NCard.FindOnTable(manualCard)!=null,playPileCount=PileType.Play.GetPile(manualPlayer).Cards.Count,queuePaused=RunManager.Instance.ActionExecutor.IsPaused,queueRunning=RunManager.Instance.ActionExecutor.IsRunning,action=RunManager.Instance.ActionExecutor.CurrentlyRunningAction?.ToString(),state=StateSummary()};break;
                case "diagnostic":
                    try {await CardPlayDiagnostics.Observe(Task.FromException(new InvalidOperationException("Owned-game diagnostic fixture")),"TEST_ONLY");}catch(InvalidOperationException){}
                    ManorUi.Open();result=new{diagnostic=CardPlayDiagnostics.LastFailure};break;
                case "architect-dialogues":
                    var endingSet=ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.TheArchitect>().DialogueSet;
                    var endingClass=Runtime.Heir.classId; var endingChecks=new List<object>();
                    try {
                        foreach(var clsForEnding in new[]{"knight","mage","ranger"}) {
                            Runtime.Heir.classId=clsForEnding;
                            foreach(var winsForEnding in new[]{0,1,4,10,100}) {
                                var ending=endingSet.GetValidDialogues(ModelDb.Character<HeirCharacter>().Id,winsForEnding,winsForEnding,false).Single();
                                if(ending.EndAttackers!=MegaCrit.Sts2.Core.Entities.Ancients.ArchitectAttackers.Both)throw new InvalidDataException("Ending animation changed.");
                                endingChecks.Add(new{classId=clsForEnding,wins=winsForEnding,lines=ending.Lines.Select(l=>new{speaker=l.Speaker.ToString(),key=l.LineText!.LocEntryKey,text=l.LineText.GetFormattedText(),next=l.NextButtonText?.GetFormattedText()}).ToArray()});
                            }
                        }
                        var baseEnding=endingSet.GetValidDialogues(ModelDb.Character<MegaCrit.Sts2.Core.Models.Characters.Ironclad>().Id,0,0,false).First();
                        if(baseEnding.Lines.Any(l=>l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY")))throw new InvalidDataException("Base character ending changed.");
                    } finally {Runtime.Heir.classId=endingClass;}
                    result=endingChecks;break;
                case "ancient-dialogues":result=AncientDialogueOracle.Inspect();break;
                case "ancient-state":result=AncientDialogueOracle.State(false);break;
                case "neow-energy":result=await HeirPresentationOracle.NeowEnergy(RunState());break;
                case "ancient-next":result=AncientDialogueOracle.State(true);break;
                case "ancient-enter":
                    var ancientModel=AncientDialogueOracle.Prepare(request.GetProperty("ancient").GetString()!,request.GetProperty("visits").GetInt32());
                    await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Event,model:ancientModel,showTransition:false);
                    RunManager.Instance.ActionExecutor.Unpause();result=AncientDialogueOracle.State(false);break;
                case "architect-state":
                case "architect-next":
                    var endingEvent=(MegaCrit.Sts2.Core.Models.Events.TheArchitect)((MegaCrit.Sts2.Core.Rooms.EventRoom)RunState().CurrentRoom!).LocalMutableEvent;
                    var endingDialogue=(MegaCrit.Sts2.Core.Entities.Ancients.AncientDialogue)AccessTools.Field(endingEvent.GetType(),"_dialogue").GetValue(endingEvent)!;
                    if(request.GetProperty("command").GetString()=="architect-next") {
                        var currentEndingLine=(int)AccessTools.Field(endingEvent.GetType(),"_currentLineIndex").GetValue(endingEvent)!;
                        if(currentEndingLine>=endingDialogue.Lines.Count-1)throw new InvalidDataException("Do not trigger victory from the dialogue fixture.");
                        await (Task)AccessTools.Method(endingEvent.GetType(),"AdvanceDialogue").Invoke(endingEvent,[])!;
                    }
                    result=new{index=(int)AccessTools.Field(endingEvent.GetType(),"_currentLineIndex").GetValue(endingEvent)!,lines=endingDialogue.Lines.Select(l=>l.LineText!.GetFormattedText()).ToArray(),options=endingEvent.CurrentOptions.Select(o=>o.Title.GetFormattedText()).ToArray()};break;
                case "orobas-refinement":result=await OrobasOracle.Check(RunState(),request.TryGetProperty("baseline",out var orobasBaseline)&&orobasBaseline.GetBoolean(),request.TryGetProperty("inspect",out var orobasInspect)&&orobasInspect.GetBoolean());break;
                case "orobas-coop":result=await OrobasOracle.Coop(RunState());break;
                case "orobas-native-control":result=await OrobasOracle.NativeControl();break;
                case "ancient-play":result=await AncientCardOracle.Check(RunState(),request.GetProperty("card").GetString()!,request.GetProperty("upgrade").GetBoolean(),UseTestSelector());break;
                case "ancients":
                    var ancientPlayer=RunState().Players.Single(); var originalClass=Runtime.Heir.classId; var ancientChecks=0;
                    try {
                        foreach(var heirClass in new[]{"knight","mage","ranger"}) {
                            Runtime.Heir.classId=heirClass;
                            for(var roll=0;roll<30;roll++) {
                                var tome=(MegaCrit.Sts2.Core.Models.Relics.DustyTome)ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.DustyTome>().ToMutable();
                                tome.SetupForPlayer(ancientPlayer);
                                var chosen=ModelDb.GetById<CardModel>(tome.AncientCard!);
                                if(chosen is not LegacyCard legacyRare || chosen.Rarity!=CardRarity.Ancient || !Design.Rows("ancient_cards").Any(r=>r.Text("classId")==heirClass&&r.Text("cardId")==legacyRare.Key))throw new InvalidDataException("Wrong heir Ancient reward.");
                                var restoredTome=(MegaCrit.Sts2.Core.Models.Relics.DustyTome)RelicModel.FromSerializable(tome.ToSerializable());
                                if(restoredTome.AncientCard!=tome.AncientCard)throw new InvalidDataException("Ancient relic save mismatch.");
                                ancientChecks++;
                            }
                        }
                    } finally {Runtime.Heir.classId=originalClass;}
                    var acquiredTome=(MegaCrit.Sts2.Core.Models.Relics.DustyTome)ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.DustyTome>().ToMutable();
                    acquiredTome.SetupForPlayer(ancientPlayer);
                    var deckBefore=ancientPlayer.Deck.Cards.Count;
                    await RelicCmd.Obtain(acquiredTome,ancientPlayer);
                    if(ancientPlayer.Deck.Cards.Count!=deckBefore+1 || !ancientPlayer.Deck.Cards.Any(c=>c.Id==acquiredTome.AncientCard && c.IsUpgraded))throw new InvalidDataException("Ancient reward was not added upgraded.");
                    var dialogueChecks=0;
                    foreach(var ancient in ModelDb.AllAncients)
                        foreach(var visits in new[]{0,1,4,10}) {
                            if(!ancient.DialogueSet.GetValidDialogues(ancientPlayer.Character.Id,visits,visits,true).Any())throw new InvalidDataException("No Ancient dialogue: "+ancient.Id);
                            dialogueChecks++;
                        }
                    foreach(var wins in new[]{0,1,4,10}) {
                        if(!ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.TheArchitect>().DialogueSet.GetValidDialogues(ancientPlayer.Character.Id,wins,wins,false).Any())throw new InvalidDataException("No ending dialogue.");
                        dialogueChecks++;
                    }
                    result=new{ancientChecks,dialogueChecks,acquired=acquiredTome.AncientCard!.ToString()};break;
                case "enter":
                    NModalContainer.Instance?.Clear();
                    var roomType=Enum.Parse<MegaCrit.Sts2.Core.Rooms.RoomType>(request.GetProperty("room").GetString()!);
                    var encounter=roomType is MegaCrit.Sts2.Core.Rooms.RoomType.Monster or MegaCrit.Sts2.Core.Rooms.RoomType.Elite or MegaCrit.Sts2.Core.Rooms.RoomType.Boss ? RunState().Act.AllEncounters.First(e=>e.RoomType==roomType).ToMutable() : null;
                    AbstractModel? roomModel=encounter;
                    if(request.TryGetProperty("event",out var eventName) && eventName.GetString()=="Neow")roomModel=ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.Neow>();
                    if(request.TryGetProperty("event",out var endingName) && endingName.GetString()=="Architect")roomModel=ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.TheArchitect>();
                    await RunManager.Instance.EnterRoomDebug(roomType,model:roomModel,showTransition:false);
                    RunManager.Instance.ActionExecutor.Unpause();
                    result = StateSummary(); break;
                case "tome-parity":result=await TomeParityOracle.Check(RunState(),request.TryGetProperty("removeBasics",out var tomeRemove)&&tomeRemove.GetBoolean(),request.TryGetProperty("forced",out var tomeForced)?tomeForced.GetString():null,request.TryGetProperty("inspect",out var tomeInspect)&&tomeInspect.GetBoolean());break;
                case "advance":
                    var advancing=RunState();
                    var next=advancing.CurrentMapPoint?.Children.FirstOrDefault() ?? advancing.Map.StartingMapPoint;
                    await RunManager.Instance.EnterMapCoord(next.coord);
                    result=StateSummary();break;
                case "save":
                    await MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SaveRun(null);
                    result=new{saved=true};break;
                case "resume":
                    NModalContainer.Instance?.Clear();
                    if(NRun.Instance != null) await NGame.Instance.ReturnToMainMenu();
                    var save = MegaCrit.Sts2.Core.Saves.SaveManager.Instance.LoadRunSave();
                    if (!save.Success || save.SaveData == null) throw new InvalidDataException("No native saved run.");
                    var restored = MegaCrit.Sts2.Core.Runs.RunState.FromSerializable(save.SaveData);
                    await RunManager.Instance.SetUpSavedSingleplayer(restored,save.SaveData);
                    await NGame.Instance.LoadRun(restored,save.SaveData.PreFinishedRoom);
                    result = StateSummary(); break;
                case "abandon":
                    ManorUi.Close();
                    await (Task)AccessTools.Method(typeof(RunManager),"AbandonInternal").Invoke(RunManager.Instance,[])!;
                    result = new {Runtime.Profile.crowns,Runtime.Profile.generation,Runtime.Profile.settledId,active=Runtime.Profile.active != null}; break;
                case "kill":
                    var victimState = RunState(); var victimPlayer = victimState.Players.Single();
                    using (UseTestSelector())
                    {
                        var victims=request.TryGetProperty("player",out var killPlayer) && killPlayer.GetBoolean() ? new[]{victimPlayer.Creature} : victimPlayer.Creature.CombatState!.GetOpponentsOf(victimPlayer.Creature).Where(e=>e.IsAlive).ToArray();
                        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(),victims,999999,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unblockable|MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,victimPlayer.Creature);
                    }
                    if(!victimPlayer.Creature.IsDead)await MegaCrit.Sts2.Core.Combat.CombatManager.Instance.CheckWinCondition();
                    result=StateSummary(); break;
                case "input":
                    var action=request.GetProperty("action").GetString()!;
                    Input.ParseInputEvent(new InputEventAction {Action=action,Pressed=true,Strength=1});
                    var holdFrames=request.TryGetProperty("frames",out var frames)?frames.GetInt32():3;
                    var holdUntil=request.TryGetProperty("milliseconds",out var milliseconds)?Time.GetTicksMsec()+(ulong)milliseconds.GetInt32():0;
                    for(var frame=0;frame<holdFrames || Time.GetTicksMsec()<holdUntil;frame++)await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
                    Input.ParseInputEvent(new InputEventAction {Action=action,Pressed=false});
                    result=new{focus=Tree.Root.GuiGetFocusOwner()?.Name.ToString(),labels=Descendants(Tree.Root).OfType<Label>().Where(n=>n.IsVisibleInTree()).Select(n=>n.Text).ToArray()}; break;
                case "cards":
                    var run = RunState(); var player = run.Players.Single(); var checks = new List<object>();
                    foreach (var row in Design.Rows("cards"))
                    {
                        var card = (LegacyCard)run.CreateCard(NativeCards.Get(row.Text("id")),player);
                        for (var upgraded=0;upgraded<2;upgraded++)
                        {
                            if (upgraded==1) CardCmd.Upgrade(card);
                            var expected = card.DesignCard;
                            if (card.DynamicVars.Damage.BaseValue != expected.Damage || card.DynamicVars.Block.BaseValue != expected.Block) throw new InvalidDataException("Native stats differ: " + card.Key);
                            var description = card.GetDescriptionForPile(PileType.Deck);
                            if (description.Contains("{0}") || description.Contains("!M!") || description.Contains("{n}") || description.Contains("{Rules}")) throw new InvalidDataException("Unresolved card text: " + card.Key);
                            _ = card.CustomPortrait;
                            checks.Add(new {id=card.Key,upgraded=card.IsUpgraded,description});
                        }
                        run.RemoveCard(card);
                    }
                    result = new { count=checks.Count,checks }; break;
                case "inheritance":
                    var geneRun=RunState(); var genePlayer=geneRun.Players.Single(); var originalGenes=Runtime.Heir.traits; var inheritedChecks=0;
                    try {
                        foreach(var trait in Design.Rows("traits").Where(t=>t.Flag("enabled"))) {
                            Runtime.Heir.traits=[trait.Text("id")];
                            foreach(var basic in Design.Rows("cards").Where(c=>c.Text("rarity")=="BASIC")) {
                                var geneCard=(LegacyCard)geneRun.CreateCard(NativeCards.Get(basic.Text("id")),genePlayer);
                                CardCmd.Upgrade(geneCard); var savedCard=geneCard.ToSerializable();
                                var reloadedCard=(LegacyCard)geneRun.LoadCard(savedCard,genePlayer);
                                var expectedGene=new CardDesign(geneCard.Key,true,[trait.Text("id")]);
                                if(reloadedCard.DynamicVars.Damage.BaseValue!=expectedGene.Damage || reloadedCard.DynamicVars.Block.BaseValue!=expectedGene.Block || reloadedCard.TraitSnapshot!=geneCard.TraitSnapshot || reloadedCard.Type!=geneCard.Type) throw new InvalidDataException("Inherited card failed native save round trip: "+trait.Text("id")+" / "+geneCard.Key);
                                CardCmd.Downgrade(reloadedCard);var downgradedGene=new CardDesign(geneCard.Key,false,[trait.Text("id")]);
                                if(reloadedCard.DynamicVars.Damage.BaseValue!=downgradedGene.Damage || reloadedCard.DynamicVars.Block.BaseValue!=downgradedGene.Block)throw new InvalidDataException("Inherited downgrade failed: "+geneCard.Key);
                                geneRun.RemoveCard(geneCard); geneRun.RemoveCard(reloadedCard); inheritedChecks++;
                            }
                        }
                    } finally {Runtime.Heir.traits=originalGenes;}
                    result=new{count=inheritedChecks}; break;
                case "rewards":
                    var rewardPlayer=RunState().Players.Single(); var allowed=NativePoolParity.Rows(Runtime.Heir.classId).Select(r=>r.Text("id")).ToHashSet(); var rewardChecks=0; var shopChecks=0;var seenRewards=new HashSet<string>();var seenShops=new HashSet<string>();
                    foreach(var room in new[]{MegaCrit.Sts2.Core.Rooms.RoomType.Monster,MegaCrit.Sts2.Core.Rooms.RoomType.Elite,MegaCrit.Sts2.Core.Rooms.RoomType.Boss})
                        for(var roll=0;roll<(room==MegaCrit.Sts2.Core.Rooms.RoomType.Boss?80:20);roll++) {
                            var reward=new MegaCrit.Sts2.Core.Rewards.CardReward(CardCreationOptions.ForRoom(rewardPlayer,room),6,rewardPlayer); reward.Populate();
                            var offered=reward.Cards.OfType<LegacyCard>().ToArray();
                            if(offered.Length!=6 || offered.Select(c=>c.Key).Distinct().Count()!=6 || offered.Any(c=>!allowed.Contains(c.Key))) throw new InvalidDataException("Wrong or duplicate native reward pool.");
                            rewardChecks+=offered.Length;
                            foreach(var offeredCard in offered)seenRewards.Add(offeredCard.Key);
                        }
                    for(var store=0;store<30;store++) {
                        var inventory=MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory.CreateForNormalMerchant(rewardPlayer);
                        foreach(var entry in inventory.CharacterCardEntries) {
                            if(entry.CreationResult?.Card is not LegacyCard stocked || !allowed.Contains(stocked.Key)) throw new InvalidDataException("Wrong native merchant pool.");
                            shopChecks++;
                            seenShops.Add(stocked.Key);
                        }
                    }
                    result=new{rewardChecks,shopChecks,classId=Runtime.Heir.classId,seenRewards=seenRewards.ToArray(),seenShops=seenShops.ToArray()};break;
                case "smoke":
                    var smokePlayer=RunState().Players.Single();var smokeCombat=smokePlayer.Creature.CombatState!;
                    await WaitForPlay(smokePlayer);
                    MegaCrit.Sts2.Core.Saves.SaveManager.Instance.MarkFtueAsComplete("shuffle_ftue");
                    var previousScale=Engine.TimeScale;Engine.TimeScale=8;var played=new List<string>();
                    try {
                        foreach(var smokeRow in Design.Rows("cards").Skip(request.GetProperty("offset").GetInt32()).Take(request.GetProperty("limit").GetInt32())) {
                            foreach(var enemy in smokeCombat.GetOpponentsOf(smokePlayer.Creature)){enemy.SetMaxHpInternal(100000);enemy.SetCurrentHpInternal(100000);}
                            smokePlayer.Creature.SetMaxHpInternal(100000);smokePlayer.Creature.SetCurrentHpInternal(100000);
                            foreach(var power in smokePlayer.Creature.Powers.Where(p=>p is not P_class).ToArray()) await PowerCmd.Remove(power);
                            var smokeCard=smokeCombat.CreateCard(NativeCards.Get(smokeRow.Text("id")),smokePlayer);
                            if(request.TryGetProperty("upgrade",out var smokeUpgrade)&&smokeUpgrade.GetBoolean())CardCmd.Upgrade(smokeCard);
                            await CardPileCmd.Add(smokeCard,PileType.Hand,skipVisuals:true);
                            using(UseTestSelector()) await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(),smokeCard,smokeCombat.GetOpponentsOf(smokePlayer.Creature).First(e=>e.IsAlive));
                            played.Add(smokeRow.Text("id"));
                        }
                    } finally {Engine.TimeScale=previousScale;}
                    result=new{count=played.Count,played};break;
                case "play":
                    var state = RunState(); var actor = state.Players.Single();
                    var combat = actor.Creature.CombatState ?? throw new InvalidOperationException("Combat is not active.");
                    await WaitForPlay(actor);
                    var model = combat.CreateCard(NativeCards.Get(request.GetProperty("card").GetString()!),actor);
                    if (request.TryGetProperty("upgrade",out var upgrade) && upgrade.GetBoolean()) CardCmd.Upgrade(model);
                    await CardPileCmd.Add(model,PileType.Hand,skipVisuals:true);
                    using (UseTestSelector()) await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(),model,combat.GetOpponentsOf(actor.Creature).FirstOrDefault(e=>e.IsAlive));
                    result = StateSummary(); break;
                case "discard-balance":
                    var discardRun = RunState(); var discardActor = discardRun.Players.Single();
                    await WaitForPlay(discardActor);
                    var discardKey = request.GetProperty("card").GetString()!;
                    var discardCard = (LegacyCard)discardActor.Creature.CombatState!.CreateCard(NativeCards.Get(discardKey),discardActor);
                    CardCmd.Upgrade(discardCard);
                    await CardPileCmd.Add(discardCard,PileType.Hand,skipVisuals:true);
                    var energyBeforeDiscard = discardActor.PlayerCombatState!.Energy;
                    var handBeforeDiscard = PileType.Hand.GetPile(discardActor).Cards.Count;
                    await CardCmd.Discard(new BlockingPlayerChoiceContext(),discardCard);
                    var firstEnergyReward = discardActor.PlayerCombatState.Energy-energyBeforeDiscard;
                    var firstDrawReward = PileType.Hand.GetPile(discardActor).Cards.Count-handBeforeDiscard+1;
                    var serializedDiscard = discardCard.ToSerializable();
                    var restoredDiscard = (LegacyCard)discardRun.LoadCard(serializedDiscard,discardActor);
                    discardRun.RemoveCard(restoredDiscard);
                    discardActor.Creature.CombatState.AddCard(restoredDiscard,discardActor);
                    await CardPileCmd.Add(restoredDiscard,PileType.Hand,skipVisuals:true);
                    var energyBeforeRepeat = discardActor.PlayerCombatState.Energy;
                    var handBeforeRepeat = PileType.Hand.GetPile(discardActor).Cards.Count;
                    await CardCmd.Discard(new BlockingPlayerChoiceContext(),restoredDiscard);
                    var secondEnergyReward = discardActor.PlayerCombatState.Energy-energyBeforeRepeat;
                    var secondDrawReward = PileType.Hand.GetPile(discardActor).Cards.Count-handBeforeRepeat+1;
                    result = new {firstEnergyReward,firstDrawReward,secondEnergyReward,secondDrawReward,rewardTurn=restoredDiscard.DiscardRewardTurn,turn=discardActor.PlayerCombatState.TurnNumber};break;
                case "retention-check":
                    result=await RetentionOracle.Check(RunState(),request.GetProperty("upgrade").GetBoolean(),request.GetProperty("stack").GetBoolean(),request.GetProperty("intrinsic").GetBoolean(),request.GetProperty("fullHand").GetBoolean());break;
                case "retention-confirm":
                    // Hidden labs cannot receive global hotkeys (native focus guard).
                    // Drive the same native selection/release callbacks instead.
                    var selectingHand=MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand.Instance!;
                    var selections=(List<CardModel>)AccessTools.Field(selectingHand.GetType(),"_selectedCards").GetValue(selectingHand)!;
                    if(selections.Count==0)AccessTools.Method(selectingHand.GetType(),"SelectCardInSimpleMode").Invoke(selectingHand,[Descendants(selectingHand).OfType<MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder>().First(h=>h.IsVisibleInTree())]);
                    var confirming=(MegaCrit.Sts2.Core.Nodes.CommonUi.NConfirmButton)AccessTools.Field(selectingHand.GetType(),"_selectModeConfirmButton").GetValue(selectingHand)!;
                    confirming.EmitSignal(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.SignalName.Released,confirming);
                    result=new{confirmed=true};break;
                case "turn":
                    var turnPlayer = RunState().Players.Single();
                    MegaCrit.Sts2.Core.Combat.CombatManager.Instance.SetReadyToEndTurn(turnPlayer,false);
                    result = new { endingTurn=true }; break;
                case "screenshot":
                    await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
                    var filename = Path.Combine(Runtime.DataDirectory,"screenshot.png");
                    var error = Tree.Root.GetTexture().GetImage().SavePng(filename);
                    result = new { path = filename, error = error.ToString() }; break;
                default: throw new InvalidDataException("Unknown owned-game test command.");
            }
            await WriteResponse(new {ok=true,result});
        }
        catch (Exception e) { var detail=DiagnosticText.Describe(e,()=>"Owned-game oracle command: "+commandName);GD.PushError(detail);await WriteResponse(new {ok=false,error=detail}); }
        finally { busy = false; }
    }
    private static async Task WriteResponse(object response) {
        var body=JsonSerializer.SerializeToElement(response);
        object? ReadFact(Func<object?> read) {try{return read();}catch(Exception e){return new{unavailable=e.GetType().Name};}}
        var metadata=new {requestId,command=commandName,utc=DateTime.UtcNow.ToString("O"),dllSha256=ReadFact(()=>dllHash??=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(typeof(Runtime).Assembly.Location))).ToLowerInvariant()),
            sourceSha=System.Environment.GetEnvironmentVariable("HEIR_TEST_SOURCE_SHA"),sourcePatchSha256=System.Environment.GetEnvironmentVariable("HEIR_TEST_PATCH_SHA256"),seed=ReadFact(()=>((RunState?)AccessTools.Property(typeof(RunManager),"State").GetValue(RunManager.Instance))?.Rng.StringSeed),game=ReadFact(()=>ReleaseInfoManager.Instance.ReleaseInfo?.Version),mod=ReadFact(()=>CardPlayDiagnostics.Version),
            platform=System.Runtime.InteropServices.RuntimeInformation.OSDescription,profile=Runtime.DataDirectory,
            mods=ReadFact(()=>MegaCrit.Sts2.Core.Modding.ModManager.Mods.Where(m=>m.state==MegaCrit.Sts2.Core.Modding.ModLoadState.Loaded).Select(m=>new{id=m.manifest?.id,version=m.manifest?.version}).ToArray())};
        var envelope=new Dictionary<string,object?>{{"requestId",requestId},{"metadata",metadata}};
        foreach(var property in body.EnumerateObject())envelope[property.Name]=property.Value;
        var serialized=JsonSerializer.Serialize(envelope);
        var temporary=Path.Combine(Runtime.DataDirectory,"response.pending");
        for(var attempt=0;attempt<5;attempt++) {
            try{File.WriteAllText(temporary,serialized);File.Move(temporary,Path.Combine(Runtime.DataDirectory,"response.json"),true);return;}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException){if(attempt==4){GD.PushWarning("Oracle result publication failed; do not replay the claimed command: "+error.Message);return;}await Task.Delay(50);}
        }
    }
    private static async Task HarnessFailure(){await Task.Delay(1);throw new InvalidOperationException("Awaited harness fixture",new ArgumentException("Inner harness fixture"));}
    private static async Task WaitForPlay(Player player) {
        var deadline=Time.GetTicksMsec()+15000;
        while(player.PlayerCombatState?.Phase!=MegaCrit.Sts2.Core.Combat.PlayerTurnPhase.Play) {
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Native combat did not enter the playable phase.");
            await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
        }
    }
    private static RunState RunState() => (RunState?)AccessTools.Property(typeof(RunManager),"State").GetValue(RunManager.Instance) ?? throw new InvalidOperationException("No test run is active.");
    private static object StateSummary()
    {
        var run = RunState(); var player = run.Players.Single();
        return new { floor=run.TotalFloor,room=run.CurrentRoom?.GetType().Name,heir=new {Runtime.Heir.name,Runtime.Heir.classId,Runtime.Heir.traits}, hp=player.Creature.CurrentHp,maxHp=player.Creature.MaxHp,block=player.Creature.Block,gold=player.Gold,energy=player.PlayerCombatState?.Energy,turn=player.PlayerCombatState?.TurnNumber,phase=player.PlayerCombatState?.Phase.ToString(),relics=player.Relics.Select(r=>new{id=r.Id.ToString(),title=r.Title.GetFormattedText()}).ToArray(),hand=player.PlayerCombatState?.Hand.Cards.Select(c=>c.Id.ToString()).ToArray(),powers=player.Creature.Powers.Select(p=>new {id=p.Id.ToString(),amount=p.Amount,display=p.DisplayAmount,title=p.Title.GetFormattedText()}).ToArray(),enemies=player.Creature.CombatState?.GetOpponentsOf(player.Creature).Select(e=>new {hp=e.CurrentHp,block=e.Block,powers=e.Powers.Select(p=>new{id=p.Id.ToString(),amount=p.Amount}).ToArray()}).ToArray() };
    }
    // Test-only selector scope acquired across the regular/beta API signatures.
    private static IDisposable UseTestSelector()
    {
        var selector = new FirstChoiceSelector();
        var method = AccessTools.Method(typeof(CardSelectCmd), "UseSelector", [typeof(ICardSelector), typeof(bool)])
            ?? AccessTools.Method(typeof(CardSelectCmd), "UseSelector", [typeof(ICardSelector)]);
        return (IDisposable)method.Invoke(null, method.GetParameters().Length == 2 ? [selector, false] : [selector])!;
    }
    private sealed class FirstChoiceSelector : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options,int minSelect,int maxSelect) => Task.FromResult(options.Take(Math.Max(0,Math.Max(minSelect,Math.Min(1,maxSelect)))));
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,IReadOnlyList<CardRewardAlternative> alternatives) => new() { card=options.FirstOrDefault()?.Card };
    }
}
