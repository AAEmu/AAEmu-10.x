using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Plots;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.Game.Models.Game.NPChar;

/// <summary>
/// When to World-run an OnSpawn plot graph under ZoneAuthority (dedic is silent).
/// A plot id is required. A <c>plot_only</c> graph runs for every zone mirror: the dedic never fires
/// those, and every NPC that carries one is an event script (summon stages, greetings, a dungeon's
/// enemy-summon trigger, the hunter that leaps on a summoned army). A graph without <c>plot_only</c>
/// stays restricted to tower-priority mirrors — Lusca stage skills attach a plot with plot_only false
/// and no direct skill_effects. Skills that still have direct skill_effects keep the old skip
/// (e.g. Crimson seed open FX). A graph that creates or removes World-owned objects (doodads, spawner NPCs)
/// also runs on any mirror: the dedic cannot touch those. In a dungeon copy the dedic fires no OnSpawn skills
/// at all, so World runs every graph there.
/// </summary>
public static class OnSpawnPlotWorldGate
{
    public static bool ShouldRun(
        bool zoneAuthority,
        bool isZoneMirror,
        bool isPriorityMirror,
        bool hasPlot,
        bool plotOnly,
        int directSkillEffectCount,
        bool summonsWorldObjects = false,
        bool inDungeonCopy = false)
    {
        if (!zoneAuthority || !isZoneMirror || !hasPlot)
            return false;
        if (plotOnly || summonsWorldObjects || inDungeonCopy)
            return true;
        return isPriorityMirror && directSkillEffectCount <= 0;
    }

    /// <summary>
    /// on_death npc skills: the zone host runs them for the NPCs it spawned in the open world, but fires none in a
    /// dungeon copy. World-spawned NPCs always run them on World.
    /// </summary>
    public static bool ShouldRunDeathSkills(bool isZoneMirror, bool isWorldAuthored, bool inDungeonCopy)
        => !isZoneMirror || isWorldAuthored || inDungeonCopy;

    /// <summary>
    /// Whether <c>Skill.Use</c> should return after starting the plot (no <c>Cast</c>).
    /// Content <c>plot_only</c> always does. A non-plot_only OnSpawn with
    /// <c>target_area_radius &gt; 0</c> must still reach <c>Cast</c> so AoE
    /// <c>DoodadFuncSkillHit</c> runs (Whalesong start skill — plot despawns spawners, Cast reveals
    /// nearby curse mines). Lusca-style plot fills with radius 0 stay plot-graph-only.
    /// </summary>
    public static bool ShouldForcePlotGraphOnly(bool plotOnly, int targetAreaRadius)
    {
        if (plotOnly)
            return true;
        return targetAreaRadius <= 0;
    }

    /// <summary>Effects that create or remove World-owned objects, which the dedic cannot do.</summary>
    public static bool ActsOnWorldObjects(EffectTemplate effect) => effect switch
    {
        NpcSpawnerSpawnEffect => true,
        InteractionEffect { WorldInteraction: WorldInteractionType.SummonDoodad } => true,
        SpecialEffect { SpecialEffectTypeId: SpecialType.RemoveDoodad } => true,
        _ => false
    };

    /// <summary>True when any event reachable from <paramref name="root"/> has an effect matching <paramref name="isWorldSummon"/>.</summary>
    public static bool SummonsWorldObjects(PlotEventTemplate root, Func<PlotEventEffect, bool> isWorldSummon)
    {
        if (root == null)
            return false;

        var seen = new HashSet<PlotEventTemplate>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<PlotEventTemplate>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var evt = pending.Pop();
            if (!seen.Add(evt))
                continue;
            if (evt.Effects.Any(isWorldSummon))
                return true;
            foreach (var next in evt.NextEvents)
            {
                if (next?.Event != null)
                    pending.Push(next.Event);
            }
        }

        return false;
    }
}
