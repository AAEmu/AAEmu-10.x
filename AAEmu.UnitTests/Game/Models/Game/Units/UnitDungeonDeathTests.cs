using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Indun;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.UnitTests.Utils;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

[NotInParallel]
public class UnitDungeonDeathTests
{
    private const uint CopyId = 4100;

    [Test]
    public async Task DeathInADungeon_RaisesOnDeathAndOnUnitKilledOnce()
    {
        using var worldScope = TestDungeonWorld.InstallWorldManager();
        using var skillScope = new SingletonScope<SkillManager>(TestManagers.CreateSkillManager());
        var world = TestDungeonWorld.CreateWorld(CopyId, 0, 410);
        world.Template.Id = 22;
        TestDungeonWorld.CreateDungeon(new IndunZone(), world);

        var unit = new Unit { ObjId = 965, Hp = 10, MaxHp = 10 };
        unit.Transform.InstanceId = CopyId;
        var deaths = 0;
        var kills = 0;
        unit.Events.OnDeath += (_, _) => deaths++;
        world.Events.OnUnitKilled += (_, _) => kills++;

        unit.ReduceCurrentHp(unit, 10);

        await Assert.That(deaths).IsEqualTo(1);
        await Assert.That(kills).IsEqualTo(1);
        world.Dispose();
    }
}
