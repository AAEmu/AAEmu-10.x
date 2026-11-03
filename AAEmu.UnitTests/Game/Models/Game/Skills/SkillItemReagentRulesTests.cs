using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

/// <summary>
/// Adventurer's Hushed Star 34006 runs skill 46252, whose 10 s buff re-casts skill 33065 every second
/// with the potion still attached. One right-click must spend one potion, not eleven.
/// </summary>
public class SkillItemReagentRulesTests
{
    private static ItemTemplate Potion() => new() { Id = 34006, UseSkillId = 46252, UseSkillAsReagent = true };

    [Test]
    public async Task TheItemsOwnUseSkill_IsTheUse()
    {
        await Assert.That(SkillItemReagentRules.IsItemUseSkill(Potion(), 46252)).IsTrue();
    }

    [Test]
    public async Task ANestedSkillCarryingTheItem_IsNotAnotherUse()
    {
        await Assert.That(SkillItemReagentRules.IsItemUseSkill(Potion(), 33065)).IsFalse();
    }

    [Test]
    public async Task AnItemWithNoUseSkill_KeepsCountingEveryCast()
    {
        var material = new ItemTemplate { Id = 1, UseSkillId = 0, UseSkillAsReagent = true };
        await Assert.That(SkillItemReagentRules.IsItemUseSkill(material, 33065)).IsTrue();
        await Assert.That(SkillItemReagentRules.IsItemUseSkill(null, 33065)).IsTrue();
    }
}
