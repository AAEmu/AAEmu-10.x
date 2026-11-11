using AAEmu.Game.Models.Game.Rankings;

namespace AAEmu.UnitTests.Game.Models.Game.Rankings;

public class RankRecordsTests
{
    [Test]
    [Arguments(3u, RankRecordKind.FishWeight)] // fishing_sum: everything the window caught weighs in
    [Arguments(4u, RankRecordKind.FishLength)] // fishing_top: only the longest catch stands
    public async Task ForBoardKind_ReadsTheRecordABoardRanks(uint boardKind, RankRecordKind expected)
    {
        await Assert.That(RankRecordRules.ForBoardKind(boardKind)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(9u)]   // character_gear_score: read off the character, not recorded
    [Arguments(11u)]  // instance_rating: nothing produces it yet
    [Arguments(15u)]  // zone score: nothing produces it yet
    [Arguments(17u)]  // game points: counted through their own running totals
    public async Task ForBoardKind_IsNullForABoardNothingRecords(uint boardKind)
    {
        await Assert.That(RankRecordRules.ForBoardKind(boardKind)).IsNull();
    }

    [Test]
    public async Task AggregateOf_KeepsTheBestOfAWindowOrItsTotal()
    {
        await Assert.That(RankRecordRules.AggregateOf(RankRecordKind.FishLength)).IsEqualTo(RankRecordAggregate.Best);
        await Assert.That(RankRecordRules.AggregateOf(RankRecordKind.FishWeight)).IsEqualTo(RankRecordAggregate.Total);
    }

    [Test]
    public async Task Add_KeepsEveryFigureWaitingToBeWritten()
    {
        var records = new RankRecords();
        await Assert.That(records.HasPending).IsFalse();

        var first = new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
        var second = first.AddMinutes(5);
        records.Add(RankRecordKind.FishLength, 249, first);
        records.Add(RankRecordKind.FishWeight, 448, first);
        records.Add(RankRecordKind.FishLength, 312, second);

        await Assert.That(records.HasPending).IsTrue();
        await Assert.That(records.Pending.Count).IsEqualTo(3);

        // the order and the moment matter for a best: the window shows when the standing figure was recorded
        await Assert.That(records.Pending[0].Kind).IsEqualTo(RankRecordKind.FishLength);
        await Assert.That(records.Pending[0].Value).IsEqualTo(249L);
        await Assert.That(records.Pending[2].Value).IsEqualTo(312L);
        await Assert.That(records.Pending[2].RecordedAtUtc).IsEqualTo(second);

        records.Clear();
        await Assert.That(records.HasPending).IsFalse();
    }

    [Test]
    public async Task Add_IgnoresAFigureOfNothing()
    {
        var records = new RankRecords();
        records.Add(RankRecordKind.FishLength, 0, DateTime.UtcNow);
        records.Add(RankRecordKind.FishWeight, -3, DateTime.UtcNow);

        await Assert.That(records.HasPending).IsFalse();
    }

    [Test]
    public async Task ResetTo_ReplacesTheRecordsRatherThanAddingToThem()
    {
        // A save can fail before it cleared anything: putting the copy back must not count a figure twice.
        var records = new RankRecords();
        var caught = new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
        records.Add(RankRecordKind.FishWeight, 448000, caught);
        List<RankRecordEvent> before = [.. records.Pending];

        records.ResetTo(before);
        await Assert.That(records.Pending.Count).IsEqualTo(1);

        records.Clear();
        records.ResetTo(before);
        await Assert.That(records.Pending.Count).IsEqualTo(1);
        await Assert.That(records.Pending[0]).IsEqualTo(new RankRecordEvent(RankRecordKind.FishWeight, 448000, caught));
    }

    [Test]
    public async Task GamePointTotals_ResetTo_ReplacesTheTotalsRatherThanAddingToThem()
    {
        var totals = new GamePointTotals();
        totals.Add(RankGamePoints.Honor, RankGamePoints.Gained, 700);
        var before = new Dictionary<(int Kind, int Method), long>(totals.Pending);

        totals.ResetTo(before);
        await Assert.That(totals.Pending[(RankGamePoints.Honor, RankGamePoints.Gained)]).IsEqualTo(700L);

        totals.Clear();
        totals.ResetTo(before);
        await Assert.That(totals.Pending[(RankGamePoints.Honor, RankGamePoints.Gained)]).IsEqualTo(700L);
        await Assert.That(totals.Pending.Count).IsEqualTo(1);
    }
}
