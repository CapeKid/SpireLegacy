using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

internal static class RelicReworkOracle
{
    internal static bool Handles(string key) => key is "triune_crest" or "family_seal" or "legacy_hourglass" or
        "guardian_knot" or "scholar_bookmark" or "trail_compass";

    internal static async Task<object> Verify(RunState run, string key, bool up)
    {
        var player = run.Players.Single();
        var owner = player.Creature;
        var combat = owner.CombatState!;
        var state = player.PlayerCombatState!;
        await Until(() => state.Phase == PlayerTurnPhase.Play && !RunManager.Instance.ActionExecutor.IsRunning);
        var enemy = combat.GetOpponentsOf(owner).First(e => e.IsAlive);
        var context = new BlockingPlayerChoiceContext();
        var cls = PlayerBloodlines.Heir(player).classId;
        var relic = player.Relics.OfType<FamilyDropRelic>().SingleOrDefault(item => item.Key == key)
            ?? (FamilyDropRelic)HeirDropRelics.Get(key).ToMutable();
        if (!player.Relics.Contains(relic)) await RelicCmd.Obtain(relic, player);
        owner.SetMaxHpInternal(10000);
        owner.SetCurrentHpInternal(10000);
        foreach (var target in combat.GetOpponentsOf(owner))
        {
            target.SetMaxHpInternal(10000);
            target.SetCurrentHpInternal(10000);
        }
        foreach (var card in state.Hand.Cards.ToArray()) await CardPileCmd.Add(card, PileType.Draw, skipVisuals: true);

        async Task<CardModel> Add(string id, PileType pile = PileType.Hand)
        {
            var card = combat.CreateCard(NativeCards.Get(id), player);
            if (up) CardCmd.Upgrade(card);
            await CardPileCmd.Add(card, pile, CardPilePosition.Top, skipVisuals: true);
            return card;
        }
        async Task<int> Play(string id)
        {
            var card = await Add(id);
            var previous = CardPlayDiagnostics.LastFailure;
            Require(card.TryManualPlay(card.TargetType == TargetType.AnyEnemy ? enemy : null),
                $"Native play of {id} was refused at {state.Phase} with {state.Energy} Energy.");
            await Until(() => card.Pile?.Type is not (PileType.Hand or PileType.Play) && !RunManager.Instance.ActionExecutor.IsRunning);
            Require(previous == CardPlayDiagnostics.LastFailure, "Relic play raised an error.");
            return CombatManager.Instance.History.Entries.OfType<CardPlayFinishedEntry>().Count(e => e.CardPlay.Card == card);
        }
        async Task NextTurn()
        {
            var turn = state.TurnNumber;
            CombatManager.Instance.SetReadyToEndTurn(player, false);
            await Until(() => state.Phase == PlayerTurnPhase.Play && state.TurnNumber > turn);
        }
        async Task RoundTrip()
        {
            var restored = (FamilyDropRelic)RelicModel.FromSerializable(relic.ToSerializable());
            Require(restored.CombatClaimed == relic.CombatClaimed && restored.TypeMask == relic.TypeMask &&
                restored.HourglassPending == relic.HourglassPending, "Relic progress did not round-trip.");
            await RelicCmd.Replace(relic, restored);
            relic = restored;
        }

        switch (key)
        {
            case "triune_crest":
                await PlayerCmd.GainEnergy(20, player);
                if (cls == "knight")
                {
                    await Play("knight_strike");
                    Require(!relic.CombatClaimed, "Unprimed Attack triggered Crest.");
                    await Play("knight_guard");
                    var block = owner.Block;
                    await Play("knight_strike");
                    Require(owner.Block == block + 8, "Counterguard did not grant 8 Block.");
                    await Play("knight_guard");
                    block = owner.Block;
                    await Play("knight_strike");
                    Require(owner.Block == block, "Counterguard paid twice.");
                }
                else if (cls == "mage")
                {
                    await Play("mage_guard");
                    await Play("mage_strike");
                    Require(!relic.CombatClaimed, "A single charge triggered Crest.");
                    await NextTurn();
                    await PlayerCmd.GainEnergy(20, player);
                    await Play("mage_guard");
                    await Play("mage_guard");
                    var energy = state.Energy;
                    await Play("mage_strike");
                    Require(state.Energy == energy, "Fully charged Arcane Flow did not refund 1 Energy.");
                }
                else
                {
                    for (var i = 1; i <= 6; i++)
                    {
                        await Play("ranger_strike");
                        var vulnerable = enemy.GetPowerAmount<VulnerablePower>();
                        Require(vulnerable == (i < 3 ? 0 : i < 6 ? 3 : 4), "Hunter Rhythm did not apply the expected Vulnerable.");
                    }
                }
                break;
            case "family_seal":
                await Play(cls + "_strike");
                await NextTurn();
                await Play(cls + "_guard");
                await Play("knight_forge_memory");
                Require(relic.TypeMask == 7 && !relic.CombatClaimed, "Family Seal did not retain the first turn's Attack.");
                await RoundTrip();
                await PlayerCmd.GainEnergy(4, player);
                Require(await Play(cls + "_guard") == 2 && await Play(cls + "_guard") == 1, "Family Seal did not replay exactly once.");
                break;
            case "legacy_hourglass":
                await PlayerCmd.LoseEnergy(state.Energy - 1, player);
                await NextTurn();
                Require(!relic.CombatClaimed && state.Energy == 3, "Hourglass triggered with only 1 unspent Energy.");
                await NextTurn();
                Require(relic.CombatClaimed && state.Energy == 6 && state.Hand.Cards.Count == 7, "Hourglass did not give 3 Energy and draw 2.");
                await NextTurn();
                Require(state.Energy == 3 && state.Hand.Cards.Count == 5, "Hourglass paid twice.");
                break;
            case "guardian_knot":
                await CreatureCmd.Damage(context, owner, 4, ValueProp.Move, enemy);
                Require(!relic.CombatClaimed, "Unblocked hit triggered Guardian Knot.");
                await CreatureCmd.GainBlock(owner, 12, ValueProp.Unpowered, null);
                await CreatureCmd.Damage(context, owner, 4, ValueProp.Move, enemy);
                await CreatureCmd.Damage(context, owner, 4, ValueProp.Move, enemy);
                Require(owner.GetPowerAmount<EnergyNextTurnPower>() == 1, "Guardian Knot did not queue exactly 1 Energy.");
                await NextTurn();
                Require(state.Energy == 4, "Guardian Knot did not pay out on the next turn.");
                break;
            case "scholar_bookmark":
                await Add("knight_forge_memory", PileType.Draw);
                var discounted = (await CardPileCmd.Draw(context, 1, player)).Single();
                Require(discounted.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "First Power did not get discounted.");
                await Add("knight_forge_memory", PileType.Draw);
                var fullPrice = (await CardPileCmd.Draw(context, 1, player)).Single();
                Require(fullPrice.EnergyCost.GetWithModifiers(CostModifiers.All) == 1, "Second Power was discounted.");
                await NextTurn();
                Require(discounted.EnergyCost.GetWithModifiers(CostModifiers.All) == 1, "Power discount persisted past the turn.");
                break;
            case "trail_compass":
                await PlayerCmd.GainEnergy(4, player);
                await Play("knight_strike");
                Require(!relic.CombatClaimed, "Unmarked hit triggered Trail Compass.");
                await PowerCmd.Apply<VulnerablePower>(context, enemy, 2, owner, null);
                var hand = state.Hand.Cards.Count;
                await Play("knight_strike");
                Require(state.Hand.Cards.Count == hand + 2, "A Vulnerable hit did not draw 2 cards.");
                hand = state.Hand.Cards.Count;
                await Play("knight_strike");
                // For a Ranger, the third Attack still draws normally through Hunter Rhythm.
                Require(state.Hand.Cards.Count == hand + (cls == "ranger" ? 1 : 0), "Trail Compass paid twice.");
                break;
        }
        Require(relic.CombatClaimed, "Relic failed to claim its combat use.");
        await RoundTrip();
        return new { key, up, cls, relic.CombatClaimed, relic.TypeMask, counter = relic.DisplayAmount, roundTrip = true, verified = true };
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidDataException(reason);
    }

    private static async Task Until(Func<bool> done)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var deadline = Time.GetTicksMsec() + 20000;
        while (!done())
        {
            if (Time.GetTicksMsec() > deadline) throw new TimeoutException("Relic rework fixture did not settle.");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
