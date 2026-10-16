using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace SpireLegacy;

// Conservative test policy, not a change to player event options or effects.
public static class PilotEvents
{
    public sealed record Decision(EventOption Option,string Reason);
    public static Decision? Choose(EventModel? model,IReadOnlyList<EventOption> options,Player player,bool planned)
    {
        var safe=options.Where(o=>!o.IsLocked&&!(o.WillKillPlayer?.Invoke(player)??false)).ToArray();
        if(safe.Length==0)return null;
        if(!planned||model is not TabletOfTruth||model.IsFinished)return new(safe[0],"first-safe-option");
        var hp=player.Creature.CurrentHp;var max=player.Creature.MaxHp;
        var cost=model.DynamicVars["DecipherMaxHpLoss"].BaseValue;
        // One cheap random upgrade can be reasonable. Do not repeatedly buy
        // increasingly expensive upgrades or trade away a useful heal.
        var buy=cost>0&&cost<=3&&cost*10<max&&hp>=max*.75m&&max-hp<10&&player.Deck.Cards.Any(c=>c.IsUpgradable);
        var choice=buy?safe.FirstOrDefault(o=>o.TextKey.EndsWith(".DECIPHER_1",StringComparison.Ordinal))
            :safe.FirstOrDefault(o=>o.TextKey.EndsWith(".SMASH",StringComparison.Ordinal)||o.TextKey.EndsWith(".GIVE_UP",StringComparison.Ordinal));
        // An unexpected schema is unsupported; never guess a costly option.
        return choice==null?null:new(choice,buy?"tablet-one-cheap-upgrade":"tablet-preserve-health");
    }
}
