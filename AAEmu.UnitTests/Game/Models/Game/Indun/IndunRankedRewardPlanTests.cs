using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

public class IndunRankedRewardPlanTests
{
    private static readonly IndunRewardRecipient A = new(1001, "A");
    private static readonly IndunRewardRecipient B = new(1002, "B");
    private static readonly IndunRewardRecipient C = new(1003, "C");

    private static IReadOnlyDictionary<ulong, IReadOnlyDictionary<uint, uint>> Points(params (ulong Character, uint Points)[] rows)
        => rows.ToDictionary(r => r.Character, r => (IReadOnlyDictionary<uint, uint>)new Dictionary<uint, uint> { [5] = r.Points });

    [Test]
    public async Task Build_RanksEachRecipientByTheirGainPoints()
    {
        var plan = IndunRankedRewardPlan.Build([A, B, C], Points((1001, 1), (1002, 5), (1003, 3)));

        await Assert.That(plan.Entries.Select(e => (e.Recipient.Id, e.Rank)).ToArray())
            .IsEquivalentTo([(1002u, 1), (1003u, 2), (1001u, 3)]);
    }

    [Test]
    public async Task APlannedKind_KeepsItsRecipientsAndRanksOnARetry()
    {
        var plans = new IndunRankedRewardPlans();
        var first = plans.GetOrCreate(7, () => IndunRankedRewardPlan.Build([A, B, C], Points((1001, 1), (1002, 5), (1003, 3))));

        // C left and A moved up: a rebuilt plan would rank A 2nd and drop C.
        var retry = plans.GetOrCreate(7, () => IndunRankedRewardPlan.Build([A, B], Points((1001, 1), (1002, 5))));

        await Assert.That(retry).IsSameReferenceAs(first);
        await Assert.That(retry.Entries.Single(e => e.Recipient.Id == 1001).Rank).IsEqualTo(3);
        await Assert.That(retry.Entries.Any(e => e.Recipient.Id == 1003)).IsTrue();
    }

    [Test]
    public async Task DifferentKinds_GetTheirOwnPlans()
    {
        var plans = new IndunRankedRewardPlans();

        var one = plans.GetOrCreate(7, () => IndunRankedRewardPlan.Build([A], Points()));
        var two = plans.GetOrCreate(8, () => IndunRankedRewardPlan.Build([B], Points()));

        await Assert.That(two).IsNotSameReferenceAs(one);
    }
}
