using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Hooks;
namespace SpireLegacy;
public static class LegacyPowers
{
 private static readonly Dictionary<string,string> NativeNames=new(){
  ["strength"]="StrengthPower",["dexterity"]="DexterityPower",["poison"]="PoisonPower",["weak"]="WeakPower",["vulnerable"]="VulnerablePower",["vigor"]="VigorPower",["thorns"]="ThornsPower",["nextEnergy"]="EnergyNextTurnPower",["nextDraw"]="DrawCardsNextTurnPower",["nextBlock"]="BlockNextTurnPower",["no_draw"]="NoDrawPower",
  ["juggernaut"]="JuggernautPower",["rupture"]="RupturePower",["feel_no_pain"]="FeelNoPainPower",["barricade"]="BarricadePower",["demon_form"]="DemonFormPower",["dark_embrace"]="DarkEmbracePower",["buffer"]="BufferPower",["panache"]="PanachePower",["tools"]="ToolsOfTheTradePower",["after_image"]="AfterimagePower",["envenom"]="EnvenomPower",["burst"]="BurstPower",["noxious"]="NoxiousFumesPower"};
 private static readonly Dictionary<string,Type> CustomTypes=typeof(LegacyPower).Assembly.GetTypes().Where(t=>!t.IsAbstract&&typeof(LegacyPower).IsAssignableFrom(t)).ToDictionary(t=>t.Name[2..]);
 static LegacyPowers() { NativeNames["flame_barrier"] = "FlameBarrierPower"; }
 public static PowerModel Get(string key){var type=NativeNames.TryGetValue(key,out var name)?typeof(PowerModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Powers."+name,true)!:CustomTypes[key];return ModelDb.GetById<PowerModel>(ModelDb.GetId(type));}
 public static PowerModel? NativeIconSource(string key)=>NativeNames.ContainsKey(key)?Get(key):null;
}
public abstract class LegacyPower:CustomPowerModel
{
 public string Key=>GetType().Name[2..];
 public override PowerType Type=>PowerType.Buff;
 public override PowerStackType StackType=>PowerStackType.Counter;
 // Choose the same source for the status-bar and tooltip icons. Never silently
 // turn a new draw/defense/utility power into the Strength sword again.
 private PowerModel IconSource=>LegacyPowers.NativeIconSource(Key)??Key switch {
  "metallicize" or "plated"=>ModelDb.Power<PlatingPower>(),
  "skill_block" or "discard_block" or "scry_block" or "status_furnace"=>ModelDb.Power<PlatingPower>(),
  "discard_next_block"=>ModelDb.Power<BlockNextTurnPower>(),
  "berserk" or "skill_energy" or "discard_energy"=>ModelDb.Power<EnergyNextTurnPower>(),
  "brutality" or "power_draw" or "evolve" or "arcane_echo" or "retained_draw" or "quiver"=>ModelDb.Power<DrawCardsNextTurnPower>(),
  "well_laid" or "foresight" or "establishment"=>ModelDb.Power<WellLaidPlansPower>(),
  "equilibrium"=>ModelDb.Power<RetainHandPower>(),
  "prophecy_return" or "discard_return"=>ModelDb.Power<ReboundPower>(),
  "attack_vigor" or "exhaust_vigor" or "hunt_momentum"=>ModelDb.Power<VigorPower>(),
  "double_tap" or "skill_echo" or "thesis_ready"=>ModelDb.Power<EchoFormPower>(),
  "thousand_cuts" or "sadistic" or "exhaust_damage" or "fire_breathing" or "combust" or "exhaust_burst"=>ModelDb.Power<InfernoPower>(),
  "reservoir" or "class"=>ModelDb.Power<FocusPower>(),
  _=>throw new InvalidOperationException($"No power icon assigned for {Key}.")
 };
 public override string CustomPackedIconPath=>IconSource.PackedIconPath;
 public override string CustomBigIconPath=>IconSource.ResolvedBigIconPath;
 public override List<(string,string)> Localization=>[("title",Key=="class"?"{ClassTitle}":Key=="well_laid"?"Measured Breath":Key=="discard_return"?"Ghost Luggage":System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Key.Replace('_',' '))),("description",DescriptionText),("selectionScreenPrompt","Choose up to {Amount} card(s) to Retain this turn.")];
 public override LocString Title {get {var title=base.Title;if(Key=="class")title.Add("ClassTitle",Design.Get("class_mechanics",PlayerBloodlines.Heir(Owner.Player).classId).Text("name"));return title;}}
 private string DescriptionText=>Key switch {
  "class"=>"{ClassRules} {CounterLabel}: {Counter}.",
  "plated"=>"At turn end, gain {Amount} Block. Lose a stack when attacked for HP damage.",
  "reservoir"=>"Each Arcane Charge adds {Amount} extra damage to every hit of your next Attack.",
  "quiver"=>"Hunter Rhythm draws {Amount} additional cards.",
  "exhaust_burst"=>"Whenever you Exhaust 3 cards, deal {Amount} damage to ALL enemies. Progress: {Counter}/3.",
  "skill_echo"=>PowerRules+" {ThesisProgress}",
  "skill_block" or "skill_energy"=>PowerRules+" Skills left: {Counter}.",
  "attack_vigor"=>PowerRules+" Attacks left: {Counter}.",
  _=>PowerRules
 };
 private string PowerRules=>Design.Get("power_effects",Key).Text("summary").Replace("!M!","{Amount}");
 private const int PlayThreshold = 3;
 private bool RepeatsOnPlayThreshold=>Key is "skill_block" or "skill_energy" or "attack_vigor";
 private int plays;private bool discardPaid;private bool cyclingDamage;
 private int charges,attacks;
 [SavedProperty] public int Charges {get=>charges;set{charges=value;if(IsMutable)InvokeDisplayAmountChanged();}}
 [SavedProperty] public int AttacksThisTurn {get=>attacks;set{attacks=value;if(IsMutable)InvokeDisplayAmountChanged();}}
 [SavedProperty] public bool ArcaneDrawUsed {get;set;}
 [SavedProperty] public int SkillsThisTurn {get;set;}
 // Retain the saved field name for compatibility; Phoenix now carries 0–2
 // Exhausts across turns. Legacy capped values are normalized on the next event.
 [SavedProperty] public int ExhaustsThisTurn {get;set;}
 [SavedProperty] public bool RetainedLastTurn {get;set;}
 [SavedProperty] public bool EchoTriggered {get;set;}
 [SavedProperty] public int MomentumAttacks {get;set;}
 [SavedProperty] public int GhostRecoveryTurn {get;set;} = -1;
 public override int DisplayAmount=>Key switch {
  "class"=>PlayerBloodlines.Heir(Owner.Player).classId=="ranger"?PlayThreshold-AttacksThisTurn%PlayThreshold:Charges,
  "skill_echo"=>EchoTriggered?0:PlayThreshold-Math.Clamp(SkillsThisTurn,0,PlayThreshold),
  _=>RepeatsOnPlayThreshold?PlayThreshold-plays:Amount
 };
 public override LocString Description
 {
  get
  {
   var text=base.Description;
   var classId=PlayerBloodlines.Heir(Owner.Player).classId;
   text.Add("Amount",Amount);
   text.Add("ClassRules",Design.Get("classes",classId).Text("summary"));
   text.Add("CounterLabel",classId=="ranger"?"Attacks left":"Current");
   text.Add("Counter",Key=="exhaust_burst"?Math.Max(0,ExhaustsThisTurn)%3:DisplayAmount);
   if(Key=="skill_echo")
    text.Add("ThesisProgress",EchoTriggered?"Triggered this turn.":$"Skills left: {DisplayAmount}.");
   return text;
  }
 }
 public override Task AfterApplied(Creature? applier,CardModel? cardSource)
 {
  // Hunt asks about last turn's Attacks, including those played before this
  // Power in its setup turn. Later plays are tracked by the power itself.
  if(Key=="hunt_momentum"&&Owner.CombatState?.CurrentSide==Owner.Side)MomentumAttacks=Math.Clamp(Owner.GetPower<P_class>()?.AttacksThisTurn??0,0,3);
  return Task.CompletedTask;
 }
 public override async Task AfterPlayerTurnStart(PlayerChoiceContext context,Player player)
 {
  if(player!=Owner.Player!)return;
  var refreshCountdown=RepeatsOnPlayThreshold&&plays!=0 || Key=="skill_echo"&&(SkillsThisTurn!=0||EchoTriggered);
  plays=0;discardPaid=false;AttacksThisTurn=0;ArcaneDrawUsed=false;SkillsThisTurn=0;EchoTriggered=false;
  if(refreshCountdown)InvokeDisplayAmountChanged();
  switch(Key){
   case "discard_return":await GhostLuggage.Recover(this,context,player);break;
   case "retained_draw":var retained=RetainedLastTurn;RetainedLastTurn=false;if(retained)await CardPileCmd.Draw(context,Amount,player);break;
   case "hunt_momentum":var priorAttacks=Math.Clamp(MomentumAttacks,0,3);MomentumAttacks=0;if(priorAttacks>0)await CardEffects.Apply(context,"vigor",Owner,Amount*priorAttacks,Owner,null);break;
   case "brutality":await CreatureCmd.Damage(context,Owner,Amount,ValueProp.Unblockable|ValueProp.Unpowered,Owner);await CardPileCmd.Draw(context,Amount,player);break;
   case "berserk":await PlayerCmd.GainEnergy(Amount,player);break;
   case "foresight":await CardEffects.Scry(player,context,Amount);break;
   case "prophecy_return":
    // Claim before the asynchronous choice. Replays stack the discount, not
    // additional recovery actions, and no live card references survive a run.
    var discount=Amount;await PowerCmd.Remove(this);
    await CardEffects.RecoverFromExhaust(player,context,attacksOnly:false,discount:discount);break;
  }
 }
 public override async Task BeforeSideTurnEnd(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants)
 {
  if(!participants.Contains(Owner))return;
  if(Key is "metallicize" or "plated")await CreatureCmd.GainBlock(Owner,Amount,ValueProp.Unpowered,null);
  if(Key=="combust"){await CreatureCmd.Damage(context,Owner,1,ValueProp.Unblockable|ValueProp.Unpowered,Owner);await DamageAll(context,Amount);}
 }
 public override async Task AfterCardPlayed(PlayerChoiceContext context,CardPlay play)
 {
  var card=play.Card;if(card.Owner.Creature!=Owner)return;
  if(Owner.CombatState?.CurrentSide==Owner.Side){
   if(Key=="hunt_momentum"&&card.Type==CardType.Attack)MomentumAttacks=Math.Min(3,MomentumAttacks+1);
   if(Key=="skill_echo"&&card.Type==CardType.Skill&&!EchoTriggered){
    SkillsThisTurn=Math.Min(PlayThreshold,SkillsThisTurn+1);
    EchoTriggered=SkillsThisTurn==PlayThreshold;
    InvokeDisplayAmountChanged();
    if(EchoTriggered)await CardEffects.Apply(context,"thesis_ready",Owner,Amount,Owner,card);
   }
  }
  if(Key=="class"){
   if(card.Type==CardType.Skill){SkillsThisTurn=Math.Min(3,SkillsThisTurn+1);if(PlayerBloodlines.Heir(Owner.Player).classId=="knight")Charges=1;if(PlayerBloodlines.Heir(Owner.Player).classId=="mage")Charges=Math.Min(2,Charges+1);}
   if(card.Type==CardType.Attack){AttacksThisTurn++;
    var spent=Charges;if(PlayerBloodlines.Heir(Owner.Player).classId is "knight" or "mage")Charges=0;
    // Claim before awaiting: replays and further Attacks cannot draw again this turn.
    if(PlayerBloodlines.Heir(Owner.Player).classId=="mage"&&spent>0&&!ArcaneDrawUsed){ArcaneDrawUsed=true;await CardPileCmd.Draw(context,spent,Owner.Player!);}
    if(PlayerBloodlines.Heir(Owner.Player).classId=="ranger"&&AttacksThisTurn%PlayThreshold==0){
     await CardPileCmd.Draw(context,1+FindAmount("quiver"),Owner.Player!);
     var opponents=Owner.CombatState!.GetOpponentsOf(Owner).Where(e=>e.IsAlive).ToArray();
     var random=card is LegacyCard legacy&&legacy.DesignCard.Row.Text("special")=="random_attack";
     IEnumerable<Creature> targets=random&&opponents.Length>0?new[]{opponents[Owner.Player!.RunState.Rng.CombatTargets.NextInt(opponents.Length)]}:card.TargetType==TargetType.AllEnemies?opponents:play.Target is {} target?new[]{target}:[];
     foreach(var enemy in targets.Where(e=>e.IsAlive))await CardEffects.Apply(context,"vulnerable",enemy,1,Owner,card);}}
  }
  if(Key=="power_draw"&&card.Type==CardType.Power)await CardPileCmd.Draw(context,Amount,Owner.Player!);
  if(Key=="arcane_echo"&&card.Type==CardType.Skill&&++plays==1){if(Owner.GetPower<P_class>() is {} charge&&PlayerBloodlines.Heir(Owner.Player).classId=="mage")charge.Charges=Math.Min(2,charge.Charges+1);await CardPileCmd.Draw(context,Amount,Owner.Player!);}
  if(Key=="thousand_cuts")await DamageAll(context,Amount);
  if((Key is "skill_block" or "skill_energy"&&card.Type==CardType.Skill)||(Key=="attack_vigor"&&card.Type==CardType.Attack)){
   var triggered=++plays==PlayThreshold;
   if(triggered)plays=0;
   InvokeDisplayAmountChanged();
   if(triggered){
    if(Key=="skill_block")await CreatureCmd.GainBlock(Owner,Amount,ValueProp.Unpowered,null);
    if(Key=="skill_energy")await CardEffects.Apply(context,"nextEnergy",Owner,Amount,Owner,card);
    if(Key=="attack_vigor")await CardEffects.Apply(context,"vigor",Owner,Amount,Owner,card);
   }
  }
 }
 public decimal InheritedDamageAdditive(Creature? target,decimal amount,ValueProp props,Creature? dealer,CardModel? card)
 {
  // Like native additive attack bonuses, apply to every powered hit. Charges
  // remain available for the whole card and are consumed in AfterCardPlayed.
  if(Key!="class"||dealer!=Owner||card?.Type!=CardType.Attack||!props.IsPoweredAttack()||target is null)return 0;
  return PlayerBloodlines.Heir(Owner.Player).classId switch{"knight"=>Charges>0?2:0,"mage"=>Charges*FindAmount("reservoir"),_=>0};
 }
 private int FindAmount(string key)=>Owner.Powers.OfType<LegacyPower>().Where(p=>p.Key==key).Sum(p=>p.Amount);
 public override async Task AfterCardDiscarded(PlayerChoiceContext context,CardModel card)
 {
  if(card.Owner.Creature!=Owner||Owner.Player?.PlayerCombatState?.Phase==PlayerTurnPhase.End || Owner.CombatState?.CurrentSide!=Owner.Side)return;
  if(Key=="discard_block")await CreatureCmd.GainBlock(Owner,Amount,ValueProp.Unpowered,null);
  if(Key=="discard_next_block")await CardEffects.Apply(context,"nextBlock",Owner,Amount,Owner,null);
  if(Key=="discard_energy"&&!discardPaid){discardPaid=true;await PlayerCmd.GainEnergy(Amount,Owner.Player!);}
 }
 public override async Task AfterCardExhausted(PlayerChoiceContext context,CardModel card,bool causedByEthereal){
  if(card.Owner.Creature!=Owner)return;
  if(Key=="exhaust_damage")await DamageAll(context,Amount);
  if(Key=="exhaust_vigor")await CardEffects.Apply(context,"vigor",Owner,Amount,Owner,null);
  if(Key=="exhaust_burst"){
   ExhaustsThisTurn=Math.Max(0,ExhaustsThisTurn)%3+1;
   if(ExhaustsThisTurn==3){ExhaustsThisTurn=0;await DamageAll(context,Amount);}
  }
 }
 public override async Task AfterCardDrawn(PlayerChoiceContext context,CardModel card,bool fromHandDraw)
 {
  if(card.Owner.Creature!=Owner)return;
  if(Key=="evolve"&&card.Type==CardType.Status)await CardPileCmd.Draw(context,Amount,Owner.Player!);
  if(Key=="fire_breathing"&&card.Type is CardType.Status or CardType.Curse)await DamageAll(context,Amount);
  if(Key=="status_furnace"&&card.Type==CardType.Status&&card.Pile?.Type==PileType.Hand){
   await BranchCommands.Exhaust(context,card);
   await CreatureCmd.GainBlock(Owner,Amount,ValueProp.Unpowered,null);
  }
 }
 public override async Task AfterFlush(PlayerChoiceContext context,Player player,IReadOnlyCollection<CardModel> flushed,IReadOnlyCollection<CardModel> retained)
 {
  if(player!=Owner.Player!)return;
  if(Key=="retained_draw")RetainedLastTurn=retained.Count>0;
  if(Key=="establishment")foreach(var card in retained)if(!card.EnergyCost.CostsX)card.EnergyCost.SetThisCombat(Math.Max(0,card.EnergyCost.GetWithModifiers(CostModifiers.Local)-Amount));
 }
 public override async Task BeforeFlushLate(PlayerChoiceContext context,Player player)
 {
  if(Key!="well_laid"||player!=Owner.Player||player.Creature.CombatState is not {} combat||!Hook.ShouldFlush(combat,player))return;
  // Beta's native Well-Laid Plans now keeps the whole hand. Measured Breath
  // instead offers a fresh bounded choice each turn, without permanent keywords.
  var chosen=await CardSelectCmd.FromHand(context,player,new CardSelectorPrefs(SelectionScreenPrompt,0,Amount),c=>!c.ShouldRetainThisTurn,this);
  foreach(var card in chosen)card.GiveSingleTurnRetain();
 }
 public override bool ShouldFlush(Player player)=>Key!="equilibrium"||player!=Owner.Player;
 public override async Task AfterSideTurnEnd(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants){if(!participants.Contains(Owner))return;if(Key=="equilibrium")await PowerCmd.Decrement(this);else if(Key is "double_tap" or "thesis_ready")await PowerCmd.Remove(this);}
 public override int ModifyCardPlayCount(CardModel card,Creature? target,int count)=>card.Owner.Creature==Owner&&card.Type==CardType.Attack?Key switch{"double_tap"=>count+1,"thesis_ready"=>count+Amount,_=>count}:count;
 public override async Task AfterModifyingCardPlayCount(CardModel card){if(Key=="double_tap")await PowerCmd.Decrement(this);else if(Key=="thesis_ready")await PowerCmd.Remove(this);}
 public override async Task AfterPowerAmountChanged(PlayerChoiceContext context,PowerModel power,decimal amount,Creature? applier,CardModel? card)
 {
  if(Key=="sadistic"&&power.Owner!=Owner&&power.Owner.IsAlive&&power.Type==PowerType.Debuff&&applier==Owner&&amount>0&&!cyclingDamage){cyclingDamage=true;try{await CreatureCmd.Damage(context,power.Owner,Amount,ValueProp.Unpowered,Owner);}finally{cyclingDamage=false;}}
 }
 public override async Task AfterDamageReceived(PlayerChoiceContext context,Creature target,DamageResult result,ValueProp props,Creature? dealer,CardModel? card){if(Key=="plated"&&target==Owner&&dealer!=Owner&&result.UnblockedDamage>0&&props.IsPoweredAttack())await PowerCmd.Decrement(this);}
 private async Task DamageAll(PlayerChoiceContext context,int amount){var opponents=Owner.CombatState?.GetOpponentsOf(Owner).Where(e=>e.IsAlive).ToArray()??[];if(opponents.Length>0)await CreatureCmd.Damage(context,opponents,amount,ValueProp.Unpowered,Owner);}
}
