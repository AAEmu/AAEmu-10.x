using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects;

public class CreatorBoundSpawnsTests
{
    [Test]
    public async Task CreatorDeath_DespawnsItsBoundSpawnOnce()
    {
        // Island Songsong (22076) spawns its bug/crab waves with despawn_on_creator_death.
        var songsong = new Unit { ObjId = 899 };
        var bug = new Unit { ObjId = 912 };
        var despawned = new List<Unit>();

        CreatorBoundSpawns.Bind(songsong, bug, despawned.Add);
        songsong.Events.OnDeath(songsong, new OnDeathArgs { Victim = songsong });
        songsong.Events.OnDeath(songsong, new OnDeathArgs { Victim = songsong });

        await Assert.That(despawned).IsEquivalentTo(new[] { bug });
    }

    [Test]
    public async Task AnotherUnitsDeath_LeavesTheSpawn()
    {
        var songsong = new Unit { ObjId = 899 };
        var bug = new Unit { ObjId = 912 };
        var despawned = new List<Unit>();

        CreatorBoundSpawns.Bind(songsong, bug, despawned.Add);
        bug.Events.OnDeath(bug, new OnDeathArgs { Victim = bug });

        await Assert.That(despawned).IsEmpty();
    }
}
