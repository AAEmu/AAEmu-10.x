using AAEmu.Game.Models.Game.Items.Loots;
using TUnit.Assertions.Enums;

namespace AAEmu.UnitTests.Game.Models.Game.Items.Loots;

public class LootDiceRulesTests
{
    [Test]
    [Arguments((sbyte)12, (sbyte)87, (sbyte)40, "Belanthas")]
    [Arguments((sbyte)87, (sbyte)12, (sbyte)40, "Ayla")]
    [Arguments((sbyte)1, (sbyte)2, (sbyte)99, "Corin")]
    [Arguments((sbyte)50, (sbyte)-1, (sbyte)49, "Ayla")]
    public async Task PickWinner_TheHighestRollWins(sbyte ayla, sbyte belanthas, sbyte corin, string expected)
    {
        var rolls = Rolls(("Ayla", ayla), ("Belanthas", belanthas), ("Corin", corin));

        await Assert.That(LootDiceRules.PickWinner(rolls, _ => throw new InvalidOperationException("no tie")))
            .IsEqualTo(expected);
    }

    [Test]
    public async Task PickWinner_AlwaysTheHighestWhicheverOrderTheRollsCameIn()
    {
        // The old pick sorted ascending and kept every roll at or above the lowest, so any roller could win.
        for (var first = 0; first < 3; first++)
        {
            var named = new List<(string, sbyte)> { ("Low", 5), ("Middle", 60), ("High", 98) };
            var rotated = named.Skip(first).Concat(named.Take(first)).ToArray();

            await Assert.That(LootDiceRules.PickWinner(Rolls(rotated), count => count - 1)).IsEqualTo("High");
        }
    }

    [Test]
    public async Task PickWinner_NobodyWhenEverybodyPassed()
    {
        var rolls = Rolls(("Ayla", -1), ("Belanthas", -1));

        await Assert.That(LootDiceRules.PickWinner(rolls, _ => 0)).IsNull();
        await Assert.That(LootDiceRules.HighestRollers(rolls).Count).IsEqualTo(0);
    }

    [Test]
    public async Task PickWinner_ARollNeverAnsweredNeverWins()
    {
        var rolls = Rolls(("Ayla", 0), ("Belanthas", -1));

        await Assert.That(LootDiceRules.PickWinner(rolls, _ => 0)).IsNull();
    }

    [Test]
    [Arguments(0, "Ayla")]
    [Arguments(1, "Corin")]
    public async Task PickWinner_ATieIsSettledOnlyAmongThoseTiedOnTheHighest(int pick, string expected)
    {
        var rolls = Rolls(("Ayla", 70), ("Belanthas", 20), ("Corin", 70), ("Dalia", -1));
        var tiedCount = 0;

        var winner = LootDiceRules.PickWinner(rolls, count =>
        {
            tiedCount = count;
            return pick;
        });

        await Assert.That(tiedCount).IsEqualTo(2);
        await Assert.That(winner).IsEqualTo(expected);
    }

    [Test]
    public async Task PickWinner_ASingleHighestRollerNeedsNoTieBreak()
    {
        var rolls = Rolls(("Ayla", 70), ("Belanthas", 71));
        var asked = false;

        var winner = LootDiceRules.PickWinner(rolls, _ =>
        {
            asked = true;
            return 0;
        });

        await Assert.That(winner).IsEqualTo("Belanthas");
        await Assert.That(asked).IsFalse();
    }

    [Test]
    public async Task Roll_AsksForOneToAHundred_BothEndsIncluded()
    {
        (int Min, int Max) asked = default;

        var lowest = LootDiceRules.Roll((min, max) =>
        {
            asked = (min, max);
            return min;
        });
        // Random.Next excludes its upper bound, so the highest it can return is one below it.
        var highest = LootDiceRules.Roll((_, max) => max - 1);

        await Assert.That(asked).IsEqualTo((1, 101));
        await Assert.That(lowest).IsEqualTo((sbyte)1);
        await Assert.That(highest).IsEqualTo((sbyte)100);
    }

    [Test]
    public async Task Roll_WithARealRandom_StaysInOneToAHundred_AndReachesBothEnds()
    {
        var random = new Random(4021);
        var rolls = Enumerable.Range(0, 20_000).Select(_ => LootDiceRules.Roll(random.Next)).ToList();

        await Assert.That(rolls.Min()).IsEqualTo((sbyte)1);
        await Assert.That(rolls.Max()).IsEqualTo((sbyte)100);
    }

    [Test]
    public async Task PickWinner_AHundredBeatsNinetyNine_AndTwoHundredsAreATie()
    {
        await Assert.That(LootDiceRules.PickWinner(Rolls(("Ayla", 99), ("Belanthas", 100)), _ => 0)).IsEqualTo("Belanthas");
        await Assert.That(LootDiceRules.HighestRollers(Rolls(("Ayla", 100), ("Belanthas", 100), ("Corin", 99))))
            .IsEquivalentTo(["Ayla", "Belanthas"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task HighestRollers_KeepsTheOrderTheRollsAreListedIn()
    {
        var rolls = Rolls(("Corin", 33), ("Ayla", 90), ("Belanthas", 90));

        await Assert.That(LootDiceRules.HighestRollers(rolls)).IsEquivalentTo(["Ayla", "Belanthas"], CollectionOrdering.Matching);
    }

    private static List<KeyValuePair<string, sbyte>> Rolls(params (string Name, sbyte Roll)[] rolls) =>
        rolls.Select(r => new KeyValuePair<string, sbyte>(r.Name, r.Roll)).ToList();
}
