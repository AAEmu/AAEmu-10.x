using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class SkillProductSpaceRulesTests
{
    // Unidentified Story Quest Infusion 54334 -> skill 50835 -> Story Quest Infusion 54335 x1.
    private static readonly SkillProduct StoryInfusion = new() { SkillId = 50835, ItemId = 54335, Amount = 1 };

    [Test]
    public async Task FullBag_RefusesTheOpening()
    {
        await Assert.That(SkillProductSpaceRules.Fits([StoryInfusion], _ => 0)).IsFalse();
    }

    [Test]
    public async Task RoomOnAnExistingStack_AllowsIt()
    {
        await Assert.That(SkillProductSpaceRules.Fits([StoryInfusion], id => id == 54335 ? 99 : 0)).IsTrue();
    }

    [Test]
    public async Task ProductsOfOneItem_AreSummed()
    {
        var two = new SkillProduct { ItemId = 54335, Amount = 2 };
        await Assert.That(SkillProductSpaceRules.Fits([StoryInfusion, two], _ => 2)).IsFalse();
        await Assert.That(SkillProductSpaceRules.Fits([StoryInfusion, two], _ => 3)).IsTrue();
    }

    [Test]
    public async Task EveryProductNeedsItsOwnRoom()
    {
        var other = new SkillProduct { ItemId = 1, Amount = 1 };
        await Assert.That(SkillProductSpaceRules.Fits([StoryInfusion, other], id => id == 54335 ? 5 : 0)).IsFalse();
    }

    [Test]
    public async Task NoProducts_AlwaysFits()
    {
        await Assert.That(SkillProductSpaceRules.Fits([], _ => 0)).IsTrue();
        await Assert.That(SkillProductSpaceRules.Fits(null, _ => 0)).IsTrue();
    }
}
