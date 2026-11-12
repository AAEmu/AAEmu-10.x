using System.Collections.Concurrent;

namespace AAEmu.Game.Models.Game.Indun;

/// <summary>
/// Runs one character's dungeon ticket purchase (check, store, pay, register) as a unit, so two requests at once
/// cannot both pass the check and both pay. The pass is stored before the ticket is taken and the stored row is
/// removed again if payment fails, so a paid pass is never unstored.
/// </summary>
public sealed class IndunTicketPurchase
{
    private readonly ConcurrentDictionary<uint, object> _gates = new();

    public bool Run(uint characterId, Func<bool> canBuy, Func<bool> pay, Action register) =>
        Run(characterId, canBuy, () => true, pay, () => { }, register);

    public bool Run(uint characterId, Func<bool> canBuy, Func<bool> store, Func<bool> pay, Action unstore, Action register)
    {
        lock (_gates.GetOrAdd(characterId, _ => new object()))
        {
            if (!canBuy() || !store())
                return false;

            if (!pay())
            {
                unstore();
                return false;
            }

            register();
            return true;
        }
    }
}
