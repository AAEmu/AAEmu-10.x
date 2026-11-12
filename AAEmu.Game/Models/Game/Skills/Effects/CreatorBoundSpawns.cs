using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects;

/// <summary>
/// <c>spawn_effects.despawn_on_creator_death</c> for spawns World authors: the spawn goes when its creator dies.
/// </summary>
public static class CreatorBoundSpawns
{
    /// <summary>
    /// Binds <paramref name="spawn"/> to <paramref name="creator"/>'s death. Returns a release for the creator's
    /// handler, which the spawn's own death also runs; an NPC spawn's deletion should run it too.
    /// </summary>
    public static Action Bind(Unit creator, Unit spawn, Action<Unit> despawn)
    {
        if (creator?.Events == null || spawn == null || despawn == null)
            return static () => { };

        EventHandler<OnDeathArgs> onSpawnDeath = null;
        EventHandler<OnDeathArgs> onCreatorDeath = null;
        void Release()
        {
            creator.Events.OnDeath -= onCreatorDeath;
            spawn.Events.OnDeath -= onSpawnDeath;
        }

        onCreatorDeath = (_, _) =>
        {
            Release();
            despawn(spawn);
        };
        onSpawnDeath = (_, _) => Release();
        creator.Events.OnDeath += onCreatorDeath;
        spawn.Events.OnDeath += onSpawnDeath;
        return Release;
    }
}
