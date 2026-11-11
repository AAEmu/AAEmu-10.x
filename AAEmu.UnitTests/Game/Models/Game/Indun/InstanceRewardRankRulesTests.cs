using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

public class InstanceRewardRankRulesTests
{
    // Same shape as IndunGainScoreboard.Snapshot(): character id -> gain rule -> points.
    private static IReadOnlyDictionary<ulong, IReadOnlyDictionary<uint, uint>> Points(
        params (ulong Character, uint Rule, uint Points)[] rows)
        => rows.GroupBy(r => r.Character)
            .ToDictionary(g => g.Key, g => (IReadOnlyDictionary<uint, uint>)g.ToDictionary(r => r.Rule, r => r.Points));

    [Test]
    public async Task HighestTotal_RanksFirst()
    {
        var ranks = InstanceRewardRankRules.Rank([1001, 1002, 1003],
            Points((1001, 5, 2), (1002, 5, 4), (1002, 9, 1), (1003, 6, 3)));

        await Assert.That(ranks[1002]).IsEqualTo(1);
        await Assert.That(ranks[1003]).IsEqualTo(2);
        await Assert.That(ranks[1001]).IsEqualTo(3);
    }

    [Test]
    public async Task NoPoints_RanksLast_AndTiesGoByCharacterId()
    {
        var ranks = InstanceRewardRankRules.Rank([1004, 1001, 1003], Points((1003, 5, 2), (1004, 5, 2)));

        await Assert.That(ranks[1003]).IsEqualTo(1);
        await Assert.That(ranks[1004]).IsEqualTo(2);
        await Assert.That(ranks[1001]).IsEqualTo(3);
    }
}
