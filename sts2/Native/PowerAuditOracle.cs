using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Opt-in disposable native fixture. Observe real hooks and paid plays, not inferred previews.
public static class PowerAuditOracle
{
    private static readonly PlayerChoiceContext Context=new BlockingPlayerChoiceContext();
    private static CardModel? observed;
    private static readonly List<(Creature target,int damage)> hits=[];
    private static readonly PileType[] Piles=[PileType.Draw,PileType.Hand,PileType.Discard,PileType.Exhaust,PileType.Play];
    private static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
    private static async Task Wait(Func<bool> done)
    {var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+20000;while(!done()){if(Time.GetTicksMsec()>until)throw new TimeoutException("Power audit native action.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}}
    public static void Observe(object[] __args)
    {if(observed!=null&&__args.OfType<CardModel>().Any(c=>c==observed)){var result=__args.OfType<DamageResult>().Single();if(result.Props.IsPoweredAttack())hits.Add((result.Receiver,result.TotalDamage));}}
    private static async Task<Creature[]> Setup(Player player,bool withClass=true)
    {
        var combat=player.Creature.CombatState!;
        await CardPileCmd.RemoveFromCombat(CardPile.GetCards(player,Piles).ToArray(),skipVisuals:true);
        foreach(var creature in combat.GetOpponentsOf(player.Creature).Append(player.Creature)) {
            foreach(var power in creature.Powers.ToArray())await PowerCmd.Remove(power);
            creature.LoseBlockInternal(creature.Block);
            if(!creature.IsPlayer){creature.SetMaxHpInternal(10000);creature.SetCurrentHpInternal(10000);}
        }
        if(withClass)await Apply(player,"class",1);
        await PlayerCmd.GainEnergy(30,player);
        for(var i=0;i<40;i++)await Add(player,ModelDb.Card<DefendIronclad>(),false,PileType.Draw);
        return combat.GetOpponentsOf(player.Creature).Where(e=>e.IsAlive).ToArray();
    }
    private static Task Apply(Player player,string key,int amount)=>CardEffects.Apply(Context,key,player.Creature,amount,player.Creature,null);
    private static async Task<CardModel> Add(Player player,CardModel canonical,bool up=false,PileType pile=PileType.Hand)
    {var card=player.Creature.CombatState!.CreateCard(canonical,player);if(up)CardCmd.Upgrade(card);await CardPileCmd.Add(card,pile,skipVisuals:true);return card;}
    private static async Task Play(CardModel card,Creature? target=null)
    {
        var failure=CardPlayDiagnostics.LastFailure;
        var energy=card.Owner.PlayerCombatState!.Energy;var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);
        Require(card.TryManualPlay(card.TargetType==TargetType.AnyEnemy?target:null),"Power probe paid play refused: "+card.Id);
        await Wait(()=>card.Pile?.Type is not (PileType.Hand or PileType.Play)&&NCard.FindOnTable(card)==null&&!RunManager.Instance.ActionExecutor.IsRunning);
        Require(CardPlayDiagnostics.LastFailure==failure,"Power action diagnostic changed.");
        Require(card.Owner.PlayerCombatState.Energy==energy-cost,"Power fixture payment differs.");
    }
    public static async Task<object> Class(RunState run,string mode,bool up,IDisposable selector)
    {
        const string patch="CapeKid.SpireLegacy2.PowerAudit";var harmony=new Harmony(patch);
        var player=run.Players.Single();var cls=PlayerBloodlines.Heir(player).classId;
        using(selector)try {
            var enemies=await Setup(player);Require(enemies.Length>0,"Class fixture needs targets.");
            harmony.Patch(typeof(Hook).GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name=="AfterDamageGiven"),prefix:new HarmonyMethod(typeof(PowerAuditOracle),nameof(Observe)));
            var counter=player.Creature.GetPower<P_class>()!;
            if(mode=="reservoir"){await Apply(player,"reservoir",2);await Apply(player,"arcane_echo",2);}
            var charges=new List<int>();
            for(var i=0;i<4;i++) {
                await Play(await Add(player,ModelDb.Card<DefendIronclad>(),up));charges.Add(counter.Charges);
                var expected=cls=="knight"?1:cls=="mage"?Math.Min(2,i+1+(mode=="reservoir"?1:0)):0;
                Require(counter.Charges==expected,"Class Skill cap/echo mismatch: "+cls);
            }
            var saved=SavedProperties.From(counter)!;
            var json=JsonSerializer.Serialize(saved,new JsonSerializerOptions{IncludeFields=true});
            var loaded=(P_class)ModelDb.Power<P_class>().ToMutable();JsonSerializer.Deserialize<SavedProperties>(json,new JsonSerializerOptions{IncludeFields=true})!.Fill(loaded);
            Require(loaded.Charges==counter.Charges&&loaded.AttacksThisTurn==counter.AttacksThisTurn,"Saved class properties differ.");
            var clone=(P_class)counter.MutableClone();clone.Charges=0;
            var field=AccessTools.Field(typeof(LegacyPower),"classBonusTargets");
            Require(!ReferenceEquals(field.GetValue(counter),field.GetValue(clone))&&counter.Charges==charges.Last(),"Power clone shares counters/target set.");
            var canonical=mode switch {
                "area"=>ModelDb.Card<Thunderclap>(),
                "random"=>NativeCards.Get(Design.Rows("cards").First(r=>r.Text("special")=="random_attack"&&r.Text("power")=="none").Text("id")),
                "single" or "zero" or "lethal"=>ModelDb.Card<StrikeIronclad>(),
                _=>ModelDb.Card<TwinStrike>() as CardModel
            };
            var card=await Add(player,canonical,up);
            if(mode=="zero") {counter.Charges=0;card.DynamicVars.Damage.BaseValue=0;}
            if(mode=="dupe") {
                var method=AccessTools.Method(typeof(CardModel),"CreateDupe");
                card=(CardModel)method.Invoke(card,method.GetParameters().Select(p=>p.ParameterType==typeof(Player)?player:p.HasDefaultValue?p.DefaultValue:throw new MissingMethodException("Dupe parameter.")).ToArray())!;
                await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            }
            if(mode=="replay")await Apply(player,"double_tap",1);
            if(mode=="lethal")enemies[0].SetCurrentHpInternal(1);
            var primed=counter.Charges;var baseDamage=card.DynamicVars.Damage.BaseValue;
            observed=card;hits.Clear();await Play(card,enemies[0]);
            var bonus=cls=="knight"&&primed>0?2:cls=="mage"?primed*(1+(mode=="reservoir"?2:0)):0;
            var seen=new HashSet<Creature>();
            foreach(var hit in hits){var extra=seen.Add(hit.target)?bonus:0;Require(hit.damage==baseDamage+extra,$"Class first-hit damage {cls}/{mode}: {hit.damage}, expected {baseDamage+extra}");}
            Require(hits.Count>0,"No observed powered hits.");
            var plays=mode=="replay"?2:1;
            if(mode!="lethal")Require(counter.AttacksThisTurn==plays&&counter.Charges==0,"Class Attack count/consumption mismatch.");
            return new{cls,mode,up,charges,primed,bonus,hits=hits.Select(h=>h.damage).ToArray(),distinctTargets=seen.Count,attacks=counter.AttacksThisTurn,counter.Charges,cloneIndependent=true,savedProperties=true,pile=card.Pile?.Type.ToString()};
        }finally{observed=null;NativeStartup.Remove(patch);}
    }
    public static async Task<object> Rhythm(RunState run,bool up,string mode,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var enemies=await Setup(player);var counter=player.Creature.GetPower<P_class>()!;
            await Apply(player,"quiver",2);var counts=new List<int>();var draws=new List<int>();
            for(var i=1;i<=6;i++) {
                var canonical=mode=="area"?ModelDb.Card<Thunderclap>() as CardModel:mode=="random"?NativeCards.Get(Design.Rows("cards").First(r=>r.Text("special")=="random_attack").Text("id")):ModelDb.Card<TwinStrike>();
                var card=await Add(player,canonical,up);var before=player.PlayerCombatState!.Hand.Cards.Count;
                await Play(card,enemies[0]);var drawn=player.PlayerCombatState.Hand.Cards.Count-before+1;
                Require(counter.AttacksThisTurn==i&&counter.DisplayAmount==i%3&&drawn==(i%3==0?3:0),"Hunter Rhythm per-card/draw cap mismatch.");
                counts.Add(counter.DisplayAmount);draws.Add(drawn);
            }
            var marks=enemies.Select(e=>e.GetPowerAmount<VulnerablePower>()).ToArray();
            Require(marks.Sum()==(mode=="area"?enemies.Length*2:2),"Hunter Rhythm target selection mismatch.");
            await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
            Require(counter.AttacksThisTurn==0&&counter.DisplayAmount==0,"Hunter Rhythm did not reset next turn.");
            return new{mode,up,counts,draws,marks,nextTurnReset=true};
        }
    }
    public static async Task<object> Barrier(RunState run,bool up,bool native)
    {
        var player=run.Players.Single();var enemies=await Setup(player,false);var enemy=enemies[0];
        var card=await Add(player,native?ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.FlameBarrier>():NativeCards.Get("flame"),up);
        await Play(card);
        var amount=player.Creature.GetPowerAmount<FlameBarrierPower>();Require(amount>0,"Flame Barrier did not use native retaliation power.");
        var results=new List<int>();
        foreach(var props in new[]{ValueProp.Move,ValueProp.Move,ValueProp.Unpowered,ValueProp.Unblockable|ValueProp.Unpowered}) {
            var hp=enemy.CurrentHp;await CreatureCmd.GainBlock(player.Creature,100,ValueProp.Unpowered,null);
            await CreatureCmd.Damage(Context,player.Creature,2,props,enemy);var delta=hp-enemy.CurrentHp;
            Require(delta==(props.IsPoweredAttack()?amount:0),"Flame Barrier flags/blocked retaliation mismatch.");results.Add(delta);
        }
        await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
        Require(player.Creature.GetPower<FlameBarrierPower>()==null,"Flame Barrier outlived enemy turn.");
        return new{native,up,amount,results,nextTurnRemoved=true};
    }
}
