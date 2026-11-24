using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

/// <summary>Per-class lines on shared family cards (sheets/class_reactions.json).</summary>
internal static class ClassReactions
{
    public static string? ClassOf(CardModel card) => card.IsMutable && card.Owner != null ? PlayerBloodlines.Heir(card.Owner).classId : null;
    public static bool Is(CardModel card, string key, string classId) => card is LegacyCard legacy && legacy.Key == key && ClassOf(card) == classId;

    public static async Task OnPlay(LegacyCard card, PlayerChoiceContext context, CardPlay play)
    {
        if (card.DesignCard.Reaction is null || ClassOf(card) is not {} cls) return;
        var player = card.Owner; var n = card.DesignCard.ReactionAmount(cls); var counter = player.Creature.GetPower<P_class>();
        switch (card.Key, cls)
        {
            // Riders skip Dexterity: the Manor already scales the card's printed Block.
            case ("family_motto" or "heirloom_blade" or "knight_clean_armor" or "banner_volley" or "birthright", "knight"):
                await CreatureCmd.GainBlock(player.Creature, n, ValueProp.Unpowered, null); break;
            case ("family_motto" or "knight_clean_armor", "mage"):
                if (counter != null) counter.Charges += n; break;
            case ("heirloom_blade", "mage") or ("knight_clean_armor", "ranger"):
                await CardPileCmd.Draw(context, n, player); break;
            // Banner Volley's own play adds the final step in P_class.AfterCardPlayed.
            case ("family_motto", "ranger"):
                if (counter != null) await counter.AdvanceRhythm(context, card, play, n); break;
            case ("banner_volley", "ranger"):
                if (counter != null) await counter.AdvanceRhythm(context, card, play, n - 1); break;
        }
    }

    public static decimal DamageBonus(LegacyCard card)
    {
        if (card.DesignCard.Reaction is null || ClassOf(card) is not {} cls || card.Owner.Creature.GetPower<P_class>() is not {} counter) return 0;
        var n = card.DesignCard.ReactionAmount(cls);
        return (card.Key, cls) switch
        {
            ("heirloom_blade", "ranger") when counter.AttacksThisTurn >= 2 => n,
            ("banner_volley", "mage") => Math.Min(8, counter.Charges * n),
            ("birthright", "mage") => Math.Min(10, counter.Charges * n),
            _ => 0
        };
    }
}
