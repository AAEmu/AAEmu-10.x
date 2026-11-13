using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class SkillItemGrantRulesTests
{
    private static SkillTemplate Skill(uint id, params EffectTemplate[] effects) =>
        new() { Id = id, Effects = effects.Select(e => new SkillEffect { Template = e }).ToList() };

    private static SpecialEffect Special(SpecialType type, int value1 = 0) =>
        new() { SpecialEffectTypeId = type, Value1 = value1 };

    [Test]
    public async Task LootPack_GrantsItems()
    {
        var skill = Skill(1, new GainLootPackItemEffect { LootPackId = 11528 });
        await Assert.That(SkillItemGrantRules.GrantsItems(skill, _ => null)).IsTrue();
    }

    [Test]
    [Arguments(SpecialType.GainItem)]
    [Arguments(SpecialType.GainItemWithPosImprint)]
    [Arguments(SpecialType.GainItemWithEmblemImprint)]
    public async Task GainItemSpecial_GrantsItems(SpecialType type)
    {
        await Assert.That(SkillItemGrantRules.GrantsItems(Skill(1, Special(type, 41511)), _ => null)).IsTrue();
    }

    [Test]
    public async Task SkillUse_IsFollowed()
    {
        var granting = Skill(2, Special(SpecialType.GainItem, 5));
        var skill = Skill(1, Special(SpecialType.SkillUse, 2));
        await Assert.That(SkillItemGrantRules.GrantsItems(skill, id => id == 2 ? granting : null)).IsTrue();
    }

    [Test]
    public async Task BuffAndInteraction_GrantNothing_AndCyclesEnd()
    {
        var skill = Skill(1, new BuffEffect(), Special(SpecialType.SkillUse, 1));
        await Assert.That(SkillItemGrantRules.GrantsItems(skill, id => id == 1 ? skill : null)).IsFalse();
        await Assert.That(SkillItemGrantRules.GrantsItems(null, _ => null)).IsFalse();
    }
}
