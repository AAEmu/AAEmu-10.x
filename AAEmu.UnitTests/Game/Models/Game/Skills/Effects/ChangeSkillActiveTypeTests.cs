using System.Reflection;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.UnitTests.Utils;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects;

/// <summary>Ids here are invented; the skill is unknown, so nothing reaches the database.</summary>
[NotInParallel]
public class ChangeSkillActiveTypeTests
{
    private SingletonScope<SkillManager> _skills;

    [Before(Test)]
    public void InstallCatalog()
    {
        _skills = new SingletonScope<SkillManager>(new SkillManager(
            Mock.Of<IAnimationManager>().Object,
            Mock.Of<IPlotManager>().Object));
        typeof(SkillManager).GetField("_skills", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(SkillManager.Instance, new Dictionary<uint, SkillTemplate>());
    }

    [After(Test)]
    public void RestoreCatalog() => _skills.Dispose();

    [Test]
    [Arguments((int)SkillActiveType.Active)]
    [Arguments(99)]
    [Arguments(-1)]
    [Arguments(300)]
    public async Task Execute_DoesNotThrowOnAnyActiveTypeValue(int activeType)
    {
        var character = new Character(new UnitCustomModelParams());
        character.SkillActiveTypes = new CharacterSkillActiveTypes(character);

        await Assert.That(() => new ChangeSkillActiveType().Execute(
            character, null, character, null, null, null, null, DateTime.UtcNow, 92001, activeType, 0, 0))
            .ThrowsNothing();
    }
}
