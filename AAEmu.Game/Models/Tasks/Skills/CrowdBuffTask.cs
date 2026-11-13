using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Tasks.Skills;

/// <summary>buffs.crowd_*: grants and takes back crowd buffs as holders gather and split up.</summary>
public class CrowdBuffTask : Task
{
    public override void Execute()
    {
        CrowdBuffs.Recheck();
    }
}
