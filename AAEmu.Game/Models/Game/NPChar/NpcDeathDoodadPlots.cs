using AAEmu.Game.Core.Managers;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Plots.Type;
using AAEmu.Game.Models.Game.Skills.Static;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.Game.Models.Game.NPChar;

public partial class Npc
{
    private int _zoneDeathDoodadConsumed;

    internal void ResetZoneDeathDoodadPlots() => Interlocked.Exchange(ref _zoneDeathDoodadConsumed, 0);

    // Zone mirrors never enter NpcSpawnerNpc.RegisterNpcEvents. Execute only the narrowly
    // identified World-owned object-drop graph; native combat/death scripts remain native.
    internal int RunZoneDeathDoodadPlots(Action<Npc, SkillTemplate> execute = null)
    {
        if (!WorldIntegration.ZoneAuthority || !IsZoneMirror || Spawner != null || !IsDead)
            return 0;
        if (Interlocked.Exchange(ref _zoneDeathDoodadConsumed, 1) != 0)
            return 0;

        var started = 0;
        foreach (var row in NpcGameData.Instance.GetNpSkills(TemplateId, SkillUseConditionKind.OnDeath)
                     .DistinctBy(row => row.SkillId))
        {
            var template = SkillManager.Instance.GetSkillTemplate(row.SkillId);
            if (!IsSingleWorldDeathDoodadPlot(template))
                continue;
            try
            {
                if (execute != null)
                    execute(this, template);
                else
                    new Skill(template).RunNpcDeathDoodadPlotAsync(this).GetAwaiter().GetResult();
                started++;
            }
            catch (Exception error)
            {
                Logger.Error(error, "Zone death object graph failed npc={0} template={1} skill={2}",
                    ObjId, TemplateId, row.SkillId);
            }
        }
        return started;
    }

    private static bool IsSingleWorldDeathDoodadPlot(SkillTemplate template)
    {
        var root = template?.Plot?.Tree?.RootNode;
        var evt = root?.Event;
        // A single, unconditional location effect has no combat targets, loop, probability,
        // channel or delayed children. Run its native positioning instead of inventing a drop.
        if (evt == null || template.Effects.Count != 0 || root.Children.Count != 0 ||
            evt.NextEvents.Count != 0 || evt.Conditions.Count != 0 || evt.AoeConditions.Count != 0 ||
            evt.Effects.Count != 1 || evt.TargetUpdateMethodId != (uint)PlotTargetUpdateMethodType.Area ||
            evt.TargetUpdateMethodParam2 != 0 ||
            evt.SourceUpdateMethodId != (uint)PlotSourceUpdateMethodType.PreviousSource)
            return false;
        var effect = evt.Effects.First.Value;
        return effect.SourceId == PlotEffectSource.OriginalSource && effect.TargetId == PlotEffectTarget.Location &&
               SkillManager.Instance.GetEffectTemplate(effect.ActualId, effect.ActualType) is InteractionEffect
                   { WorldInteraction: WorldInteractionType.SummonDoodad, DoodadId: > 0 };
    }
}
