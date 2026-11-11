using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Plots;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

public class OnSpawnPlotWorldGateTests
{
    [Test]
    public async Task CrimsonStage_PlotOnly_Runs()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: true,
            hasPlot: true,
            plotOnly: true,
            directSkillEffectCount: 0)).IsTrue();
    }

    [Test]
    public async Task LuscaStage_PlotWithoutPlotOnly_Runs()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: true,
            hasPlot: true,
            plotOnly: false,
            directSkillEffectCount: 0)).IsTrue();
    }

    [Test]
    public async Task SeedOpenFx_PlotPlusDirectEffects_Skipped()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: true,
            hasPlot: true,
            plotOnly: false,
            directSkillEffectCount: 1)).IsFalse();
    }

    [Test]
    public async Task Recruiter_NoPlot_Skipped()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: true,
            hasPlot: false,
            plotOnly: false,
            directSkillEffectCount: 14)).IsFalse();
    }

    [Test]
    public async Task AmbientNpc_NotPriority_Skipped()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: false,
            hasPlot: true,
            plotOnly: false,
            directSkillEffectCount: 0)).IsFalse();
    }

    [Test]
    public async Task EventScript_PlotOnly_RunsWithoutPriority()
    {
        // The hunter summoned onto an army stands still unless its plot_only OnSpawn graph runs:
        // the dedic never fires plot_only skills, and it is not a tower-priority mirror.
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: false,
            hasPlot: true,
            plotOnly: true,
            directSkillEffectCount: 0)).IsTrue();
    }

    [Test]
    public async Task NotAZoneMirror_PlotOnly_Skipped()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: false,
            isPriorityMirror: false,
            hasPlot: true,
            plotOnly: true,
            directSkillEffectCount: 0)).IsFalse();
    }

    [Test]
    public async Task WithoutZoneAuthority_Skipped()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: false,
            isZoneMirror: true,
            isPriorityMirror: true,
            hasPlot: true,
            plotOnly: true,
            directSkillEffectCount: 0)).IsFalse();
    }

    [Test]
    public async Task ForcePlotGraphOnly_PlotOnly_Always()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldForcePlotGraphOnly(
            plotOnly: true, targetAreaRadius: 100)).IsTrue();
        await Assert.That(OnSpawnPlotWorldGate.ShouldForcePlotGraphOnly(
            plotOnly: true, targetAreaRadius: 0)).IsTrue();
    }

    [Test]
    public async Task ForcePlotGraphOnly_AreaSkillHit_NeedsCast()
    {
        // Whalesong OnSpawn: plot despawns + Cast SkillHits curse mines.
        await Assert.That(OnSpawnPlotWorldGate.ShouldForcePlotGraphOnly(
            plotOnly: false, targetAreaRadius: 100)).IsFalse();
    }

    [Test]
    public async Task ForcePlotGraphOnly_LuscaStyle_RadiusZero()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldForcePlotGraphOnly(
            plotOnly: false, targetAreaRadius: 0)).IsTrue();
    }

    [Test]
    public async Task HarvestSpawner_SummonsWorldObjects_RunsWithoutPriority()
    {
        // Island of Abundance's farming spawn NPC (22078, skill 51105, plot 7114): not plot_only, not a tower
        // mirror, and the dedic fires nothing in the copy, so its SummonDoodad graph only runs if World runs it.
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: false,
            hasPlot: true,
            plotOnly: false,
            directSkillEffectCount: 0,
            summonsWorldObjects: true)).IsTrue();
    }

    [Test]
    public async Task SummonsWorldObjects_FindsASummonBehindNextEvents()
    {
        // plot 7114: start (special) -> contact -> grade 3/2/1 SummonDoodad interactions
        var summon = Event(Effect("InteractionEffect", 9997));
        var contact = Event();
        contact.NextEvents.AddLast(new PlotNextEvent { Event = summon });
        var start = Event(Effect("SpecialEffect", 69327));
        start.NextEvents.AddLast(new PlotNextEvent { Event = contact });

        var found = OnSpawnPlotWorldGate.SummonsWorldObjects(start, e => e.ActualType == "InteractionEffect");

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task SummonsWorldObjects_SelfLoopWithoutSummon_IsFalse()
    {
        // plot 7120's spawner event repeats itself; a walk must stop on the loop.
        var loop = Event(Effect("SpecialEffect", 1));
        loop.NextEvents.AddLast(new PlotNextEvent { Event = loop });

        var found = OnSpawnPlotWorldGate.SummonsWorldObjects(loop, e => e.ActualType == "NpcSpawnerSpawnEffect");

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task DungeonCopy_AnyPlot_RunsWithoutPriority()
    {
        // Island Songsong (22076, skill 51149, plot 7127): SpawnEffect waves, not plot_only, not a tower mirror.
        // The dedic fires no OnSpawn skills in a dungeon copy, so World runs every graph there.
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: false,
            hasPlot: true,
            plotOnly: false,
            directSkillEffectCount: 0,
            inDungeonCopy: true)).IsTrue();
    }

    [Test]
    public async Task DungeonCopy_StillNeedsAPlot()
    {
        await Assert.That(OnSpawnPlotWorldGate.ShouldRun(
            zoneAuthority: true,
            isZoneMirror: true,
            isPriorityMirror: false,
            hasPlot: false,
            plotOnly: false,
            directSkillEffectCount: 2,
            inDungeonCopy: true)).IsFalse();
    }

    [Test]
    public async Task ActsOnWorldObjects_DoodadEraserRemoval_IsWorldWork()
    {
        // Island eraser 22108, skill 51200, plot 7146: special 58 per crop template, 120 m.
        var removal = new SpecialEffect { SpecialEffectTypeId = SpecialType.RemoveDoodad };

        await Assert.That(OnSpawnPlotWorldGate.ActsOnWorldObjects(removal)).IsTrue();
    }

    [Test]
    public async Task ActsOnWorldObjects_SummonsAndSpawners_AreWorldWork()
    {
        var summon = new InteractionEffect { WorldInteraction = WorldInteractionType.SummonDoodad };

        await Assert.That(OnSpawnPlotWorldGate.ActsOnWorldObjects(summon)).IsTrue();
        await Assert.That(OnSpawnPlotWorldGate.ActsOnWorldObjects(new NpcSpawnerSpawnEffect())).IsTrue();
    }

    [Test]
    public async Task ActsOnWorldObjects_OtherEffects_AreNot()
    {
        var cooldown = new SpecialEffect { SpecialEffectTypeId = SpecialType.GlobalCooldown };
        var use = new InteractionEffect { WorldInteraction = WorldInteractionType.Use };

        await Assert.That(OnSpawnPlotWorldGate.ActsOnWorldObjects(cooldown)).IsFalse();
        await Assert.That(OnSpawnPlotWorldGate.ActsOnWorldObjects(use)).IsFalse();
        await Assert.That(OnSpawnPlotWorldGate.ActsOnWorldObjects(null)).IsFalse();
    }

    private static PlotEventTemplate Event(params PlotEventEffect[] effects)
    {
        var evt = new PlotEventTemplate();
        foreach (var effect in effects)
            evt.Effects.AddLast(effect);
        return evt;
    }

    private static PlotEventEffect Effect(string type, uint id) => new() { ActualType = type, ActualId = id };
}
