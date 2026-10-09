using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
namespace SpireLegacy;
public static class CardEffects
{
 public static async Task Apply(PlayerChoiceContext context,string key,Creature target,int amount,Creature source,CardModel? card)
 {
  if(amount==0)return;
  var canonical=LegacyPowers.Get(key);var power=PowerCmd.FindExistingInstanceForStacking(canonical,target,source);
  if(power is null)await PowerCmd.Apply(context,canonical.ToMutable(),target,amount,source,card);
  else await PowerCmd.ModifyAmount(context,power,amount,source,card);
 }
 public static async Task Execute(LegacyCard card,PlayerChoiceContext context,CardPlay play)
 {
  var d=card.DesignCard;var row=d.Row;var player=card.Owner;var creature=player.Creature;
  IEnumerable<Creature> enemies=row.Flag("aoe")?card.CombatState!.GetOpponentsOf(creature):play.Target is {} target?new[]{target}:[];
  if(d.Amount("hpLoss")>0)await CreatureCmd.Damage(context,creature,d.Amount("hpLoss"),ValueProp.Unblockable|ValueProp.Unpowered,creature,card);
  if(d.Amount("heal")+d.Gene("heal")>0)await CreatureCmd.Heal(creature,d.Amount("heal")+d.Gene("heal"));
  foreach(var key in new[]{"vigor","thorns","plated","nextEnergy","nextDraw","nextBlock"})await Apply(context,key,creature,d.Amount(key),creature,card);
  if(row.Text("special")=="scry")await Scry(card,context,d.SpecialAmount);
  if(d.Amount("draw")>0)await CardPileCmd.Draw(context,d.Amount("draw"),player);
  if(d.Amount("discard")>0)await CardCmd.Discard(context,await CardSelectCmd.FromHandForDiscard(context,player,new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt,d.Amount("discard")),null,card));
  if(d.Amount("energy")>0)await PlayerCmd.GainEnergy(d.Amount("energy"),player);
  await Apply(context,"strength",creature,d.Amount("strength"),creature,card);await Apply(context,"dexterity",creature,d.Amount("dexterity"),creature,card);
  foreach(var enemy in enemies.Where(e=>e.IsAlive)){
   foreach(var key in new[]{"poison","weak","vulnerable"})await Apply(context,key,enemy,d.Amount(key),creature,card);
   if(d.Mercy)await Apply(context,"poison",enemy,d.Gene("poison")+(card.IsUpgraded?1:0),creature,card);
  }
  if(row.Text("power")!="none")await Apply(context,row.Text("power"),creature,d.Magic,creature,card);
  await Special(card,context,enemies);
 }
 public static async Task Scry(CardModel card,PlayerChoiceContext context,int amount)
 {
  var top=PileType.Draw.GetPile(card.Owner).Cards.Take(amount).ToArray();
  if(top.Length>0){var chosen=await CardSelectCmd.FromSimpleGrid(context,top,card.Owner,new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt,0,top.Length));foreach(var selected in chosen)await CardPileCmd.Add(selected,PileType.Discard);}
  foreach(var engine in card.Owner.Creature.Powers.OfType<LegacyPower>().Where(p=>p.Key=="scry_block"))await CreatureCmd.GainBlock(card.Owner.Creature,engine.Amount,ValueProp.Unpowered,null);
 }
 private static async Task Special(LegacyCard card,PlayerChoiceContext context,IEnumerable<Creature> enemies)
 {
  var row=card.DesignCard.Row;var amount=card.DesignCard.SpecialAmount;var player=card.Owner;var creature=player.Creature;var hand=PileType.Hand.GetPile(player);
  switch(row.Text("special")){
   case "none":case "block_damage":case "chain_damage":case "random_attack":case "execute":case "guard_bonus":case "marked_bonus":case "scry":case "exhaust_damage":case "retain_damage":case "retain_block":case "discard_draw":case "discard_energy":case "discard_block":return;
   case "wounds":for(var i=0;i<amount;i++)await CardPileCmd.AddGeneratedCardToCombat(card.CombatState!.CreateCard(ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Wound>(),player),PileType.Draw,player,CardPilePosition.Random);break;
   case "refill_hand":var count=hand.Cards.Count;await CardCmd.Discard(context,hand.Cards.ToArray());await CardPileCmd.Draw(context,count,player);break;
   case "exhaust_block":await CreatureCmd.GainBlock(creature,Math.Min(10,PileType.Exhaust.GetPile(player).Cards.Count)*amount,ValueProp.Unpowered,null);break;
   case "exhaust_nonattacks":foreach(var fuel in hand.Cards.Where(c=>c.Type!=CardType.Attack).ToArray()){await CardCmd.Exhaust(context,fuel);await CreatureCmd.GainBlock(creature,amount,ValueProp.Unpowered,null);}break;
   case "double_block":await CreatureCmd.GainBlock(creature,creature.Block,ValueProp.Unpowered,null);break;
   case "cleanse":foreach(var power in creature.Powers.Where(p=>p.Type==PowerType.Debuff).ToArray())await PowerCmd.Remove(power);break;
   case "exhaust_one":foreach(var fuel in await CardSelectCmd.FromHand(context,player,new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt,amount),null,card))await CardCmd.Exhaust(context,fuel);break;
   case "retrieve":case "exhume":var pile=row.Text("special")=="retrieve"?PileType.Discard:PileType.Exhaust;var selected=await CardSelectCmd.FromCombatPile(context,pile.GetPile(player),player,new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString("cards",card.Id.Entry+".returnPrompt"),amount));foreach(var retrieved in selected)await CardPileCmd.Add(retrieved,PileType.Hand);break;
   case "charge_gain":if(creature.GetPower<P_class>() is {} cls && Runtime.Heir.classId=="mage")cls.Charges=Math.Min(2,cls.Charges+amount);break;
   case "discount_hand":var eligible=hand.Cards.Where(c=>!c.EnergyCost.CostsX&&c.EnergyCost.GetWithModifiers(CostModifiers.All)>0).ToList();for(var i=0;i<amount&&eligible.Count>0;i++){var index=player.RunState.Rng.CombatCardSelection.NextInt(eligible.Count);var discounted=eligible[index];eligible.RemoveAt(index);discounted.EnergyCost.AddThisTurn(-1);}break;
   case "free_hand":foreach(var held in hand.Cards.Where(c=>!c.EnergyCost.CostsX))held.SetToFreeThisTurn();await Apply(context,"no_draw",creature,1,creature,card);break;
   case "double_poison":foreach(var enemy in enemies.Where(e=>e.IsAlive))await Apply(context,"poison",enemy,enemy.GetPowerAmount<PoisonPower>(),creature,card);break;
   default:throw new InvalidDataException("Unmapped StS2 card effect: "+row.Text("special"));
  }
 }
}
