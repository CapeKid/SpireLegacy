using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
namespace SpireLegacy;

// Opt-in, isolated owned-game presentation probes; never used in a normal launch.
public static class PresentationAuditOracle
{
    private static SceneTree Tree=>(SceneTree)Engine.GetMainLoop();
    public static object Prepare()
    {
        foreach(var key in new[]{"accept_tutorials_ftue","map_select_ftue","combat_rules_ftue","shuffle_ftue","multiplayer_ftue"})MegaCrit.Sts2.Core.Saves.SaveManager.Instance.MarkFtueAsComplete(key);
        return new {privateTutorialsComplete=true};
    }
    public static async Task<object> Motion(string cls)
    {
        var source=new Node2D();var destination=new Node2D();var visual=new Node2D();var sprite=HeirSprite.Create(cls,1);
        Tree.Root.AddChild(source);Tree.Root.AddChild(destination);source.AddChild(visual);visual.AddChild(sprite);HeirRoomMotion.Attach(visual,sprite,false);
        try {
            await Task.Delay(450);var before=sprite.Position;
            source.RemoveChild(visual);destination.AddChild(visual);await Task.Delay(450);
            var after=sprite.Position;var moving=before!=after;
            destination.RemoveChild(visual);source.AddChild(visual);await Task.Delay(450);var again=sprite.Position;
            return new {cls,before=before.ToString(),after=after.ToString(),again=again.ToString(),moving,secondMove=again!=after};
        }finally {source.QueueFree();destination.QueueFree();}
    }
    public static object Resources()=>new {nodes=(long)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),resources=(long)Performance.GetMonitor(Performance.Monitor.ObjectResourceCount),orphans=(long)Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),memory=System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64,nativeMemory=(long)Performance.GetMonitor(Performance.Monitor.MemoryStatic),frameSeconds=Performance.GetMonitor(Performance.Monitor.TimeProcess),sprites=NativeTestBridge.Descendants(Tree.Root).OfType<HeirSprite>().Count(),audio=NativeTestBridge.Descendants(Tree.Root).OfType<AudioStreamPlayer>().Count(a=>a.HasMeta("spirelegacy_sound")),nativeAudio=NativeTestBridge.Descendants(Tree.Root).OfType<AudioStreamPlayer>().Count(a=>!a.HasMeta("spirelegacy_sound")),trails=NativeTestBridge.Descendants(Tree.Root).OfType<NCardTrailVfx>().Count(),textureSlots=((System.Collections.IDictionary)AccessTools.Field(typeof(Runtime),"Textures").GetValue(null)!).Count};
    public static async Task<object> Shop(RunState run)
    {
        var player=PlayerBloodlines.LocalPlayer!;var room=(MegaCrit.Sts2.Core.Rooms.MerchantRoom)run.CurrentRoom!;
        var inventory=room.GetLocalInventory();var entry=inventory.CharacterCardEntries.First(e=>e.IsStocked);
        var sprites=NativeTestBridge.Descendants(Tree.Root).OfType<HeirSprite>().Where(s=>s.Name=="SpireLegacyRoomHeir"&&s.IsVisibleInTree()).ToArray();
        var counts=sprites.Select(s=>s.Events.Count(e=>e=="skill")).ToArray();
        await PlayerCmd.GainGold(1000,player);var gold=player.Gold;var deck=player.Deck.Cards.Count;
        var bought=await entry.OnTryPurchaseWrapper(inventory);
        return new {owner=player.NetId,bought,goldSpent=gold-player.Gold,cardsAdded=player.Deck.Cards.Count-deck,sprites=sprites.Select((s,i)=>new {owner=s.GetMeta("spirelegacy_owner").AsUInt64(),cls=s.ClassId,skillEvents=s.Events.Count(e=>e=="skill")-counts[i]}).ToArray()};
    }
    public static object RoomFacing()=>NativeTestBridge.Descendants(Tree.Root).OfType<HeirSprite>().Where(s=>s.Name=="SpireLegacyRoomHeir"&&s.IsVisibleInTree()).Select(s=>new {owner=s.GetMeta("spirelegacy_owner").AsUInt64(),cls=s.ClassId,nativeX=s.GetParent().GetChildren().OfType<Node2D>().First(n=>n.GetClass()=="SpineSprite").Scale.X,spriteX=s.Scale.X}).ToArray();
    public static object TextureFailures()
    {
        var path=Path.Combine(Runtime.DataDirectory,"presentation-fixture.svg");
        File.WriteAllText(path,"<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'><rect width='8' height='8' fill='red'/></svg>");
        var first=Runtime.Texture(path);first.Dispose();var reloaded=Runtime.Texture(path);
        var valid=GodotObject.IsInstanceValid(reloaded);var errors=new List<string>();
        foreach(var (name,content) in new[]{("bad.svg","not SVG"),("missing.png",(string?)null)}) {
            var target=Path.Combine(Runtime.DataDirectory,name);if(content!=null)File.WriteAllText(target,content);
            try {Runtime.Texture(target);throw new InvalidOperationException("Bad asset was accepted.");}catch(InvalidDataException e){errors.Add(e.ToString());}
        }
        try {HeirPresentation.PlaySound("spirelegacy://mage/unknown",1);throw new InvalidOperationException("Bad audio role accepted.");}catch(InvalidDataException e){errors.Add(e.ToString());}
        return new {disposed=!GodotObject.IsInstanceValid(first),reloaded=valid,size=reloaded.GetSize().ToString(),errors};
    }
    public static async Task<object> Crab()
    {
        await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Boss,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.KaiserCrabBoss>().ToMutable(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();return new {entered=true};
    }
    public static async Task<object> Paid(RunState run,string key,bool upgrade,bool left)
    {
        var player=PlayerBloodlines.LocalPlayer!;var combat=player.Creature.CombatState!;
        var enemies=combat.GetOpponentsOf(player.Creature).Where(e=>e.IsAlive).ToArray();
        var target=enemies.FirstOrDefault(e=>left?e.HasPower<BackAttackLeftPower>():e.HasPower<BackAttackRightPower>())??enemies[0];
        foreach(var e in enemies){e.SetMaxHpInternal(10000);e.SetCurrentHpInternal(10000);}
        var card=combat.CreateCard(NativeCards.Get(key),player);if(upgrade)CardCmd.Upgrade(card);
        await CardPileCmd.Add(card,PileType.Hand,skipVisuals:false);await PlayerCmd.GainEnergy(10,player);
        var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);var before=player.PlayerCombatState!.Energy;
        var accepted=card.TryManualPlay(card.TargetType==TargetType.AnyEnemy?target:null);
        var samples=new List<object>();var until=Time.GetTicksMsec()+15000;
        while(card.Pile?.Type is PileType.Hand or PileType.Play || MegaCrit.Sts2.Core.Nodes.Cards.NCard.FindOnTable(card)!=null) {
            if(Time.GetTicksMsec()>until)throw new TimeoutException("Presentation paid play did not finish.");
            samples.Add(Facing(player));await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
        }
        return new {key,upgrade,left,accepted,cost,energySpent=before-player.PlayerCombatState.Energy,pile=card.Pile?.Type.ToString(),samples,after=Facing(player)};
    }
    private static object Facing(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        var node=NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;var sprite=node.Visuals.GetNode<HeirSprite>("SpireLegacyHeir");
        return new {owner=player.NetId,cls=sprite.ClassId,nativeX=node.Body.Scale.X,spriteX=sprite.Scale.X,pose=sprite.Animation.ToString(),facing=player.Creature.GetPower<SurroundedPower>()?.Facing.ToString(),events=sprite.Events.ToArray()};
    }
    public static async Task<object> Trail(string cls)
    {
        var card=new Control {Position=new(300,300)};Tree.Root.AddChild(card);
        var trail=NCardTrailVfx.Create(card,HeirPresentation.TrailFor(cls))!;Tree.Root.AddChild(trail);
        var samples=new List<object>();var started=Time.GetTicksMsec();
        try {
            for(var i=0;i<40;i++) {
                card.Position+=new Vector2(15,0);await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
                var ribbon=NativeTestBridge.Descendants(trail).OfType<Line2D>().Single();
                var parent=(Node2D)ribbon.GetParent();
                samples.Add(new {milliseconds=Time.GetTicksMsec()-started,points=ribbon.GetPointCount(),alpha=parent.Modulate.A*trail.Modulate.A,scale=ribbon.GlobalScale.ToString(),parent=parent.Name.ToString()});
            }
            await trail.FadeOut();return new {cls,samples};
        }finally {if(GodotObject.IsInstanceValid(card))card.QueueFree();if(GodotObject.IsInstanceValid(trail))trail.QueueFree();}
    }
}
