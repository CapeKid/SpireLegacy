using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace SpireLegacy;

public static class GhostLuggage
{
    // The history owns card-copy references and is cleared with the combat.
    // Its last-player-turn query also spans the enemy turn, so check the side
    // explicitly rather than accepting enemy-turn discards.
    private static readonly System.Reflection.PropertyInfo Side = AccessTools.Property(typeof(CombatHistoryEntry), "CurrentSide");
    public static HashSet<CardModel> Candidates(Player player) => CombatManager.Instance.History.Entries
        .OfType<CardDiscardedEntry>()
        .Where(e => e.Card.Owner == player && e.HappenedLastPlayerTurn(player)
            && (CombatSide)Side.GetValue(e)! == player.Creature.Side)
        .Select(e => e.Card)
        .Where(c => c.Type == CardType.Attack && c.Pile?.Type == PileType.Discard).ToHashSet();

    public static async Task Recover(LegacyPower power, PlayerChoiceContext context, Player player)
    {
        var turn = player.PlayerCombatState!.TurnNumber;
        if (power.GhostRecoveryTurn == turn) return;
        power.GhostRecoveryTurn = turn; // Claim before any asynchronous choices.
        var eligible = Candidates(player);
        var prompt = new LocString("cards", ModelDb.Card<C_ranger_ghost_luggage>().Id.Entry + ".attackReturnPrompt");
        // Each stacked copy recovers one Attack. A canceled/full-hand recovery
        // is consumed and does not retry later in the same turn.
        for (var i = 0; i < power.Amount; i++) {
            if (!eligible.Any(c => c.Owner == player && c.Pile?.Type == PileType.Discard)) break;
            var chosen = await CardChoiceLifetime.Run(player, () => CardSelectCmd.FromCombatPile(context,
                PileType.Discard.GetPile(player), player, new CardSelectorPrefs(prompt, 0, 1),
                c => c.Owner == player && c.Type == CardType.Attack && eligible.Contains(c)));
            if (chosen.Count() == 0) break;
            foreach (var card in chosen) {
                if (!eligible.Remove(card) || card.Owner != player || card.Pile?.Type != PileType.Discard) continue;
                await CardPileCmd.Add(card, PileType.Hand);
                if (card.Pile?.Type != PileType.Hand || card.EnergyCost.CostsX) continue;
                var reduction = Math.Min(1, Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All)));
                if (reduction > 0) card.EnergyCost.AddThisTurn(-reduction);
            }
        }
    }
}
