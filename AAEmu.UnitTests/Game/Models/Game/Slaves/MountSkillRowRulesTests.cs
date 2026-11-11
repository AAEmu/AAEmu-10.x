using AAEmu.Game.Models.Game.Mate;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Slaves;

public class MountSkillRowRulesTests
{
    private const uint FireHumanCannon = 16650;

    // mount_skills in table order: the four cannon rows share skill 16650, 81 is the exit and 67 the
    // generic cannon with a skill of its own.
    private static readonly MountSkills[] Rows =
    [
        new() { Id = 67, SkillId = 15693 },
        new() { Id = 69, SkillId = FireHumanCannon },  // rider 15626, 1300 m
        new() { Id = 72, SkillId = FireHumanCannon },  // rider 15757, 1000 m to Golden Fable Harbor
        new() { Id = 74, SkillId = FireHumanCannon },  // rider 15623, 1080 m
        new() { Id = 81, SkillId = 16813 },
        new() { Id = 179, SkillId = FireHumanCannon }, // rider 20075, Loka
    ];

    [Test]
    [Arguments(68u, 69u)] // Blueglass (the three spawned cannons)
    [Arguments(67u, 69u)] // Golden Fable
    [Arguments(65u, 74u)] // Blueglass, 1080 m
    [Arguments(66u, 74u)] // Grenwolf
    [Arguments(71u, 72u)] // Golden Fable, 1000 m
    [Arguments(99u, 179u)] // Amed's cannon, Loka
    public async Task PickRow_TakesTheRowTheCannonCarries(uint slaveId, uint expectedRow)
    {
        // slave_mount_skills: each cannon carries one fire row and the exit row 81
        var carried = slaveId switch
        {
            65u or 66u => new uint[] { 74, 81 },
            67u or 68u => [69, 81],
            71u => [72, 81],
            99u => [179, 81],
            _ => []
        };

        await Assert.That(MountSkillRowRules.PickRow(FireHumanCannon, 0, Rows, carried)).IsEqualTo(expectedRow);
    }

    [Test]
    public async Task PickRow_NoLongerHandsEveryCannonTheFirstRow()
    {
        // the old lookup: first row naming 16650, i.e. 69 (1300 m) for Grenwolf too
        await Assert.That(MountSkillRowRules.PickRow(FireHumanCannon, 0, Rows, [74u, 81u])).IsNotEqualTo(69u);
    }

    [Test]
    public async Task PickRow_ClientRowCannotBorrowARowTheCarrierDoesNotHave()
    {
        // a Grenwolf cannon (74) asked for the Loka row (179)
        await Assert.That(MountSkillRowRules.PickRow(FireHumanCannon, 179, Rows, [74u, 81u])).IsEqualTo(74u);
    }

    [Test]
    public async Task PickRow_ClientRowPicksBetweenTheCarriersOwnRows()
    {
        await Assert.That(MountSkillRowRules.PickRow(FireHumanCannon, 72, Rows, [69u, 72u])).IsEqualTo(72u);
    }

    [Test]
    public async Task PickRow_SilentTablesTakeAValidClientRowOrTheFirstRow()
    {
        await Assert.That(MountSkillRowRules.PickRow(FireHumanCannon, 179, Rows, [])).IsEqualTo(179u);
        await Assert.That(MountSkillRowRules.PickRow(FireHumanCannon, 81, Rows, [])).IsEqualTo(69u);
        await Assert.That(MountSkillRowRules.PickRow(FireHumanCannon, 0, Rows, [])).IsEqualTo(69u);
    }

    [Test]
    public async Task PickRow_CarrierWithoutARowForTheSkillFallsBackToTheFirstRow()
    {
        await Assert.That(MountSkillRowRules.PickRow(FireHumanCannon, 0, Rows, [81u])).IsEqualTo(69u);
    }

    [Test]
    public async Task PickRow_UnknownSkillHasNoRow()
    {
        await Assert.That(MountSkillRowRules.PickRow(1, 69, Rows, [69u])).IsEqualTo(0u);
    }

    [Test]
    public async Task RiderHelperLeavesSeat_OnlyForASeatedRiderAndAnUnmountSkill()
    {
        var cannonJump = new SkillTemplate { Id = 15626, Unmount = true };
        var sailHelper = new SkillTemplate { Id = 28228, Unmount = false };

        await Assert.That(MountSkillRowRules.RiderHelperLeavesSeat(cannonJump, seatedOnCarrier: true)).IsTrue();
        await Assert.That(MountSkillRowRules.RiderHelperLeavesSeat(cannonJump, seatedOnCarrier: false)).IsFalse();
        await Assert.That(MountSkillRowRules.RiderHelperLeavesSeat(sailHelper, seatedOnCarrier: true)).IsFalse();
        await Assert.That(MountSkillRowRules.RiderHelperLeavesSeat(null, seatedOnCarrier: true)).IsFalse();
    }
}
