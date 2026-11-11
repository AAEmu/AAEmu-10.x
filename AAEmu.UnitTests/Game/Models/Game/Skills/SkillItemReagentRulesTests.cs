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

    [Test]
    public async Task AClientCastNamingAnotherSkill_IsStillAUseOfTheItem()
    {
        // A request can name any skill for a bind-on-pickup item. It must not get past the check
        // that the caster holds the item by naming one that is not the item's use skill.
        await Assert.That(SkillItemReagentRules.IsItemUse(Potion(), 33065, nestedCast: false)).IsTrue();
        await Assert.That(SkillItemReagentRules.IsItemUse(Potion(), 46252, nestedCast: false)).IsTrue();
    }

    [Test]
    public async Task AServerNestedCastOfAnotherSkill_IsNotAUse()
    {
        // The potion's heal tick: the potion may be gone from the bag by now.
        await Assert.That(SkillItemReagentRules.IsItemUse(Potion(), 33065, nestedCast: true)).IsFalse();
    }

    [Test]
    public async Task AServerNestedCastOfTheItemsOwnUseSkill_IsAUse()
    {
        await Assert.That(SkillItemReagentRules.IsItemUse(Potion(), 46252, nestedCast: true)).IsTrue();
        await Assert.That(SkillItemReagentRules.IsItemUse(new ItemTemplate { Id = 1, UseSkillId = 0 }, 33065, nestedCast: true)).IsTrue();
        await Assert.That(SkillItemReagentRules.IsItemUse(null, 33065, nestedCast: true)).IsTrue();
    }

    [Test]
    public async Task OnlyTheSkillUseEffect_MarksACastNested()
    {
        // A skill built for a client request is never nested; the flag is init-only.
        await Assert.That(new Skill().NestedCast).IsFalse();
        await Assert.That(new Skill { NestedCast = true }.NestedCast).IsTrue();
    }
}
