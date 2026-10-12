using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// No ordinary gameplay entry point: called only by the isolated opt-in bridge.
public static class BalanceOracle
{
    private static SceneTree Tree=>(SceneTree)Engine.GetMainLoop();
    private static async Task Wait(Func<bool> done)
    {
        var until=Time.GetTicksMsec()+30000;
        while(!done()){if(Time.GetTicksMsec()>until)throw new TimeoutException("Balance fixture did not settle.");await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);}
    }
    private static async Task Play(CardModel card)
    {
        var enemy=card.Owner.Creature.CombatState!.GetOpponentsOf(card.Owner.Creature).FirstOrDefault(e=>e.IsAlive);
        if(!card.TryManualPlay(card.TargetType==TargetType.AnyEnemy?enemy:null))throw new InvalidDataException("Paid balance play refused "+card.Id);
        await Wait(()=>card.Pile?.Type is not (PileType.Hand or PileType.Play));
        await Wait(()=>NCard.FindOnTable(card)==null);
    }
    private static async Task<LegacyCard> Add(Player player,string key,bool upgraded=false,PileType pile=PileType.Hand)
    {
        var card=(LegacyCard)player.Creature.CombatState!.CreateCard(NativeCards.Get(key),player);
        if(upgraded)CardCmd.Upgrade(card);await CardPileCmd.Add(card,pile,skipVisuals:true);return card;
    }
    private static async Task End(Player player,bool protect)
    {
        NModalContainer.Instance?.Clear();
        CombatManager.Instance.Unpause();
        await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
        if(protect)await CreatureCmd.GainBlock(player.Creature,999,ValueProp.Unpowered,null);
        var combat=player.Creature.CombatState!;
        var turn=player.PlayerCombatState!.TurnNumber;
        CombatManager.Instance.SetReadyToEndTurn(player,false);
        await Wait(()=>!player.Creature.IsAlive||!combat.GetOpponentsOf(player.Creature).Any(e=>e.IsAlive)||CombatManager.Instance.IsEnding||player.PlayerCombatState.Phase==PlayerTurnPhase.Play&&player.PlayerCombatState.TurnNumber>turn);
    }
    public static async Task<object> Probe(RunState run,string key,bool upgraded,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var hand=PileType.Hand.GetPile(player);var context=new BlockingPlayerChoiceContext();
            foreach(var old in hand.Cards.ToArray())await CardPileCmd.Add(old,PileType.Draw,skipVisuals:true);
            for(var i=0;i<20;i++)await Add(player,Runtime.Heir.classId+"_guard",pile:PileType.Draw);
            var targets=player.Creature.CombatState!.GetOpponentsOf(player.Creature).Where(e=>e.IsAlive).ToArray();
            foreach(var target in targets){target.SetMaxHpInternal(10000);target.SetCurrentHpInternal(10000);}
            var card=await Add(player,key,upgraded);
            var beforeEnergy=player.PlayerCombatState!.Energy;var beforeBlock=player.Creature.Block;
            if(key=="knight_second_wind"){
                await Add(player,"knight_guard");await Add(player,"knight_guard");
                await Play(card);
                var ordinary=player.Creature.Block-beforeBlock;var fuel=PileType.Exhaust.GetPile(player).Cards.Count;
                await PlayerCmd.GainEnergy(1,player);
                await PowerCmd.Apply<FeelNoPainPower>(context,player.Creature,2,player.Creature,null);
                await Add(player,"knight_guard");await Add(player,"knight_guard");card=await Add(player,key,upgraded);
                beforeBlock=player.Creature.Block;await Play(card);
                return new{key,upgraded,ordinaryBlock=ordinary,engineBlock=player.Creature.Block-beforeBlock,fuelExhausted=fuel,cost=card.DesignCard.Cost};
            }
            if(key=="mage_rune_familiar"){
                await Play(card);var first=hand.Cards.Count;
                var power=await Add(player,"surge");await Play(power);
                return new{key,upgraded,firstDraw=first,secondDraw=hand.Cards.Count-first,cost=card.DesignCard.Cost,energySpent=beforeEnergy-player.PlayerCombatState.Energy};
            }
            if(key=="ranger_long_vigil"){
                var values=new List<decimal>{card.DynamicVars.Damage.BaseValue};
                for(var i=0;i<2;i++){await End(player,true);values.Add(card.DynamicVars.Damage.BaseValue);}
                foreach(var debuff in player.Creature.Powers.Where(p=>p.Type==MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff).ToArray())await PowerCmd.Remove(debuff);
                var hp=targets[0].CurrentHp;var block=targets[0].Block;beforeEnergy=player.PlayerCombatState.Energy;
                await Play(card);
                var restored=(LegacyCard)run.LoadCard(card.ToSerializable(),player);var roundTrip=restored.DynamicVars.Damage.BaseValue==card.DynamicVars.Damage.BaseValue;run.RemoveCard(restored);
                return new{key,upgraded,values,damage=hp-targets[0].CurrentHp+block-targets[0].Block,energySpent=beforeEnergy-player.PlayerCombatState.Energy,roundTrip};
            }
            if(key is "ranger_poison_tip" or "mage_toxic_cloud"){
                await Play(card);var initial=targets.Select(e=>e.GetPowerAmount<PoisonPower>()).ToArray();
                var hp=targets.Select(e=>e.CurrentHp).ToArray();await End(player,true);await End(player,true);
                return new{key,upgraded,initial,twoTurnDamage=targets.Select((e,i)=>hp[i]-e.CurrentHp).ToArray(),cost=card.DesignCard.Cost};
            }
            if(key=="mage_rekindle"){
                var fuel=await Add(player,"mage_guard",pile:PileType.Exhaust);await Play(card);
                return new{key,upgraded,returned=hand.Cards.Contains(fuel),cost=card.DesignCard.Cost,pile=card.Pile?.Type.ToString()};
            }
            throw new ArgumentException("Unknown balance probe card.");
        }
    }
    public static async Task<object> RecoveryLoop(RunState run,IDisposable selector,bool discard=false)
    {
        using(selector){
            var player=run.Players.Single();var hand=PileType.Hand.GetPile(player);
            foreach(var old in hand.Cards.ToArray())await CardPileCmd.Add(old,PileType.Draw,skipVisuals:true);
            if(discard)foreach(var enemy in player.Creature.CombatState!.GetOpponentsOf(player.Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
            var b=await Add(player,discard?"ranger_hooked_arrow":"mage_returned_prophecy",pile:discard?PileType.Discard:PileType.Exhaust);
            var a=await Add(player,discard?"mage_memory_prism":"knight_arms_vault");a.EnergyCost.SetThisCombat(0);b.EnergyCost.SetThisCombat(0);
            var energy=player.PlayerCombatState!.Energy;await Play(a);var returnedB=hand.Cards.Contains(b);
            if(returnedB)await Play(b);
            return new{returnedRecovery=returnedB,returnedOriginal=hand.Cards.Contains(a),energySpent=energy-player.PlayerCombatState.Energy};
        }
    }
    public static async Task<object> RetentionSnapshots(RunState run)
    {
        var player=run.Players.Single();var checks=0;
        foreach(var row in Design.Rows("cards").Where(r=>r.Text("special") is "retain_damage" or "retain_block"))
        foreach(var upgraded in new[]{false,true}){
            var card=await Add(player,row.Text("id"),upgraded);
            await card.AfterFlush(new BlockingPlayerChoiceContext(),player,[],[card]);
            await card.AfterFlush(new BlockingPlayerChoiceContext(),player,[],[card]);
            var saved=(LegacyCard)run.LoadCard(card.ToSerializable(),player);
            decimal Value(LegacyCard c)=>row.Text("special")=="retain_damage"?c.DynamicVars.Damage.BaseValue:c.DynamicVars.Block.BaseValue;
            if(Value(saved)!=Value(card)||saved.RetainedGrowth!=card.RetainedGrowth)throw new InvalidDataException("Retained growth lost: "+card.Key);
            var growth=saved.RetainedGrowth;CardCmd.Downgrade(saved);CardCmd.Upgrade(saved);
            if(saved.RetainedGrowth!=growth)throw new InvalidDataException("Retained growth lost during upgrade: "+card.Key);
            await saved.BeforeCombatStart();
            var baseline=row.Text("special")=="retain_damage"?saved.DesignCard.Damage:saved.DesignCard.Block;
            if(saved.RetainedGrowth!=0||Value(saved)!=baseline)throw new InvalidDataException("Retained growth leaked to a new combat: "+card.Key);
            run.RemoveCard(saved);checks++;
        }
        return new{checks,saveRoundTrip=true,newCombatReset=true,upgradePreservesGrowth=true};
    }
    public static async Task<object> Encounter(RunState run,string build,IDisposable selector)
    {
        using(selector){
            var player=run.Players.Single();var oldScale=Engine.TimeScale;Engine.TimeScale=6;
            try{
                var additions=build switch{
                    "exhaust"=>new[]{"knight_second_wind","knight_burnished_legacy","knight_cast_off","knight_forge_sparks"},
                    "powers"=>new[]{"mage_rune_familiar","reservoir","mage_spell_threads","surge"},
                    "retention"=>new[]{"ranger_long_vigil","ranger_heartseeker","ranger_perch_discipline","mage_crystal_seed"},
                    "poison"=>new[]{"ranger_poison_tip","ranger_poison_tip","ranger_razor_fletching","ranger_double_dose"},
                    _=>throw new ArgumentException("Unknown balance deck.")};
                foreach(var key in additions)await CardPileCmd.Add(run.CreateCard(NativeCards.Get(key),player),PileType.Deck,skipVisuals:true);
                await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
                RunManager.Instance.ActionExecutor.Unpause();await Wait(()=>player.PlayerCombatState?.Phase==PlayerTurnPhase.Play);
                NModalContainer.Instance?.Clear();
                CombatManager.Instance.Unpause();
                var hp=player.Creature.CurrentHp;var seen=new Dictionary<string,int>();var played=new Dictionary<string,int>();var dead=new Dictionary<string,int>();
                var energySpent=0;var endedTurns=0;var exhaustedBudget=false;
                for(var turn=0;turn<20&&player.Creature.IsAlive&&!CombatManager.Instance.IsEnding;turn++){
                    var hand=PileType.Hand.GetPile(player);foreach(var c in hand.Cards)Count(seen,c);
                    var plays=0;
                    while(player.Creature.IsAlive&&!CombatManager.Instance.IsEnding){
                        var enemies=player.Creature.CombatState!.GetOpponentsOf(player.Creature).Where(e=>e.IsAlive).ToArray();if(enemies.Length==0)break;
                        var incoming=enemies.Sum(e=>e.Monster!.NextMove.Intents.OfType<AttackIntent>().Sum(i=>i.GetTotalDamage(new[]{player.Creature},e)));
                        var next=hand.Cards.Where(c=>c.CanPlay()).OrderByDescending(c=>Score(c,incoming-player.Creature.Block)).ThenBy(c=>c.Id.ToString(),StringComparer.Ordinal).FirstOrDefault();
                        if(next==null)break;
                        if(++plays>30){exhaustedBudget=true;break;}
                        var energy=player.PlayerCombatState!.Energy;await Play(next);energySpent+=energy-player.PlayerCombatState.Energy;Count(played,next);
                    }
                    foreach(var c in hand.Cards)Count(dead,c);
                    if(exhaustedBudget||!player.Creature.IsAlive||CombatManager.Instance.IsEnding)break;
                    if(!player.Creature.CombatState!.GetOpponentsOf(player.Creature).Any(e=>e.IsAlive))break;
                    await End(player,false);endedTurns++;
                }
                var victory=!player.Creature.CombatState!.GetOpponentsOf(player.Creature).Any(e=>e.IsAlive);
                if(victory){await Wait(()=>!CombatManager.Instance.IsInProgress);for(var frame=0;frame<30;frame++)await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);}
                return new{build,seed=run.Rng.StringSeed,manor=Runtime.Profile.manor,initialHp=hp,finalHp=player.Creature.CurrentHp,damageTaken=hp-player.Creature.CurrentHp,victory,deadPlayer=!player.Creature.IsAlive,endedTurns,energySpent,seen,played,unplayedEndTurn=dead,exhaustedBudget};
            }finally{Engine.TimeScale=oldScale;}
        }
    }
    private static void Count(Dictionary<string,int> counts,CardModel card){var key=card is LegacyCard lc?lc.Key:card.Id.ToString();counts[key]=counts.GetValueOrDefault(key)+1;}
    private static double Score(CardModel card,int needBlock)
    {
        if(card.Type==CardType.Power)return 100;
        if(card is not LegacyCard lc)return card.Type==CardType.Attack?20:10;
        var d=lc.DesignCard;
        if(needBlock>0&&d.Block>0)return 80+Math.Min(needBlock,d.Block);
        if(d.Amount("energy")>0)return 70;
        if(d.Amount("draw")>0)return 60;
        if(d.Amount("poison")>0||d.Row.Text("special")=="double_poison")return 50;
        if(card.Type==CardType.Attack)return 30+(double)card.DynamicVars.Damage.BaseValue*d.Row.Number("hits")/Math.Max(1,d.Cost);
        return 10;
    }
}
