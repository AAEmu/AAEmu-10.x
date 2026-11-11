using System.Collections.Concurrent;

namespace AAEmu.Game.Models.Game.Indun;

/// <summary>
/// Runs one character's dungeon ticket purchase (check, pay, register) as a unit, so two requests at once cannot
/// both pass the check and both pay.
/// </summary>
public sealed class IndunTicketPurchase
{
    private readonly ConcurrentDictionary<uint, object> _gates = new();

    public bool Run(uint characterId, Func<bool> canBuy, Func<bool> pay, Action register)
    {
        lock (_gates.GetOrAdd(characterId, _ => new object()))
        {
            if (!canBuy() || !pay())
                return false;

            register();
            return true;
        }
    }
}
