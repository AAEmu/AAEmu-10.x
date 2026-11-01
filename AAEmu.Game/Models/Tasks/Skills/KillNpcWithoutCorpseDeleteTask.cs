using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Effects;

namespace AAEmu.Game.Models.Tasks.Skills;

/// <summary>
/// Finishes a deferred <see cref="KillNpcWithoutCorpseEffect"/> despawn after same-event SkillUse.
/// </summary>
public sealed class KillNpcWithoutCorpseDeleteTask(Npc npc, bool zoneAuthority) : Task
{
    public override void Execute() =>
        KillNpcWithoutCorpseEffect.FinishRemoval(npc, zoneAuthority);
}
