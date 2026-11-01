using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Models.Game.NPChar;

/// <summary>
/// When an OnSpawn skill must run through the skill engine because it authors world state
/// (AoE SkillHit on doodads, <see cref="NpcSpawnerSpawnEffect"/> recruiter gaps, …), not
/// because a buff/npc tag names another unit.
/// </summary>
/// <remarks>
/// Plot graphs stay on <see cref="OnSpawnPlotWorldGate"/>; tagged target scripts stay on
/// <see cref="OnSpawnTargetEffectRules"/>. Self-buff paint stays on
/// <see cref="Npc.ApplyOnSpawnSkillBuffs"/>. Engine scripts are the leftover: plotless OnSpawn
/// with <c>target_area_radius &gt; 0</c> (Activate Worldgate, Abyssal Attack Begins) <b>or</b>
/// self-targeted <see cref="NpcSpawnerSpawnEffect"/> / <see cref="NpcSpawnerDespawnEffect"/> /
/// <see cref="SpawnEffect"/> (Abyssal seed recruiter fan-out; Aegis/Ynystere step cleanup — radius 0).
/// </remarks>
public static class OnSpawnAreaScriptRules
{
    /// <summary>
    /// Whether the skill's effect list needs a full <c>Skill.Use</c> (spawner/spawn telegraphs),
    /// even when the area radius is zero.
    /// </summary>
    public static bool HasEngineSpawnEffects(SkillTemplate template)
    {
        if (template?.Effects == null || template.Effects.Count == 0)
            return false;

        foreach (var effect in template.Effects)
        {
            // Spawn fan-out (Abyssal seed) and step cleanup despawns (Aegis / Ynystere purify).
            if (effect?.Template is NpcSpawnerSpawnEffect
                or NpcSpawnerDespawnEffect
                or SpawnEffect)
                return true;
        }

        return false;
    }

    /// <summary>
    /// World should <c>Skill.Use</c> this OnSpawn skill so AoE / spawner effects run under ZA.
    /// </summary>
    public static bool ShouldWorldRun(
        bool zoneAuthority,
        bool isZoneMirror,
        bool isWorldAuthored,
        bool isPriorityMirror,
        bool hasPlot,
        int targetAreaRadius,
        bool claimedByTargetEffectPath,
        bool hasEngineSpawnEffects = false)
    {
        if (!zoneAuthority || !isZoneMirror)
            return false;
        if (hasPlot)
            return false;
        if (claimedByTargetEffectPath)
            return false;

        // AoE OnSpawn (SkillHit on World doodads): dedic cannot advance World-owned map doodads.
        // City recruiters (Abyssal 15156 / Golden Plains 15157) are ambient zone mirrors — still
        // need World Skill.Use so closed→opened portal phases fire.
        if (targetAreaRadius > 0)
            return true;

        // Radius-0 engine spawn effects (seed recruiter fan-out): World-authored / priority only
        // so ambient mirrors do not dual-cast every NpcSpawnerSpawnEffect in the catalog.
        if (hasEngineSpawnEffects)
            return isWorldAuthored || isPriorityMirror;

        return false;
    }
}
