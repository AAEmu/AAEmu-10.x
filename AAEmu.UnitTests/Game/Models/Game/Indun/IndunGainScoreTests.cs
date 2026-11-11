using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

/// <summary>
/// Island of Abundance scoring: an instance_gain_rules row points at an instance_point_doodad_phase_changes row,
/// and a harvest that moves that doodad into that phase earns its harvester a point on the rule. The client's
/// mini scoreboard (instance_mini_scoreboards) shows the points keyed by gain rule id.
/// </summary>
public class IndunGainScoreTests
{
    private static readonly InstanceGainRule[] Rules =
    [
        new(5, 80, 1, 371, 5, InstanceGainRuleTargetType.InstancePointDoodadPhaseChange, 2),
        new(6, 80, 1, 371, 6, InstanceGainRuleTargetType.InstancePointDoodadPhaseChange, 2)
    ];

    private static readonly InstancePointDoodadPhaseChange[] Targets =
    [
        new(5, 18140, 52980),
        new(6, 18141, 52987)
    ];

    [Test]
    public async Task RulesFor_ScoringPhase_ReturnsItsRule()
    {
        var rules = IndunGainRules.RulesFor(18141, 52987, Rules, Targets);

        await Assert.That(rules).IsEquivalentTo(new uint[] { 6 });
    }

    [Test]
    public async Task RulesFor_OtherPhaseOfTheSameDoodad_ReturnsNothing()
    {
        // 52986 is the shining crop's usable phase; only its harvested phase 52987 scores.
        var rules = IndunGainRules.RulesFor(18141, 52986, Rules, Targets);

        await Assert.That(rules).IsEmpty();
    }

    [Test]
    public async Task Scoreboard_CountsPerCharacterAndRule()
    {
        var board = new IndunGainScoreboard();
        board.Add(1001, 5);
        board.Add(1001, 5);
        board.Add(1001, 6);
        board.Add(2002, 5);

        var snapshot = board.Snapshot();

        await Assert.That(snapshot[1001UL][5u]).IsEqualTo(2u);
        await Assert.That(snapshot[1001UL][6u]).IsEqualTo(1u);
        await Assert.That(snapshot[2002UL][5u]).IsEqualTo(1u);
    }
}
