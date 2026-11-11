using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Tasks.Skills;

/// <summary>buffs.crowd_*: grants crowd buffs to holders who gathered after receiving the source buff.</summary>
public class CrowdBuffTask : Task
{
    public override void Execute()
    {
        CrowdBuffs.Recheck(WorldManager.Instance.GetAllCharacters());
    }
}
