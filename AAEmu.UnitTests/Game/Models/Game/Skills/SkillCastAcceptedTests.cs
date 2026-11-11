using System.Reflection;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Buffs;
using AAEmu.Game.Models.Game.Skills.Plots;
using AAEmu.Game.Models.Game.Skills.Static;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.UnitTests.Utils;
using AAEmu.UnitTests.Utils.Mocks;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

/// <summary>
/// <see cref="Skill.CastAccepted"/>: what a caller hangs on a cast that must only happen when the cast
/// really goes ahead, and before its plot starts.
/// </summary>
/// <remarks>
/// The rider helper of a vehicle skill leaves its seat there. The vehicle's own skill is cast as the
/// vehicle and the helper as the rider, so the rider can be refused after the vehicle fired: taking the
/// rider out of the seat ahead of the cast left a stunned or silenced rider standing next to a human
/// cannon that had not launched them.
/// </remarks>
[NotInParallel]
public class SkillCastAcceptedTests
{
    // 15626 is the 1300 m human-cannon jump: unmount, instant, self target, driven by its plot.
    private const uint RiderJumpSkillId = 15626;
    private const uint StunnedBuffId = 31544;
    private const uint SilencedBuffId = 2675;
    private const uint SleepingBuffId = 5695;

    private SingletonScope<BuffGameData> _buffGameData;
    private SingletonScope<SkillManager> _skills;
    private SingletonScope<UnitRequirementsGameData> _unitRequirements;

    [Before(Test)]
    public void InstallContentLookups()
    {
        // Adding a buff reads buff_modifiers (BuffGameData) and the buff's template and tags (SkillManager).
        var gameData = new BuffGameData();
        SetField(gameData, "_buffModifiers", new Dictionary<uint, List<BuffModifier>>());
        SetField(gameData, "_buffTolerances", new Dictionary<uint, BuffTolerance>());
        SetField(gameData, "_buffTolerancesById", new Dictionary<uint, BuffTolerance>());
        _buffGameData = new SingletonScope<BuffGameData>(gameData);

        var skillManager = TestManagers.CreateSkillManager();
        SetField(skillManager, "_buffs", new Dictionary<uint, BuffTemplate>
        {
            [StunnedBuffId] = new BuffTemplate { Id = StunnedBuffId, Duration = 0, Stun = true },
            [SilencedBuffId] = new BuffTemplate { Id = SilencedBuffId, Duration = 0, Silence = true },
            [SleepingBuffId] = new BuffTemplate { Id = SleepingBuffId, Duration = 0, Sleep = true }
        });
        SetField(skillManager, "_buffTags", new Dictionary<uint, List<uint>>());
        SetField(skillManager, "_taggedBuffs", new Dictionary<uint, List<uint>>());
        _skills = new SingletonScope<SkillManager>(skillManager);

        // unit_reqs with no rows: the cast has none to fail.
        var unitRequirements = new UnitRequirementsGameData();
        unitRequirements.SetForTest();
        _unitRequirements = new SingletonScope<UnitRequirementsGameData>(unitRequirements);
    }

    [After(Test)]
    public void RestoreContentLookups()
    {
        _unitRequirements.Dispose();
        _skills.Dispose();
        _buffGameData.Dispose();
    }

    private static void SetField(object target, string name, object value)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private static CharacterMock Rider() => new() { Id = 1, ObjId = 1, Name = "Rider", Hp = 100 };

    private static void Afflict(Unit unit, uint buffId)
    {
        var source = new Unit { ObjId = 2 };
        var template = SkillManager.Instance.GetBuffTemplate(buffId);
        unit.Buffs.AddBuff(new Buff(unit, source, new SkillCasterUnit(source.ObjId), template, null, DateTime.UtcNow)
        {
            Passive = true,
            AbLevel = 1
        });
    }

    /// <summary>The rider helper as <c>Unit.UseSkill</c> casts it: the rider's own cast, aimed at itself.</summary>
    private static SkillResult CastRiderJump(Unit rider, Skill skill)
    {
        var caster = SkillCaster.GetByType(SkillCasterType.Unit);
        caster.ObjId = rider.ObjId;
        var target = SkillCastTarget.GetByType(SkillCastTargetType.Unit);
        target.ObjId = rider.ObjId;

        return skill.Use(rider, caster, target, null, true, out _);
    }

    private static Skill RiderJump() => new(new SkillTemplate
    {
        Id = RiderJumpSkillId,
        Unmount = true,
        // use_condition_bits as shipped: source_alive and source_mount.
        UseConditionBits = 5,
        // A plot with no tree ends itself; plot_only keeps the test out of the instant-cast branch.
        Plot = new Plot { Id = 194 },
        PlotOnly = true
    });

    [Test]
    [Arguments(StunnedBuffId, SkillResult.CannotCastInStun)]
    [Arguments(SilencedBuffId, SkillResult.Silence)]
    [Arguments(SleepingBuffId, SkillResult.CannotCastInStun)] // sleep reuses the stun result
    public async Task ARefusedCast_NeverRunsIt(uint buffId, SkillResult refusal)
    {
        var rider = Rider();
        Afflict(rider, buffId);
        var skill = RiderJump();
        var leftTheSeat = 0;
        skill.CastAccepted = () => leftTheSeat++;

        var result = CastRiderJump(rider, skill);

        await Assert.That(result).IsEqualTo(refusal);
        await Assert.That(leftTheSeat).IsEqualTo(0);
    }

    [Test]
    public async Task AnAcceptedCast_RunsItOnce_BeforeThePlotStarts()
    {
        var rider = Rider();
        var skill = RiderJump();
        var leftTheSeat = 0;
        ushort timelineWhenLeaving = 0;
        ushort plotTimelineWhenLeaving = ushort.MaxValue;
        skill.CastAccepted = () =>
        {
            leftTheSeat++;
            timelineWhenLeaving = skill.TlId;
            plotTimelineWhenLeaving = skill.PlotCastTlId;
        };

        var result = CastRiderJump(rider, skill);

        await Assert.That(result).IsEqualTo(SkillResult.Success);
        await Assert.That(leftTheSeat).IsEqualTo(1);
        // The cast held its timeline already (every check had passed), and the plot had none yet.
        await Assert.That(timelineWhenLeaving).IsNotEqualTo((ushort)0);
        await Assert.That(plotTimelineWhenLeaving).IsEqualTo((ushort)0);
    }
}
