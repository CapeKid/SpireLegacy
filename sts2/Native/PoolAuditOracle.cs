using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace SpireLegacy;

// Disposable lab fixtures only; all entry points are behind the native test bridge.
public static class PoolAuditOracle
{
    private static void Require(bool value,string message) {if(!value)throw new InvalidDataException(message);}
    public static object Registry()
    {
        var cards=ModelDb.AllCards.ToArray();var pools=ModelDb.AllCardPools.ToArray();
        Require(cards.Select(c=>c.Id).Distinct().Count()==cards.Length,"Duplicate registration ID.");
        Require(pools.Select(p=>p.Id).Distinct().Count()==pools.Length,"Duplicate pool ID.");
        var historical=Design.Rows("cards").Select(r=>NativeCards.Get(r.Text("id"))).ToArray();
        Require(historical.All(c=>!c.IsMutable&&cards.Single(m=>m.Id==c.Id)==c&&ModelDb.GetById<CardModel>(c.Id)==c),"Historical lookup/canonical identity changed.");
        Require(pools.Where(p=>p is not FamilyCardPool).All(p=>p.AllCards.All(c=>c is not LegacyCard)),"Heir cards contaminated a native pool.");
        var detail=pools.OfType<FamilyCardPool>().Select(p=>new{pool=p.Id.ToString(),cards=p.AllCards.Cast<LegacyCard>().Select(c=>new{key=c.Key,rarity=c.Rarity.ToString(),mutable=c.IsMutable}).ToArray()}).ToArray();
        Require(detail.All(p=>p.cards.All(c=>!c.mutable)),"Mutable pool canonical.");
        return new{expanded=NativePoolParity.Expanded,historical=historical.Length,total=cards.Length,pools=pools.Length,detail};
    }
    public static object Owner(RunState run)
    {
        var checks=new List<object>();
        foreach(var player in run.Players)
        {
            var allowed=player.Character.CardPool.GetUnlockedCards(player.UnlockState,player.RunState.CardMultiplayerConstraint).Where(c=>c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare).ToArray();
            var ids=allowed.Select(c=>c.Id).ToHashSet();var rewards=new List<object>();var shops=new List<object>();
            var canon=allowed.Select(c=>new{c.Id,c.IsUpgraded,damage=c.DynamicVars.Damage.BaseValue,block=c.DynamicVars.Block.BaseValue}).ToArray();
            foreach(var room in new[]{RoomType.Monster,RoomType.Elite,RoomType.Boss})
                foreach(var count in new[]{1,3,6,12})
                    for(var repeat=0;repeat<5;repeat++)
                    {
                        var reward=new CardReward(CardCreationOptions.ForRoom(player,room),count,player);reward.Populate();
                        var cards=reward.Cards.ToArray();Require(cards.Length==count&&cards.Select(c=>c.Id).Distinct().Count()==count,"Reward count/uniqueness.");
                        Require(cards.All(c=>c.Owner==player&&c.IsMutable&&ids.Contains(c.Id)),"Reward owner/pool leaked.");
                        Require(room!=RoomType.Boss||cards.All(c=>c.Rarity==CardRarity.Rare),"Boss reward rarity changed.");
                        rewards.Add(new{room=room.ToString(),count,repeat,ids=cards.Select(c=>c.Id.ToString()).ToArray(),upgraded=cards.Count(c=>c.IsUpgraded)});
                        foreach(var card in cards)run.RemoveCard(card);
                    }
            for(var repeat=0;repeat<10;repeat++)
            {
                var shop=MerchantInventory.CreateForNormalMerchant(player);var cards=shop.CharacterCardEntries.Select(e=>e.CreationResult!.Card).ToArray();
                Require(cards.Length==5&&cards.All(c=>c.Owner==player&&c.IsMutable&&ids.Contains(c.Id)),"Merchant owner/pool leaked.");
                Require(cards.Select(c=>c.Type).SequenceEqual(new[]{CardType.Attack,CardType.Attack,CardType.Skill,CardType.Skill,CardType.Power}),"Merchant type slots changed.");
                Require(shop.ColorlessCardEntries.All(e=>e.CreationResult!.Card is not LegacyCard),"Heir colorless shop contamination.");
                shops.Add(new{repeat,ids=cards.Select(c=>c.Id.ToString()).ToArray()});
                foreach(var entry in shop.CardEntries)run.RemoveCard(entry.CreationResult!.Card);
            }
            var transforms=new List<object>();
            foreach(var card in player.Deck.Cards)
            {
                var choices=CardFactory.GetDefaultTransformationOptions(card,false).ToArray();
                Require(choices.Length>0&&choices.All(c=>ids.Contains(c.Id)&&c.Id!=card.Id),"Transformation pool leaked.");
                var created=CardFactory.CreateRandomCardForTransform(card,false,player.PlayerRng.Rewards);
                Require(created.Owner==player&&ids.Contains(created.Id)&&created.Id!=card.Id,"Transform result owner/pool.");
                transforms.Add(new{original=card.Id.ToString(),created=created.Id.ToString(),choices=choices.Length});run.RemoveCard(created);
            }
            Require(JsonSerializer.Serialize(canon)==JsonSerializer.Serialize(allowed.Select(c=>new{c.Id,c.IsUpgraded,damage=c.DynamicVars.Damage.BaseValue,block=c.DynamicVars.Block.BaseValue})),"Reward/shop/transform mutated canonical values.");
            checks.Add(new{player.NetId,cls=player.Character is HeirCharacter?PlayerBloodlines.Heir(player).classId:"native",pool=player.Character.CardPool.Id.ToString(),allowed=allowed.Select(c=>c is LegacyCard legacy?legacy.Key:c.Id.ToString()).ToArray(),rewards,shops,transforms,canonicalUnchanged=true});
        }
        return checks;
    }
    public static object Sparse(RunState run)
    {
        var player=run.Players.Single();var allowed=player.Character.CardPool.GetUnlockedCards(player.UnlockState,player.RunState.CardMultiplayerConstraint).Where(c=>c.Rarity==CardRarity.Common).ToArray();var checks=new List<object>();
        foreach(var size in new[]{0,1,2,6})foreach(var count in new[]{0,1,3,6,7})
        {
            var options=CardCreationOptions.ForNonCombatWithUniformOdds([player.Character.CardPool],c=>allowed.Take(size).Contains(c));
            var started=Time.GetTicksMsec();string? diagnostic=null;CardModel[] cards=[];
            try{cards=CardFactory.CreateForReward(player,count,options).Select(r=>r.Card).ToArray();}
            catch(Exception error){diagnostic=DiagnosticText.Describe(error,()=>"Sparse reward size="+size+", requested="+count);}
            var elapsed=Time.GetTicksMsec()-started;
            Require(elapsed<2000,"Sparse reward selection was not bounded.");
            Require(count<=size?diagnostic==null&&cards.Length==count&&cards.Select(c=>c.Id).Distinct().Count()==count:diagnostic!=null,"Sparse reward contract changed.");
            checks.Add(new{size,count,elapsed,offered=cards.Select(c=>c.Id.ToString()).ToArray(),diagnostic});foreach(var card in cards)run.RemoveCard(card);
        }
        return checks;
    }
    public static object Historical(RunState run,bool import)
    {
        var player=run.Players.Single();var path=Path.Combine(Runtime.DataDirectory,"pool-cards.json");var checks=new List<object>();var saves=new List<SerializableCard>();
        if(import)saves=JsonSerializer.Deserialize(File.ReadAllText(path),JsonSerializationUtility.GetTypeInfo<List<SerializableCard>>())!;
        else foreach(var row in Design.Rows("cards"))foreach(var upgraded in new[]{false,true})
        {
            var card=run.CreateCard(NativeCards.Get(row.Text("id")),player);if(upgraded)CardCmd.Upgrade(card);saves.Add(card.ToSerializable());run.RemoveCard(card);
        }
        foreach(var saved in saves)
        {
            var card=(LegacyCard)run.LoadCard(saved,player);
            Require(card.Owner==player&&card.Pool==player.Character.CardPool&&card.Id==saved.Id,"Saved ID/pool/owner changed.");
            var round=card.ToSerializable();Require(round.Id==saved.Id&&card.IsUpgraded==(saved.CurrentUpgradeLevel>0),"Saved upgrade changed.");
            checks.Add(new{key=card.Key,upgrade=card.IsUpgraded,pool=card.Pool.Id.ToString()});run.RemoveCard(card);
        }
        if(!import)File.WriteAllText(path,JsonSerializer.Serialize(saves,JsonSerializationUtility.GetTypeInfo<List<SerializableCard>>()));
        return new{import,checks};
    }
    public static async Task<object> Library(RunState? run)
    {
        var tree=(SceneTree)Engine.GetMainLoop();var library=NCardLibrary.Create()!;tree.Root.AddChild(library);library.Initialize(run!);
        try
        {
            library.OnSubmenuOpened();for(var i=0;i<20;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            var rendered=NativeTestBridge.Descendants(library).OfType<MegaCrit.Sts2.Core.Nodes.Cards.NCard>().Select(c=>c.Model).Where(c=>c!=null).ToArray();
            Require(rendered.Length>0,"Compendium has no rendered cards.");
            return new{rendered=rendered.Length,legacy=rendered.OfType<LegacyCard>().Select(c=>c.Key).Distinct().ToArray(),labels=NativeTestBridge.Descendants(library).OfType<Label>().Select(l=>l.Text).ToArray()};
        }
        finally {library.OnSubmenuClosed();library.QueueFree();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
    }
}
