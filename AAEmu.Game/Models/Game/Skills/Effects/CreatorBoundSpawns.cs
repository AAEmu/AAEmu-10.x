using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects;

/// <summary>
/// <c>spawn_effects.despawn_on_creator_death</c> for spawns World authors: the spawn goes when its creator dies.
/// </summary>
public static class CreatorBoundSpawns
{
    public static void Bind(Unit creator, Unit spawn, Action<Unit> despawn)
    {
        if (creator?.Events == null || spawn == null || despawn == null)
            return;

        EventHandler<OnDeathArgs> handler = null;
        handler = (_, _) =>
        {
            creator.Events.OnDeath -= handler;
            despawn(spawn);
        };
        creator.Events.OnDeath += handler;
    }
}
