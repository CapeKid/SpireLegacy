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

namespace SpireLegacy;

/// <summary>Opt-in local owned-game oracle, absent unless HEIR_TEST_MODE=1.</summary>
public static class NativeTestBridge
{
    private static bool busy;
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();
    public static void Initialize()
    {
        if (System.Environment.GetEnvironmentVariable("HEIR_TEST_MODE") != "1") return;
        Tree.ProcessFrame += Poll;
    }
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
        try
        {
            string serialized;
            // Windows can briefly lock the atomically delivered request during a file scan.
            // Retry before executing any command, so a transient lock cannot duplicate an action.
            try {serialized=File.ReadAllText(path);File.Delete(path);}catch(IOException){return;}
            using var doc = JsonDocument.Parse(serialized);
            var request = doc.RootElement;
            object result;
            switch (request.GetProperty("command").GetString())
            {
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
                case "manor": ManorUi.Open(); result = new { open = true }; break;
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
                    if (!ContentBootstrap.Ready) throw new InvalidDataException(ContentBootstrap.Failure);
                    NModalContainer.Instance?.Clear(); ManorUi.Close();
                    if(NRun.Instance != null) {
                        if(Runtime.Profile.active != null) await (Task)AccessTools.Method(typeof(RunManager),"AbandonInternal").Invoke(RunManager.Instance,[])!;
                        if(RunState().IsGameOver && Runtime.Profile.active is {} testOrphan)Runtime.Profile.Settle(testOrphan.id,testOrphan.floors,0,false);
                        await NGame.Instance.ReturnToMainMenu();
                    }
                    if (Runtime.Profile.active is {} orphan) Runtime.Profile.Settle(orphan.id,orphan.floors,0,false);
                    var cls = request.GetProperty("class").GetString()!;
                    Runtime.Profile.selected = new FamilyProfile.Heir { name="PortTester",classId=cls,traits=request.TryGetProperty("traits",out var traits) ? traits.EnumerateArray().Select(t=>t.GetString()!).ToList() : [] };
                    if(request.TryGetProperty("quick",out var quick)&&quick.GetBoolean() && Runtime.Profile.active==null)Runtime.Profile.generation=0;
                    await NGame.Instance.StartNewSingleplayerRun(ModelDb.Character<HeirCharacter>(),true,ModelDb.ActsByIndex.Select(a=>a.First()).ToArray(),[],"SPIRELEGACY-PORT-TEST",GameMode.Standard,request.TryGetProperty("ascension",out var ascension)?ascension.GetInt32():0);
                    result = StateSummary(); break;
                case "state": result = StateSummary(); break;
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
                case "ancients":
                    var ancientPlayer=RunState().Players.Single(); var originalClass=Runtime.Heir.classId; var ancientChecks=0;
                    try {
                        foreach(var heirClass in new[]{"knight","mage","ranger"}) {
                            Runtime.Heir.classId=heirClass;
                            for(var roll=0;roll<30;roll++) {
                                var tome=(MegaCrit.Sts2.Core.Models.Relics.DustyTome)ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.DustyTome>().ToMutable();
                                tome.SetupForPlayer(ancientPlayer);
                                var chosen=ModelDb.GetById<CardModel>(tome.AncientCard!);
                                if(chosen is not LegacyCard legacyRare || chosen.Rarity!=CardRarity.Rare || !Design.Pool(heirClass).Any(r=>r.Text("id")==legacyRare.Key))throw new InvalidDataException("Wrong heir Ancient reward.");
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
                    await RunManager.Instance.EnterRoomDebug(roomType,model:roomModel,showTransition:false);
                    RunManager.Instance.ActionExecutor.Unpause();
                    result = StateSummary(); break;
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
                    var rewardPlayer=RunState().Players.Single(); var allowed=Design.Pool(Runtime.Heir.classId).Select(r=>r.Text("id")).ToHashSet(); var rewardChecks=0; var shopChecks=0;
                    foreach(var room in new[]{MegaCrit.Sts2.Core.Rooms.RoomType.Monster,MegaCrit.Sts2.Core.Rooms.RoomType.Elite,MegaCrit.Sts2.Core.Rooms.RoomType.Boss})
                        for(var roll=0;roll<20;roll++) {
                            var reward=new MegaCrit.Sts2.Core.Rewards.CardReward(CardCreationOptions.ForRoom(rewardPlayer,room),6,rewardPlayer); reward.Populate();
                            var offered=reward.Cards.OfType<LegacyCard>().ToArray();
                            if(offered.Length!=6 || offered.Select(c=>c.Key).Distinct().Count()!=6 || offered.Any(c=>!allowed.Contains(c.Key))) throw new InvalidDataException("Wrong or duplicate native reward pool.");
                            rewardChecks+=offered.Length;
                        }
                    for(var store=0;store<3;store++) {
                        var inventory=MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory.CreateForNormalMerchant(rewardPlayer);
                        foreach(var entry in inventory.CharacterCardEntries) {
                            if(entry.CreationResult?.Card is not LegacyCard stocked || !allowed.Contains(stocked.Key)) throw new InvalidDataException("Wrong native merchant pool.");
                            shopChecks++;
                        }
                    }
                    result=new{rewardChecks,shopChecks,classId=Runtime.Heir.classId};break;
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
            WriteResponse(new {ok=true,result});
        }
        catch (Exception e) { GD.PushError(e.ToString()); WriteResponse(new {ok=false,error=e.ToString()}); }
        finally { busy = false; }
    }
    private static void WriteResponse(object response) {
        var temporary=Path.Combine(Runtime.DataDirectory,"response.pending");File.WriteAllText(temporary,JsonSerializer.Serialize(response));File.Move(temporary,Path.Combine(Runtime.DataDirectory,"response.json"),true);
    }
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
