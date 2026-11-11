using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects.SpecialEffects;

public class RechargeItemRndAttrUnitModifierTests
{
    [Test]
    public async Task ARejectedRecharge_CancelsTheCast_SoTheCleanerIsKept()
    {
        // Daru Costume Cleaner 54515 (skill 51047) used without a costume to recharge.
        var skill = new Skill(new SkillTemplate { Id = 51047 });
        var owner = new Character(new UnitCustomModelParams()) { Id = 1 };

        new RechargeItemRndAttrUnitModifier().Execute(owner, new SkillCasterUnit(1), owner, null, null, skill, null,
            DateTime.UtcNow, 0, 0, 0, 0);

        await Assert.That(skill.Cancelled).IsTrue();
    }
}
