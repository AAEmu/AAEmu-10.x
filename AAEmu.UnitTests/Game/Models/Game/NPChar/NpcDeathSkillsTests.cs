using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Static;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

public class NpcDeathSkillsTests
{
    private static int DeathHandlers(Npc npc) => npc.Events.OnDeath.GetInvocationList().Length;

    [Test]
    public async Task OnDeathSkill_IsHookedOnce()
    {
        // Songsong (22076): on_death 51183 breaks the chest when the waves kill it before the escape.
        var npc = new Npc();
        var before = DeathHandlers(npc);
        var skills = new[] { new NpcSkill { SkillId = 51183, SkillUseCondition = SkillUseConditionKind.OnDeath } };

        await Assert.That(npc.RegisterDeathSkills(skills)).IsTrue();
        npc.RegisterDeathSkills(skills);

        await Assert.That(DeathHandlers(npc)).IsEqualTo(before + 1);
    }

    [Test]
    public async Task OtherConditions_AreLeftToTheZone()
    {
        var npc = new Npc();
        var before = DeathHandlers(npc);
        var skills = new[]
        {
            new NpcSkill { SkillId = 51187, SkillUseCondition = SkillUseConditionKind.InCombat },
            new NpcSkill { SkillId = 51149, SkillUseCondition = SkillUseConditionKind.OnSpawn }
        };

        await Assert.That(npc.RegisterDeathSkills(skills)).IsFalse();
        await Assert.That(DeathHandlers(npc)).IsEqualTo(before);
    }
}
