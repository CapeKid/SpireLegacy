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
namespace SpireLegacy;
public static class LegacyPowers
{
 private static readonly Dictionary<string,string> NativeNames=new(){
  ["strength"]="StrengthPower",["dexterity"]="DexterityPower",["poison"]="PoisonPower",["weak"]="WeakPower",["vulnerable"]="VulnerablePower",["vigor"]="VigorPower",["thorns"]="ThornsPower",["nextEnergy"]="EnergyNextTurnPower",["nextDraw"]="DrawCardsNextTurnPower",["nextBlock"]="BlockNextTurnPower",["no_draw"]="NoDrawPower",
  ["juggernaut"]="JuggernautPower",["rupture"]="RupturePower",["feel_no_pain"]="FeelNoPainPower",["barricade"]="BarricadePower",["demon_form"]="DemonFormPower",["dark_embrace"]="DarkEmbracePower",["buffer"]="BufferPower",["panache"]="PanachePower",["tools"]="ToolsOfTheTradePower",["after_image"]="AfterimagePower",["well_laid"]="WellLaidPlansPower",["envenom"]="EnvenomPower",["burst"]="BurstPower",["noxious"]="NoxiousFumesPower"};
 private static readonly Dictionary<string,Type> CustomTypes=typeof(LegacyPower).Assembly.GetTypes().Where(t=>!t.IsAbstract&&typeof(LegacyPower).IsAssignableFrom(t)).ToDictionary(t=>t.Name[2..]);
 public static PowerModel Get(string key){var type=NativeNames.TryGetValue(key,out var name)?typeof(PowerModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Powers."+name,true)!:CustomTypes[key];return ModelDb.GetById<PowerModel>(ModelDb.GetId(type));}
}
public abstract class LegacyPower:CustomPowerModel
{
 public string Key=>GetType().Name[2..];
 public override PowerType Type=>PowerType.Buff;
 public override PowerStackType StackType=>PowerStackType.Counter;
 public override string CustomPackedIconPath=>ModelDb.Power<StrengthPower>().PackedIconPath;
 public override string CustomBigIconPath=>ModelDb.Power<StrengthPower>().ResolvedBigIconPath;
 public override List<(string,string)> Localization=>[("title",Key=="class"?"{ClassTitle}":System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Key.Replace('_',' '))),("description",DescriptionText)];
 public override LocString Title {get {var title=base.Title;if(Key=="class")title.Add("ClassTitle",Design.Get("class_mechanics",Runtime.Heir.classId).Text("name"));return title;}}
 private string DescriptionText=>Key switch{"class"=>"{ClassRules} Current: {Counter}.","plated"=>"At turn end, gain {Amount} Block. Lose a stack when attacked for HP damage.","reservoir"=>"Each Arcane Charge adds {Amount} extra attack damage.","quiver"=>"Hunter Rhythm draws {Amount} additional cards.",_=>Design.Get("power_effects",Key).Text("summary").Replace("!M!","{Amount}")};
 private int plays;private bool discardPaid;private bool cyclingDamage;
 private int charges,attacks;
 [SavedProperty] public int Charges {get=>charges;set{charges=value;if(IsMutable)InvokeDisplayAmountChanged();}}
 [SavedProperty] public int AttacksThisTurn {get=>attacks;set{attacks=value;if(IsMutable)InvokeDisplayAmountChanged();}}
 public override int DisplayAmount=>Key=="class"?(Runtime.Heir.classId=="ranger"?AttacksThisTurn%3:Charges):Amount;
 public override LocString Description {get{var text=base.Description;text.Add("Amount",Amount);text.Add("ClassRules",Design.Get("classes",Runtime.Heir.classId).Text("summary"));text.Add("Counter",DisplayAmount);return text;}}
 public override async Task AfterPlayerTurnStart(PlayerChoiceContext context,Player player)
 {
  if(player!=Owner.Player!)return;plays=0;discardPaid=false;AttacksThisTurn=0;
  switch(Key){
   case "brutality":await CreatureCmd.Damage(context,Owner,Amount,ValueProp.Unblockable|ValueProp.Unpowered,Owner);await CardPileCmd.Draw(context,Amount,player);break;
   case "berserk":await PlayerCmd.GainEnergy(Amount,player);break;
   case "foresight":await CardEffects.Scry(player.Deck.Cards.First(),context,Amount);break;
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
  if(Key=="class"){
   if(card.Type==CardType.Skill){if(Runtime.Heir.classId=="knight")Charges=1;if(Runtime.Heir.classId=="mage")Charges=Math.Min(2,Charges+1);}
   if(card.Type==CardType.Attack){AttacksThisTurn++;if(Runtime.Heir.classId is "knight" or "mage")Charges=0;
    if(Runtime.Heir.classId=="ranger"&&AttacksThisTurn%3==0){
     await CardPileCmd.Draw(context,1+FindAmount("quiver"),Owner.Player!);
     var opponents=Owner.CombatState!.GetOpponentsOf(Owner).Where(e=>e.IsAlive).ToArray();
     var random=card is LegacyCard legacy&&legacy.DesignCard.Row.Text("special")=="random_attack";
     IEnumerable<Creature> targets=random&&opponents.Length>0?new[]{opponents[Owner.Player!.RunState.Rng.CombatTargets.NextInt(opponents.Length)]}:card.TargetType==TargetType.AllEnemies?opponents:play.Target is {} target?new[]{target}:[];
     foreach(var enemy in targets.Where(e=>e.IsAlive))await CardEffects.Apply(context,"vulnerable",enemy,1,Owner,card);}}
  }
  if(Key=="power_draw"&&card.Type==CardType.Power)await CardPileCmd.Draw(context,Amount,Owner.Player!);
  if(Key=="thousand_cuts")await DamageAll(context,Amount);
  if((Key is "skill_block" or "skill_energy"&&card.Type==CardType.Skill)||(Key=="attack_vigor"&&card.Type==CardType.Attack)){
   if(++plays==3){plays=0;if(Key=="skill_block")await CreatureCmd.GainBlock(Owner,Amount,ValueProp.Unpowered,null);if(Key=="skill_energy")await CardEffects.Apply(context,"nextEnergy",Owner,Amount,Owner,card);if(Key=="attack_vigor")await CardEffects.Apply(context,"vigor",Owner,Amount,Owner,card);}}
 }
 public decimal InheritedDamageAdditive(Creature? target,decimal amount,ValueProp props,Creature? dealer,CardModel? card)
 {
  if(Key!="class"||dealer!=Owner||!props.IsPoweredAttack())return 0;
  return Runtime.Heir.classId switch{"knight"=>Charges>0?2:0,"mage"=>Charges*(1+FindAmount("reservoir")),_=>0};
 }
 private int FindAmount(string key)=>Owner.Powers.OfType<LegacyPower>().Where(p=>p.Key==key).Sum(p=>p.Amount);
 public override async Task AfterCardDiscarded(PlayerChoiceContext context,CardModel card)
 {
  if(card.Owner.Creature!=Owner||Owner.Player?.PlayerCombatState?.Phase==PlayerTurnPhase.End || Owner.CombatState?.CurrentSide!=Owner.Side)return;
  if(Key=="discard_block")await CreatureCmd.GainBlock(Owner,Amount,ValueProp.Unpowered,null);
  if(Key=="discard_energy"&&!discardPaid){discardPaid=true;await PlayerCmd.GainEnergy(Amount,Owner.Player!);}
 }
 public override async Task AfterCardExhausted(PlayerChoiceContext context,CardModel card,bool causedByEthereal){if(card.Owner.Creature==Owner&&Key=="exhaust_damage")await DamageAll(context,Amount);}
 public override async Task AfterCardDrawn(PlayerChoiceContext context,CardModel card,bool fromHandDraw)
 {
  if(card.Owner.Creature!=Owner)return;
  if(Key=="evolve"&&card.Type==CardType.Status)await CardPileCmd.Draw(context,Amount,Owner.Player!);
  if(Key=="fire_breathing"&&card.Type is CardType.Status or CardType.Curse)await DamageAll(context,Amount);
 }
 public override async Task AfterFlush(PlayerChoiceContext context,Player player,IReadOnlyCollection<CardModel> flushed,IReadOnlyCollection<CardModel> retained)
 {
  if(player!=Owner.Player!)return;
  if(Key=="establishment")foreach(var card in retained)if(!card.EnergyCost.CostsX)card.EnergyCost.SetThisCombat(Math.Max(0,card.EnergyCost.GetWithModifiers(CostModifiers.Local)-Amount));
 }
 public override bool ShouldFlush(Player player)=>Key!="equilibrium"||player!=Owner.Player;
 public override async Task AfterSideTurnEnd(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants){if(Key is "equilibrium" or "double_tap"&&participants.Contains(Owner))await PowerCmd.Remove(this);}
 public override int ModifyCardPlayCount(CardModel card,Creature? target,int count)=>Key=="double_tap"&&card.Owner.Creature==Owner&&card.Type==CardType.Attack?count+1:count;
 public override async Task AfterModifyingCardPlayCount(CardModel card){if(Key=="double_tap")await PowerCmd.Decrement(this);}
 public override async Task AfterPowerAmountChanged(PlayerChoiceContext context,PowerModel power,decimal amount,Creature? applier,CardModel? card)
 {
  if(Key=="sadistic"&&power.Owner!=Owner&&power.Owner.IsAlive&&power.Type==PowerType.Debuff&&applier==Owner&&amount>0&&!cyclingDamage){cyclingDamage=true;try{await CreatureCmd.Damage(context,power.Owner,Amount,ValueProp.Unpowered,Owner);}finally{cyclingDamage=false;}}
 }
 public override async Task AfterDamageReceived(PlayerChoiceContext context,Creature target,DamageResult result,ValueProp props,Creature? dealer,CardModel? card){if(Key=="plated"&&target==Owner&&dealer!=Owner&&result.UnblockedDamage>0&&props.IsPoweredAttack())await PowerCmd.Decrement(this);}
 private async Task DamageAll(PlayerChoiceContext context,int amount){var opponents=Owner.CombatState?.GetOpponentsOf(Owner).Where(e=>e.IsAlive).ToArray()??[];if(opponents.Length>0)await CreatureCmd.Damage(context,opponents,amount,ValueProp.Unpowered,Owner);}
}
