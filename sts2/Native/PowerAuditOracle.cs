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
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
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
    private static async Task Play(CardModel card,Creature? target=null,int energyGain=0)
    {
        var failure=CardPlayDiagnostics.LastFailure;
        var energy=card.Owner.PlayerCombatState!.Energy;var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);
        Require(card.TryManualPlay(card.TargetType==TargetType.AnyEnemy?target:null),"Power probe paid play refused: "+card.Id);
        await Wait(()=>card.Pile?.Type is not (PileType.Hand or PileType.Play)&&NCard.FindOnTable(card)==null&&!RunManager.Instance.ActionExecutor.IsRunning);
        Require(CardPlayDiagnostics.LastFailure==failure,"Power action diagnostic changed.");
        Require(card.Owner.PlayerCombatState.Energy==energy-cost+energyGain,"Power fixture payment differs.");
    }
    public static async Task<object> Class(RunState run,string mode,bool up,IDisposable selector)
    {
        var firstHitBaseline=mode.StartsWith("baseline-");if(firstHitBaseline)mode=mode[9..];
        const string patch="CapeKid.SpireLegacy2.PowerAudit";var harmony=new Harmony(patch);
        var player=run.Players.Single();var cls=PlayerBloodlines.Heir(player).classId;
        using(selector)try {
            var initial=player.Creature.GetPower<P_class>();Require(initial is {Charges:0,AttacksThisTurn:0},"New combat inherited old class counters.");
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
            // Regular does not cache power fields: run saves contain no live powers.
            // Register this test model explicitly for the separate property-codec probe.
            BaseLib.Utils.BetaMainCompatibility.CacheSavedProperties(typeof(P_class));
            var saved=SavedProperties.From(counter)??new SavedProperties();
            var json=JsonSerializer.Serialize(saved,new JsonSerializerOptions{IncludeFields=true});
            var loaded=(P_class)ModelDb.Power<P_class>().ToMutable();JsonSerializer.Deserialize<SavedProperties>(json,new JsonSerializerOptions{IncludeFields=true})!.Fill(loaded);
            Require(loaded.Charges==counter.Charges&&loaded.AttacksThisTurn==counter.AttacksThisTurn&&loaded.ArcaneDrawUsed==counter.ArcaneDrawUsed,"Saved class properties differ.");
            var clone=(P_class)counter.MutableClone();clone.Charges=0;
            Require(counter.Charges==charges.Last(),"Power clone shares counters.");
            await SaveManager.Instance.SaveRun(null);
            var canonical=mode switch {
                "area"=>ModelDb.Card<Thunderclap>(),
                "random"=>NativeCards.Get(Design.Rows("cards").First(r=>r.Text("special")=="random_attack"&&r.Text("power")=="none").Text("id")),
                "single" or "zero" or "lethal"=>ModelDb.Card<StrikeIronclad>(),
                "legacy-multi"=>NativeCards.Get(Design.Rows("cards").First(r=>r.Text("classId")==cls&&r.Number("hits")>1&&r.Text("special")=="none"&&r.Text("power")=="none").Text("id")),
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
            if(mode is "lethal" or "lethal-chain")enemies[0].SetCurrentHpInternal(1);
            var primed=counter.Charges;var baseDamage=card.DynamicVars.Damage.BaseValue;
            observed=card;hits.Clear();await Play(card,enemies[0]);
            var bonus=cls=="knight"&&primed>0?2:cls=="mage"?primed*(mode=="reservoir"?2:0):0;
            var seen=new HashSet<Creature>();
            for(var i=0;i<hits.Count;i++){var hit=hits[i];var fresh=seen.Add(hit.target);var extra=firstHitBaseline?(fresh?bonus:0):mode=="replay"&&i>=2?0:bonus;var expected=mode is "lethal" or "lethal-chain"?1:baseDamage+extra;Require(hit.damage==expected,$"Class attack damage {cls}/{mode}: {hit.damage}, expected {expected}");}
            Require(hits.Count>0,"No observed powered hits.");
            var plays=mode=="replay"?2:1;
            if(mode!="lethal")Require(counter.AttacksThisTurn==plays&&counter.Charges==0,"Class Attack count/consumption mismatch.");
            if(mode=="lethal-chain")Require(hits.Count==1&&enemies.Skip(1).All(e=>e.IsAlive),"Dead target continued receiving chain hits.");
            return new{cls,mode,up,charges,primed,bonus,hits=hits.Select(h=>h.damage).ToArray(),distinctTargets=seen.Count,attacks=counter.AttacksThisTurn,counter.Charges,cloneIndependent=true,savedProperties=true,pile=card.Pile?.Type.ToString()};
        }finally{observed=null;NativeStartup.Remove(patch);}
    }
    public static async Task<object> MageFlow(RunState run,int chargeCount,bool up,string mode)
    {
        var player=run.Players.Single();Require(PlayerBloodlines.Heir(player).classId=="mage","Mage fixture required.");
        var enemies=await Setup(player);var counter=player.Creature.GetPower<P_class>()!;
        var hand=PileType.Hand.GetPile(player);var draws=new List<int>();var damage=new List<int>();
        if(mode=="reservoir")await Apply(player,"reservoir",2);
        async Task Skill()=>await Play(await Add(player,ModelDb.Card<DefendIronclad>(),up));
        async Task Attack(int expectedDraw){
            var card=await Add(player,ModelDb.Card<TwinStrike>(),up);var before=hand.Cards.Count;
            var hp=enemies[0].CurrentHp;var spent=counter.Charges;
            var repeats=player.Creature.Powers.OfType<LegacyPower>().Any(p=>p.Key=="double_tap")?2:1;
            await Play(card,enemies[0]);var drawn=hand.Cards.Count-before+1;draws.Add(drawn);
            var lost=hp-enemies[0].CurrentHp;damage.Add(lost);
            Require(lost==2*(up?7:5)*repeats+(mode=="reservoir"?4*spent:0),"Mage native HP damage differs.");
            Require(drawn==expectedDraw,"Arcane Flow drew "+drawn+", expected "+expectedDraw);
            Require(counter.Charges==0,"Arcane Flow did not consume charges.");
        }
        for(var i=0;i<chargeCount;i++)await Skill();
        Require(counter.Charges==Math.Min(2,chargeCount),"Arcane Flow charge cap.");
        if(mode=="replay")await Apply(player,"double_tap",1);
        if(mode=="blocked")await Apply(player,"no_draw",1);
        if(mode=="full")while(hand.Cards.Count<9)await Add(player,ModelDb.Card<DefendIronclad>(),up);
        var expected=mode is "blocked" or "full"?0:Math.Min(2,chargeCount);
        // Full hand is inspected separately: an Attack in Play may leave a slot.
        if(mode=="full")expected=1;
        await Attack(expected);
        Require(counter.ArcaneDrawUsed==(chargeCount>0),"Uncharged Attack used the draw allowance.");
        BaseLib.Utils.BetaMainCompatibility.CacheSavedProperties(typeof(P_class));
        var saved=SavedProperties.From(counter)??new SavedProperties();
        var loaded=(P_class)ModelDb.Power<P_class>().ToMutable();saved.Fill(loaded);
        Require(loaded.ArcaneDrawUsed==counter.ArcaneDrawUsed,"Arcane Flow allowance serialization.");
        var clone=(P_class)counter.MutableClone();clone.ArcaneDrawUsed=!counter.ArcaneDrawUsed;
        Require(clone.ArcaneDrawUsed!=counter.ArcaneDrawUsed,"Arcane Flow clone shares allowance.");
        foreach(var held in hand.Cards.ToArray())await CardPileCmd.Add(held,PileType.Discard,skipVisuals:true);
        if(mode=="blocked")await PowerCmd.Remove(player.Creature.Powers.OfType<NoDrawPower>().Single());
        await Skill();await Skill();await Attack(chargeCount==0?2:0);
        // Unspent charges survive the turn; the once-per-turn draw allowance resets.
        await Skill();Require(counter.Charges==1,"Missing banked Arcane Charge.");
        await BalanceOracle.End(player,true);
        Require(!counter.ArcaneDrawUsed&&counter.Charges==1,"Arcane Flow turn reset/carry.");
        foreach(var held in hand.Cards.ToArray())await CardPileCmd.Add(held,PileType.Discard,skipVisuals:true);
        await Attack(1);
        return new{chargeCount,up,mode,draws,damage,serialized=true,cloneIndependent=true,realTurnReset=true,bankedCharge=true};
    }
    public static async Task<object> MagePool(RunState run,string key,bool up,string mode,IDisposable selector)
    {
        using(selector){
            var player=run.Players.Single();var enemies=await Setup(player);var counter=player.Creature.GetPower<P_class>()!;
            var hand=PileType.Hand.GetPile(player);var cls=PlayerBloodlines.Heir(player).classId;
            var card=(LegacyCard)await Add(player,NativeCards.Get(key),up);
            var restored=(LegacyCard)run.LoadCard(card.ToSerializable(),player);
            Require(restored.DesignCard.Rules()==card.DesignCard.Rules()&&restored.IsUpgraded==up,"Mage pool saved card changed.");run.RemoveCard(restored);
            async Task Skill()=>await Play(await Add(player,ModelDb.Card<DefendIronclad>()));
            if(key=="mage_rune_detonation"){
                var charges=mode=="one"?1:mode=="zero"?0:2;
                for(var i=0;i<charges;i++)await Skill();
                if(mode=="used") {await Play(await Add(player,ModelDb.Card<StrikeIronclad>()),enemies[0]);await Skill();await Skill();}
                if(mode=="replay")await Apply(player,"double_tap",1);
                var refund=cls=="mage"&&charges==2&&mode!="used"?1:0;
                Require(PilotCardInfo.Amount(card,"energy")==refund,"Pilot charge refund prediction.");
                var before=hand.Cards.Count;await Play(card,enemies[0],refund);
                var draw=hand.Cards.Count-before+1;Require(draw==(refund>0?2:cls=="mage"&&mode=="one"?1:0),"Rune Detonation charge draw.");
                await Skill();await Skill();await Play(await Add(player,NativeCards.Get(key),up),enemies[0]);
                return new{key,up,mode,cls,refund,draw,repeatedRefund=0,saveRoundTrip=true};
            }
            if(key=="mage_spell_notes"){
                var top=PileType.Draw.GetPile(player).Cards.Take(4).ToArray();
                var before=hand.Cards.Count;await Play(card);
                Require(hand.Cards.Count==before&&top[0].Pile?.Type==PileType.Discard&&hand.Cards.Contains(top[1]),"Spell Notes Scry then draw ordering.");
                Require(counter.Charges==1,"Spell Notes passive charge.");await Skill();
                await Play(await Add(player,NativeCards.Get("mage_ember_dart")),enemies[0]);
                Require(hand.Cards.Contains(top[2])&&hand.Cards.Contains(top[3]),"Prepared top cards did not reach Arcane Flow.");
                return new{key,up,mode,scry=card.DesignCard.SpecialAmount,printedDraw=1,flowDraw=2,saveRoundTrip=true};
            }
            if(key is "mage_ember_dart" or "mage_moonstone_lance"){
                Require(card.ShouldRetainThisTurn,"Cash-out Attack must Retain.");
                await BalanceOracle.End(player,true);Require(hand.Cards.Contains(card),"Cash-out Attack was not retained.");
                var growth=key=="mage_moonstone_lance"?(up?3:2):0;
                Require(card.DynamicVars.Damage.BaseValue==card.DesignCard.Damage+growth,"Retained cash-out growth.");
                await Skill();await Skill();var before=hand.Cards.Count;var hp=enemies[0].CurrentHp;
                await Play(card,enemies[0]);Require(hand.Cards.Count-before+1==2&&hp-enemies[0].CurrentHp==card.DesignCard.Damage+growth,"Retained cash-out damage/draw.");
                return new{key,up,mode,growth,flowDraw=2,cost=card.DesignCard.Cost,saveRoundTrip=true};
            }
            var hpBefore=player.Creature.CurrentHp;var held=hand.Cards.Count;
            await Play(card);var drawn=hand.Cards.Count-held+1;
            Require(counter.Charges==2&&drawn==card.DesignCard.Amount("draw"),"Delayed setup draw/charges.");
            var loss=hpBefore-player.Creature.CurrentHp;Require(loss==card.DesignCard.Amount("hpLoss"),"Overcharge HP payment.");
            await BalanceOracle.End(player,true);
            Require(player.PlayerCombatState!.Energy==3+card.DesignCard.Amount("nextEnergy")&&counter.Charges==2&&!counter.ArcaneDrawUsed,"Delayed setup Energy and banked charges.");
            var count=hand.Cards.Count;await Play(await Add(player,NativeCards.Get("mage_ember_dart")),enemies[0]);
            Require(hand.Cards.Count-count==2,"Delayed setup Arcane Flow draw.");
            return new{key,up,mode,drawn,hpLoss=loss,nextEnergy=card.DesignCard.Amount("nextEnergy"),flowDraw=2,saveRoundTrip=true};
        }
    }
    public static async Task<object> DamageControl(RunState run,string power,bool up)
    {
        const string patch="CapeKid.SpireLegacy2.PowerAuditControl";var harmony=new Harmony(patch);var player=run.Players.Single();
        try{var enemies=await Setup(player,false);await Apply(player,power,2);
            harmony.Patch(typeof(Hook).GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name=="AfterDamageGiven"),prefix:new HarmonyMethod(typeof(PowerAuditOracle),nameof(Observe)));
            var card=await Add(player,ModelDb.Card<TwinStrike>(),up);observed=card;hits.Clear();await Play(card,enemies[0]);var first=hits.Select(h=>h.damage).ToArray();Require(first.SequenceEqual(new[]{up?9:7,up?9:7}),"Native attack bonus control.");
            var next=await Add(player,ModelDb.Card<StrikeIronclad>(),up);observed=next;hits.Clear();await Play(next,enemies[0]);var expected=(up?9:6)+(power=="strength"?2:0);Require(hits.Single().damage==expected,"Native damage control consumption.");
            return new{power,up,first,next=hits.Single().damage,vigorRemaining=player.Creature.GetPowerAmount<VigorPower>()};
        }finally{observed=null;NativeStartup.Remove(patch);}
    }
    public static async Task<object> MultihitCoopPrepare(RunState run,bool up)
    {
        foreach(var enemy in run.Players[0].Creature.CombatState!.GetOpponentsOf(run.Players[0].Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
        foreach(var player in run.Players.OrderBy(p=>p.NetId)){
            foreach(var held in player.PlayerCombatState!.Hand.Cards.ToArray())await CardPileCmd.Add(held,PileType.Draw,skipVisuals:true);
            await PlayerCmd.GainEnergy(20,player);
            for(var i=0;i<2;i++)await Add(player,ModelDb.Card<DefendIronclad>(),up);
            for(var i=0;i<2;i++)await Add(player,ModelDb.Card<TwinStrike>(),up);
        }
        return new{prepared=true,up};
    }
    public static async Task<object> MultihitCoopPlay(RunState run,bool up)
    {
        const string patch="CapeKid.SpireLegacy2.MultiHitCoop";var harmony=new Harmony(patch);var player=PlayerBloodlines.LocalPlayer!;var checkpoint=RunManager.Instance.ChecksumTracker.NextId;
        var others=run.Players.Where(p=>p!=player).Select(p=>new{p,charges=p.Creature.GetPower<P_class>()!.Charges,hand=p.PlayerCombatState!.Hand.Cards.Count,energy=p.PlayerCombatState.Energy}).ToArray();
        try{
            harmony.Patch(typeof(Hook).GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name=="AfterDamageGiven"),prefix:new HarmonyMethod(typeof(PowerAuditOracle),nameof(Observe)));
            foreach(var card in player.PlayerCombatState!.Hand.Cards.OfType<DefendIronclad>().ToArray())await Play(card);
            var counter=player.Creature.GetPower<P_class>()!;var primed=counter.Charges;Require(primed==(PlayerBloodlines.Heir(player).classId=="knight"?1:2),"Co-op class priming.");
            var target=player.Creature.CombatState!.GetOpponentsOf(player.Creature).First(e=>e.IsAlive);var measured=new List<int[]>();
            foreach(var card in player.PlayerCombatState.Hand.Cards.OfType<TwinStrike>().ToArray()){
                observed=card;hits.Clear();await Play(card,target);measured.Add(hits.Select(h=>h.damage).ToArray());Require(counter.Charges==0,"Co-op charge consumption.");
            }
            var damage=up?7:5;Require(measured.Count==2&&measured[0].SequenceEqual(new[]{damage+(PlayerBloodlines.Heir(player).classId=="knight"?2:0),damage+(PlayerBloodlines.Heir(player).classId=="knight"?2:0)})&&measured[1].SequenceEqual(new[]{damage,damage}),"Co-op multi-hit class effect.");
            Require(others.All(o=>o.p.Creature.GetPower<P_class>()!.Charges==o.charges&&o.p.PlayerCombatState!.Hand.Cards.Count==o.hand&&o.p.PlayerCombatState.Energy==o.energy),"Multi-hit class effect leaked owners.");
            return new{owner=player.NetId,cls=PlayerBloodlines.Heir(player).classId,up,primed,hits=measured,counter.Charges,ownerOnly=true,checksumNext=checkpoint+4};
        }finally{observed=null;NativeStartup.Remove(patch);}
    }
    public static async Task<object> Rhythm(RunState run,bool up,string mode,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var enemies=await Setup(player);var counter=player.Creature.GetPower<P_class>()!;
            await Apply(player,"quiver",2);var counts=new List<int>();var draws=new List<int>();var printedMarks=0;
            for(var i=1;i<=6;i++) {
                var canonical=mode=="area"?ModelDb.Card<Thunderclap>() as CardModel:mode=="random"?NativeCards.Get(Design.Rows("cards").First(r=>r.Text("special")=="random_attack").Text("id")):ModelDb.Card<TwinStrike>();
                var card=await Add(player,canonical,up);var before=player.PlayerCombatState!.Hand.Cards.Count;
                if(mode=="area")printedMarks+=(int)card.DynamicVars.Vulnerable.BaseValue;
                await Play(card,enemies[0]);var drawn=player.PlayerCombatState.Hand.Cards.Count-before+1;
                Require(counter.AttacksThisTurn==i&&counter.DisplayAmount==i%3&&drawn==(i%3==0?3:0),"Hunter Rhythm per-card/draw cap mismatch.");
                counts.Add(counter.DisplayAmount);draws.Add(drawn);
            }
            var marks=enemies.Select(e=>e.GetPowerAmount<VulnerablePower>()).ToArray();
            Require(marks.Sum()==(mode=="area"?enemies.Length*(2+printedMarks):2),"Hunter Rhythm target selection mismatch: "+string.Join(",",marks));
            await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
            Require(counter.AttacksThisTurn==0&&counter.DisplayAmount==0,"Hunter Rhythm did not reset next turn.");
            return new{mode,up,counts,draws,marks,printedMarks,nextTurnReset=true};
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
    public static async Task<object> Engines(RunState run,string mode,bool up,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var creature=player.Creature;var enemies=await Setup(player);
            if(mode=="third") {
                await Apply(player,"skill_block",2);await Apply(player,"skill_block",1);
                await Apply(player,"skill_energy",2);await Apply(player,"attack_vigor",4);await Apply(player,"arcane_echo",2);
                var block=creature.Block;var hand=player.PlayerCombatState!.Hand.Cards.Count;
                for(var i=1;i<=3;i++) {
                    var card=await Add(player,ModelDb.Card<DefendIronclad>(),up);await Play(card);
                    Require(creature.Block-block==i*(up?8:5)+(i==3?3:0),"Third Skill Block/stacking mismatch.");
                    Require(creature.GetPowerAmount<EnergyNextTurnPower>()==(i==3?2:0),"Third Skill next-Energy mismatch.");
                    Require(player.PlayerCombatState.Hand.Cards.Count-hand==2,"Arcane Echo repeated within a turn.");
                }
                for(var i=1;i<=3;i++){await Play(await Add(player,ModelDb.Card<StrikeIronclad>(),up),enemies[0]);Require(creature.GetPowerAmount<VigorPower>()==(i==3?4:0),"Third Attack Vigor mismatch.");}
                await Play(await Add(player,ModelDb.Card<TwinStrike>(),up),enemies[0]);Require(creature.GetPowerAmount<VigorPower>()==0,"Generated Vigor was not consumed by next Attack.");
                await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState.Phase==PlayerTurnPhase.Play);
                Require(player.PlayerCombatState.Energy==5&&creature.GetPower<EnergyNextTurnPower>()==null,"Next Energy not delivered once.");
                var nextHand=player.PlayerCombatState.Hand.Cards.Count;
                await Play(await Add(player,ModelDb.Card<DefendIronclad>(),up));
                Require(player.PlayerCombatState.Hand.Cards.Count-nextHand==2&&creature.GetPower<EnergyNextTurnPower>()==null,"First Skill/reset claims did not reset.");
                return new{mode,up,stack=3,skillPlays=4,attackPlays=4,firstSkillDraw=2,nextEnergy=5,reset=true};
            }
            if(mode=="discard") {
                await Apply(player,"discard_energy",2);await Apply(player,"discard_block",3);
                var before=player.PlayerCombatState!.Energy;var block=creature.Block;
                for(var i=0;i<2;i++){var card=await Add(player,ModelDb.Card<DefendIronclad>());await CardCmd.Discard(Context,card);}
                Require(player.PlayerCombatState.Energy==before+2&&creature.Block==block+6,"Manual discard once-only claim mismatch.");
                await Add(player,ModelDb.Card<DefendIronclad>());
                await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState.Phase==PlayerTurnPhase.Play);
                Require(player.PlayerCombatState.Energy==3&&creature.Block==0,"End-turn discard paid a manual reward.");
                var card2=await Add(player,ModelDb.Card<DefendIronclad>());await CardCmd.Discard(Context,card2);
                Require(player.PlayerCombatState.Energy==5&&creature.Block==3,"Discard next-turn claim did not reset.");
                return new{mode,up,firstReward=2,secondReward=0,nextReward=2,automaticReward=0};
            }
            if(mode=="exhaust") {
                await Apply(player,"exhaust_damage",3);await Apply(player,"dark_embrace",1);await Apply(player,"feel_no_pain",2);
                var hp=enemies.Select(e=>e.CurrentHp).ToArray();var block=creature.Block;var hand=player.PlayerCombatState!.Hand.Cards.Count;
                for(var i=0;i<2;i++){var card=await Add(player,ModelDb.Card<DefendIronclad>(),up);await BranchCommands.Exhaust(Context,card);Require(card.Pile?.Type==PileType.Exhaust,"Exhaust engine stranded fuel.");}
                Require(creature.Block-block==4&&player.PlayerCombatState.Hand.Cards.Count-hand==2,"Exhaust Block/draw hook mismatch.");
                Require(enemies.Select((e,i)=>hp[i]-e.CurrentHp).All(n=>n==6),"Exhaust damage did not hit every live enemy.");
                return new{mode,up,exhausts=2,block=4,draw=2,damage=6};
            }
            if(mode=="draw-chain") {
                await CardPileCmd.RemoveFromCombat(CardPile.GetCards(player,Piles).ToArray(),skipVisuals:true);
                await Apply(player,"evolve",1);await Apply(player,"fire_breathing",2);
                for(var i=0;i<20;i++)await Add(player,ModelDb.Card<Wound>(),false,PileType.Draw);
                var hp=enemies.Select(e=>e.CurrentHp).ToArray();await CardPileCmd.Draw(Context,1,player);
                Require(player.PlayerCombatState!.Hand.Cards.Count==10,"Status draw recursion did not stop at full hand.");
                Require(enemies.Select((e,i)=>hp[i]-e.CurrentHp).All(n=>n==20),"Status draw damage duplicated or skipped hooks.");
                return new{mode,up,hand=10,drawPile=10,damage=20,bounded=true};
            }
            if(mode=="sadistic") {
                await Apply(player,"sadistic",3);await PowerCmd.Apply<FlameBarrierPower>(Context,enemies[0],4,enemies[0],null);
                var hp=enemies[0].CurrentHp;var self=creature.CurrentHp;
                await CardEffects.Apply(Context,"weak",enemies[0],2,creature,null);
                Require(hp-enemies[0].CurrentHp==3&&creature.CurrentHp==self,"Sadistic damage recursed through retaliation.");
                hp=enemies[0].CurrentHp;await CardEffects.Apply(Context,"weak",enemies[0],1,creature,null);
                Require(hp-enemies[0].CurrentHp==3,"Sadistic stacking did not trigger once.");
                await PowerCmd.Remove(enemies[0].GetPower<WeakPower>()!);Require(hp-enemies[0].CurrentHp==3,"Debuff removal triggered Sadistic.");
                return new{mode,up,applies=2,damage=6,unpoweredNoRetaliation=true};
            }
            if(mode=="turn") {
                creature.SetCurrentHpInternal(creature.MaxHp-10);
                await Apply(player,"brutality",2);await Apply(player,"berserk",1);await Apply(player,"combust",3);await Apply(player,"metallicize",4);await Apply(player,"plated",2);
                var hp=creature.CurrentHp;var enemyHp=enemies.Select(e=>e.CurrentHp).ToArray();
                await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
                Require(creature.CurrentHp==hp-3&&player.PlayerCombatState!.Energy==4&&player.PlayerCombatState.Hand.Cards.Count==7,"Turn-start/end loss, Energy or draw mismatch.");
                Require(enemies.Select((e,i)=>enemyHp[i]-e.CurrentHp).All(n=>n==3),"Combust did not damage every enemy once.");
                var plated=creature.GetPower<P_plated>()!;
                await CreatureCmd.Damage(Context,creature,2,ValueProp.Unblockable|ValueProp.Unpowered,enemies[0]);Require(plated.Amount==2,"Unpowered HP loss consumed Plated.");
                await CreatureCmd.Damage(Context,creature,2,ValueProp.Move,enemies[0]);Require(plated.Amount==1,"Powered HP damage did not consume Plated.");
                await PowerCmd.Remove(plated);await Apply(player,"plated",2);Require(creature.GetPower<P_plated>()!.Amount==2,"Removed/reapplied power retained old state.");
                return new{mode,up,hpLoss=3,energy=4,hand=7,damage=3,platedReset=true};
            }
            if(mode=="foresight-empty") {
                await Apply(player,"foresight",2);await Apply(player,"scry_block",3);
                foreach(var card in player.Deck.Cards.ToArray())await CardPileCmd.RemoveFromDeck(card);
                await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
                Require(player.Deck.Cards.Count==0&&creature.Block==3,"Empty-deck Foresight did not complete Scry.");
                return new{mode,up,deck=0,block=3,nextTurn=true};
            }
            if(mode=="misc") {
                await Apply(player,"power_draw",2);await Apply(player,"thousand_cuts",3);
                await Apply(player,"foresight",2);await Apply(player,"scry_block",3);await Apply(player,"establishment",1);
                var hp=enemies.Select(e=>e.CurrentHp).ToArray();var hand=player.PlayerCombatState!.Hand.Cards.Count;
                await Play(await Add(player,ModelDb.Card<Inflame>(),up));
                Require(player.PlayerCombatState.Hand.Cards.Count-hand==2&&enemies.Select((e,i)=>hp[i]-e.CurrentHp).All(n=>n==3),"Power draw/Thousand Cuts mismatch.");
                var retained=await Add(player,NativeCards.Get(Design.Rows("cards").First(r=>r.Flag("retain")&&r.Number("cost")>0).Text("id")),up);
                var cost=retained.EnergyCost.GetWithModifiers(CostModifiers.Local);
                for(var i=1;i<=2;i++) {
                    await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState.Phase==PlayerTurnPhase.Play);
                    Require(retained.Pile?.Type==PileType.Hand&&retained.EnergyCost.GetWithModifiers(CostModifiers.Local)==Math.Max(0,cost-i)&&creature.Block==3,"Establishment/Foresight turn ordering mismatch.");
                }
                return new{mode,up,powerDraw=2,cuts=3,scryBlock=3,retainedTurns=2,cost=retained.EnergyCost.GetWithModifiers(CostModifiers.Local)};
            }
            throw new ArgumentException("Unknown engine fixture: "+mode);
        }
    }
    public static async Task<object> EnchantedCopies(RunState run,string mode,bool up)
    {
        var player=run.Players.Single();var checks=new List<object>();
        foreach(var key in new[]{"knight_guard","knight_stand_ready","ranger_shed_cloak"}) {
            await Setup(player);await Apply(player,"dexterity",2);
            var original=(LegacyCard)await Add(player,NativeCards.Get(key),up);CardCmd.Enchant<Nimble>(original,3);
            LegacyCard card;
            if(mode=="dupe") {
                var method=AccessTools.Method(typeof(CardModel),"CreateDupe");
                card=(LegacyCard)method.Invoke(original,method.GetParameters().Select(p=>p.ParameterType==typeof(Player)?player:p.HasDefaultValue?p.DefaultValue:throw new MissingMethodException("Dupe parameter.")).ToArray())!;
            }else {
                var loaded=run.LoadCard(original.ToSerializable(),player);card=(LegacyCard)player.Creature.CombatState!.CloneCard(loaded);run.RemoveCard(loaded);
            }
            Require(card.Enchantment?.Amount==3,"Nimble disappeared from a card copy.");
            await CardPileCmd.RemoveFromCombat(original,skipVisuals:true);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            var design=card.DesignCard;var secondary=design.Row.Text("special")=="exhaust_nonattacks";
            if(secondary)await Add(player,ModelDb.Card<DefendIronclad>());
            var before=player.Creature.Block;await Play(card);
            var expected=(design.Block>0?design.Block+5:0)+(secondary?design.SpecialAmount+3:0);
            Require(player.Creature.Block-before==expected,"Copied Nimble immediate/secondary Block differs.");
            var delayed=design.Amount("nextBlock");
            if(delayed>0) {
                Require(player.Creature.GetPowerAmount<BlockNextTurnPower>()==delayed+5,"Copied Nimble delayed amount differs.");
                await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
                Require(player.Creature.Block==delayed+5&&player.Creature.GetPower<BlockNextTurnPower>()==null,"Copied Nimble delayed delivery differs.");
            }
            checks.Add(new{key,mode,up,block=expected,nextBlock=delayed>0?delayed+5:0});
        }
        return checks;
    }
    public static object Snapshot(RunState run)=>run.Players.Select(p=>new{owner=p.NetId,cls=PlayerBloodlines.Heir(p).classId,charges=p.Creature.GetPower<P_class>()?.Charges,attacks=p.Creature.GetPower<P_class>()?.AttacksThisTurn}).ToArray();
    private sealed class RetainSelector : ICardSelector
    {
        public int Choose=1,Calls;public HashSet<CardModel> Preferred=[];
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options,int minSelect,int maxSelect)
        {Calls++;Require(minSelect==0,"Retention unexpectedly became mandatory.");var cards=options.ToArray();return Task.FromResult(cards.OrderBy(c=>Preferred.Contains(c)?0:1).Take(Math.Min(Choose,maxSelect)).AsEnumerable());}
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,IReadOnlyList<CardRewardAlternative> alternatives)=>new(){card=options.FirstOrDefault()?.Card};
    }
    public static async Task<object> RetentionEdges(RunState run,bool empty)
    {
        var player=run.Players.Single();await Setup(player);await Apply(player,"well_laid",2);
        if(!empty)await PowerCmd.Apply<WellLaidPlansPower>(Context,player.Creature,1,player.Creature,null);
        var originals=new List<CardModel>();if(!empty)for(var i=0;i<5;i++)originals.Add(await Add(player,ModelDb.Card<DefendIronclad>()));
        var flush=Hook.ShouldFlush(player.Creature.CombatState!,player);var selector=new RetainSelector{Preferred=originals.ToHashSet()};
        var method=AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector),typeof(bool)])??AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector)]);
        using((IDisposable)method.Invoke(null,method.GetParameters().Length==2?[selector,false]:[selector])!) {
            await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
            var first=originals.Count(c=>c.Pile?.Type==PileType.Hand);
            Require(first==(empty?0:flush?2:5),"Native/custom retention interaction differs.");
            Require(originals.All(c=>!c.ShouldRetainThisTurn&&!c.Keywords.Contains(CardKeyword.Retain)),"Retention keywords became permanent.");
            if(empty)return new{empty,first,turn=player.PlayerCombatState!.TurnNumber,hand=player.PlayerCombatState.Hand.Cards.Count,noHang=true};
            await PowerCmd.Remove(player.Creature.GetPower<WellLaidPlansPower>()!);
            await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
            var second=originals.Count(c=>c.Pile?.Type==PileType.Hand);Require(second==1,"Measured Breath exceeded a new bounded choice after native retention removal.");
            selector.Choose=0;await BalanceOracle.End(player,true);await Wait(()=>player.PlayerCombatState!.Phase==PlayerTurnPhase.Play);
            Require(originals.All(c=>c.Pile?.Type!=PileType.Hand),"Declined retention persisted permanently.");
            return new{empty,flush,first,second,third=0,calls=selector.Calls,temporary=true};
        }
    }
    public static async Task<object> CoopPrepare(RunState run,bool up)
    {
        foreach(var enemy in run.Players[0].Creature.CombatState!.GetOpponentsOf(run.Players[0].Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
        foreach(var player in run.Players.OrderBy(p=>p.NetId)) {
            foreach(var held in player.PlayerCombatState!.Hand.Cards.ToArray())await CardPileCmd.Add(held,PileType.Draw,skipVisuals:true);
            foreach(var key in new[]{"arcane_echo","skill_block","discard_energy"})foreach(var old in player.Creature.Powers.OfType<LegacyPower>().Where(p=>p.Key==key).ToArray())await PowerCmd.Remove(old);
            await Apply(player,"arcane_echo",2);await Apply(player,"skill_block",3);await Apply(player,"discard_energy",2);
            await PlayerCmd.GainEnergy(20,player);
            for(var i=0;i<3;i++)await Add(player,ModelDb.Card<DefendIronclad>(),up);
            if(PlayerBloodlines.Heir(player).classId=="ranger")for(var i=0;i<3;i++)await Add(player,ModelDb.Card<TwinStrike>(),up);
        }
        return new{up,owners=run.Players.Count};
    }
    public static async Task<object> CoopPlay(RunState run,bool up)
    {
        var checkpoint=RunManager.Instance.ChecksumTracker.NextId;
        var player=PlayerBloodlines.LocalPlayer!;var others=run.Players.Where(p=>p!=player).Select(p=>new{p,energy=p.PlayerCombatState!.Energy,block=p.Creature.Block,hand=p.PlayerCombatState.Hand.Cards.Count,charges=p.Creature.GetPower<P_class>()!.Charges}).ToArray();
        var energy=player.PlayerCombatState!.Energy;var block=player.Creature.Block;
        var cards=player.PlayerCombatState.Hand.Cards.OfType<DefendIronclad>().Where(c=>c.IsUpgraded==up).TakeLast(3).ToArray();Require(cards.Length==3,"Missing co-op Skill fuel.");
        foreach(var card in cards)await Play(card);
        Require(player.PlayerCombatState.Energy==energy-3&&player.Creature.Block-block==3*(up?8:5)+3,"Co-op third Skill stacking differs.");
        var counter=player.Creature.GetPower<P_class>()!;var cls=PlayerBloodlines.Heir(player).classId;
        Require(counter.Charges==(cls=="knight"?1:cls=="mage"?2:0),"Co-op class Skill cap differs.");
        var attackPlays=0;if(cls=="ranger") {
            var before=counter.AttacksThisTurn;var target=player.Creature.CombatState!.GetOpponentsOf(player.Creature).First(e=>e.IsAlive);
            var attacks=player.PlayerCombatState.Hand.Cards.OfType<TwinStrike>().Where(c=>c.IsUpgraded==up).TakeLast(3).ToArray();Require(attacks.Length==3,"Missing prepared co-op Attack fuel.");
            foreach(var card in attacks)await Play(card,target);
            attackPlays=3;Require(counter.AttacksThisTurn==before+3&&counter.DisplayAmount==0,"Co-op Hunter Rhythm counted hits instead of cards.");
        }
        Require(others.All(o=>o.p.PlayerCombatState!.Energy==o.energy&&o.p.Creature.Block==o.block&&o.p.PlayerCombatState.Hand.Cards.Count==o.hand&&o.p.Creature.GetPower<P_class>()!.Charges==o.charges),"Power hook leaked to another owner.");
        Require(RunManager.Instance.ChecksumTracker.IsEnabled,"Co-op checksums must remain enabled.");
        await Wait(()=>RunManager.Instance.ChecksumTracker.NextId>=checkpoint+3+attackPlays);
        return new{owner=player.NetId,cls,up,plays=3,attackPlays,charges=counter.Charges,ownerOnly=true,checksumNext=RunManager.Instance.ChecksumTracker.NextId};
    }
    public static async Task<object> CoopCheckpoint(uint minimum)
    {
        await Wait(()=>!RunManager.Instance.ActionExecutor.IsRunning&&RunManager.Instance.ChecksumTracker.NextId>=minimum);
        return new{next=RunManager.Instance.ChecksumTracker.NextId,enabled=RunManager.Instance.ChecksumTracker.IsEnabled};
    }
}
