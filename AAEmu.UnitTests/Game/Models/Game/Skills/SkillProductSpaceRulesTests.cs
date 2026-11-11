using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class SkillProductSpaceRulesTests
{
    private const uint InfusionId = 54335;
    private const uint OtherId = 1;
    private const int MaxStack = 100;

    // Unidentified Story Quest Infusion 54334 -> skill 50835 -> Story Quest Infusion 54335 x1.
    private static readonly SkillProduct StoryInfusion = new() { SkillId = 50835, ItemId = InfusionId, Amount = 1 };
    private static readonly SkillProduct Other = new() { SkillId = 50835, ItemId = OtherId, Amount = 1 };

    /// <summary>A bag with <paramref name="freeSlots"/> empty slots and room left on the infusion stacks only.</summary>
    private static bool Fits(IEnumerable<SkillProduct> products, int freeSlots, int infusionStackRoom = 0) =>
        SkillProductSpaceRules.Fits(products, id => id == InfusionId ? infusionStackRoom : 0, _ => MaxStack, freeSlots);

    [Test]
    public async Task FullBag_RefusesTheOpening()
    {
        await Assert.That(Fits([StoryInfusion], freeSlots: 0)).IsFalse();
    }

    [Test]
    public async Task OneEmptySlot_AllowsIt()
    {
        await Assert.That(Fits([StoryInfusion], freeSlots: 1)).IsTrue();
    }

    [Test]
    public async Task RoomOnAnExistingStack_AllowsIt()
    {
        await Assert.That(Fits([StoryInfusion], freeSlots: 0, infusionStackRoom: 99)).IsTrue();
    }

    [Test]
    public async Task ProductsOfOneItem_AreSummed()
    {
        var two = new SkillProduct { ItemId = InfusionId, Amount = 2 };
        await Assert.That(Fits([StoryInfusion, two], freeSlots: 0, infusionStackRoom: 2)).IsFalse();
        await Assert.That(Fits([StoryInfusion, two], freeSlots: 0, infusionStackRoom: 3)).IsTrue();
    }

    [Test]
    public async Task EveryProductNeedsItsOwnRoom()
    {
        await Assert.That(Fits([StoryInfusion, Other], freeSlots: 0, infusionStackRoom: 5)).IsFalse();
    }

    [Test]
    public async Task TwoProductTypes_DoNotShareOneEmptySlot()
    {
        // Each fits the single empty slot on its own; together they need two.
        await Assert.That(Fits([StoryInfusion], freeSlots: 1)).IsTrue();
        await Assert.That(Fits([Other], freeSlots: 1)).IsTrue();
        await Assert.That(Fits([StoryInfusion, Other], freeSlots: 1)).IsFalse();
        await Assert.That(Fits([StoryInfusion, Other], freeSlots: 2)).IsTrue();
    }

    [Test]
    public async Task ProductOnAnExistingStack_LeavesTheEmptySlotToTheOther()
    {
        await Assert.That(Fits([StoryInfusion, Other], freeSlots: 1, infusionStackRoom: 1)).IsTrue();
    }

    [Test]
    public async Task AmountPastTheStacks_TakesOneSlotPerNewStack()
    {
        // 250 units, 30 onto the stack already there, 220 left = three new stacks of 100.
        var many = new SkillProduct { ItemId = InfusionId, Amount = 250 };
        await Assert.That(Fits([many], freeSlots: 2, infusionStackRoom: 30)).IsFalse();
        await Assert.That(Fits([many], freeSlots: 3, infusionStackRoom: 30)).IsTrue();
    }

    [Test]
    public async Task UnknownItem_NeverFits()
    {
        await Assert.That(SkillProductSpaceRules.Fits([StoryInfusion], _ => 0, _ => 0, freeSlots: 5)).IsFalse();
    }

    [Test]
    public async Task EveryProductGranted_NothingIsTakenBack()
    {
        var granted = new List<SkillProduct>();
        var takenBack = new List<SkillProduct>();

        var done = SkillProductSpaceRules.GrantAllOrNone([StoryInfusion, Other], p => { granted.Add(p); return true; }, takenBack.Add);

        await Assert.That(done).IsTrue();
        await Assert.That(granted).IsEquivalentTo(new[] { StoryInfusion, Other });
        await Assert.That(takenBack).IsEmpty();
    }

    [Test]
    public async Task ARefusedProduct_StopsTheExchangeAndTakesBackTheEarlierOnes()
    {
        // The bag fills between the room check and the grant: the third product is refused.
        var third = new SkillProduct { SkillId = 50835, ItemId = 2, Amount = 1 };
        var never = new SkillProduct { SkillId = 50835, ItemId = 3, Amount = 1 };
        var asked = new List<SkillProduct>();
        var takenBack = new List<SkillProduct>();

        var done = SkillProductSpaceRules.GrantAllOrNone(
            [StoryInfusion, Other, third, never],
            p => { asked.Add(p); return p != third; },
            takenBack.Add);

        // False tells the cast to cancel, which is what keeps the reagent in the bag.
        await Assert.That(done).IsFalse();
        await Assert.That(asked.Contains(never)).IsFalse();
        await Assert.That(takenBack.Count).IsEqualTo(2);
        await Assert.That(takenBack[0]).IsSameReferenceAs(Other);
        await Assert.That(takenBack[1]).IsSameReferenceAs(StoryInfusion);
    }

    [Test]
    public async Task TheFirstProductRefused_LeavesNothingToTakeBack()
    {
        var takenBack = new List<SkillProduct>();

        var done = SkillProductSpaceRules.GrantAllOrNone([StoryInfusion, Other], _ => false, takenBack.Add);

        await Assert.That(done).IsFalse();
        await Assert.That(takenBack).IsEmpty();
    }

    [Test]
    public async Task NoProducts_NothingToGrant()
    {
        var calls = 0;
        await Assert.That(SkillProductSpaceRules.GrantAllOrNone(null, _ => { calls++; return false; }, _ => calls++)).IsTrue();
        await Assert.That(SkillProductSpaceRules.GrantAllOrNone(
            [new SkillProduct { ItemId = InfusionId, Amount = 0 }], _ => { calls++; return false; }, _ => calls++)).IsTrue();
        await Assert.That(calls).IsEqualTo(0);
    }

    [Test]
    public async Task NoProducts_AlwaysFits()
    {
        await Assert.That(Fits([], freeSlots: 0)).IsTrue();
        await Assert.That(Fits(null, freeSlots: 0)).IsTrue();
        await Assert.That(Fits([new SkillProduct { ItemId = InfusionId, Amount = 0 }], freeSlots: 0)).IsTrue();
    }
}
