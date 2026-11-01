using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

public class OnSpawnAreaScriptRulesTests
{
    [Test]
    public async Task WorldAuthored_AreaRadius_Runs()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: true,
            isPriorityMirror: false,
            hasPlot: false,
            targetAreaRadius: 15,
            claimedByTargetEffectPath: false)).IsTrue();
    }

    [Test]
    public async Task PriorityMirror_AreaRadius_Runs()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: false,
            isPriorityMirror: true,
            hasPlot: false,
            targetAreaRadius: 50,
            claimedByTargetEffectPath: false)).IsTrue();
    }

    [Test]
    public async Task PriorityMirror_ZeroRadius_SpawnerSpawnEffects_Runs()
    {
        // Abyssal seed 15117 OnSpawn 27912 — NpcSpawnerSpawnEffect fan-out, no area.
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: false,
            isPriorityMirror: true,
            hasPlot: false,
            targetAreaRadius: 0,
            claimedByTargetEffectPath: false,
            hasEngineSpawnEffects: true)).IsTrue();
    }

    [Test]
    public async Task ZeroRadius_WithoutSpawnEffects_IsNotRun()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: true,
            isPriorityMirror: true,
            hasPlot: false,
            targetAreaRadius: 0,
            claimedByTargetEffectPath: false,
            hasEngineSpawnEffects: false)).IsFalse();
    }

    [Test]
    public async Task AmbientMirror_AreaRadius_RunsForWorldDoodadSkillHit()
    {
        // Recruiter 15156 OnSpawn 27842 — ambient zone mirror; World must Skill.Use for 8594.
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: false,
            isPriorityMirror: false,
            hasPlot: false,
            targetAreaRadius: 15,
            claimedByTargetEffectPath: false)).IsTrue();
    }

    [Test]
    public async Task AmbientMirror_ZeroRadius_SpawnerEffects_IsNotRun()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: false,
            isPriorityMirror: false,
            hasPlot: false,
            targetAreaRadius: 0,
            claimedByTargetEffectPath: false,
            hasEngineSpawnEffects: true)).IsFalse();
    }

    [Test]
    public async Task ZeroRadius_IsNotRun()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: true,
            isPriorityMirror: false,
            hasPlot: false,
            targetAreaRadius: 0,
            claimedByTargetEffectPath: false)).IsFalse();
    }

    [Test]
    public async Task WithPlot_IsLeftToThePlotPath()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: true,
            isPriorityMirror: true,
            hasPlot: true,
            targetAreaRadius: 15,
            claimedByTargetEffectPath: false)).IsFalse();
    }

    [Test]
    public async Task ClaimedByTargetEffect_IsLeftToThatPath()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isWorldAuthored: true,
            isPriorityMirror: true,
            hasPlot: false,
            targetAreaRadius: 15,
            claimedByTargetEffectPath: true)).IsFalse();
    }

    [Test]
    public async Task HasEngineSpawnEffects_IncludesDespawn()
    {
        // Aegis/Ynystere step NPCs OnSpawn — NpcSpawnerDespawnEffect cleanup, radius 0.
        var withDespawn = new SkillTemplate
        {
            Effects = [new SkillEffect { Template = new NpcSpawnerDespawnEffect { SpawnerId = 1 } }]
        };
        await Assert.That(OnSpawnAreaScriptRules.HasEngineSpawnEffects(withDespawn)).IsTrue();

        var withSpawn = new SkillTemplate
        {
            Effects = [new SkillEffect { Template = new NpcSpawnerSpawnEffect() }]
        };
        await Assert.That(OnSpawnAreaScriptRules.HasEngineSpawnEffects(withSpawn)).IsTrue();

        await Assert.That(OnSpawnAreaScriptRules.HasEngineSpawnEffects(
            new SkillTemplate { Effects = [] })).IsFalse();
    }

    [Test]
    public async Task WithoutZoneAuthority_IsNotRun()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: false,
            isZoneMirror: true,
            isWorldAuthored: true,
            isPriorityMirror: true,
            hasPlot: false,
            targetAreaRadius: 15,
            claimedByTargetEffectPath: false)).IsFalse();
    }

    [Test]
    public async Task NotAZoneMirror_IsNotRun()
    {
        await Assert.That(OnSpawnAreaScriptRules.ShouldWorldRun(
            zoneAuthority: true,
            isZoneMirror: false,
            isWorldAuthored: true,
            isPriorityMirror: true,
            hasPlot: false,
            targetAreaRadius: 15,
            claimedByTargetEffectPath: false)).IsFalse();
    }
}
