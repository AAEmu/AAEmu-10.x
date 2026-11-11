using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Models.Game.Units;

/// <summary>Grants a buff's crowd buff to the holders around its owner (<see cref="CrowdBuffRules"/>).</summary>
public static class CrowdBuffs
{
    public static void OnBuffApplied(Unit owner, BuffTemplate applied)
    {
        if (owner == null || applied == null || applied.CrowdBuffId == 0)
            return;

        var checkBuffId = CrowdBuffRules.CheckBuffId(applied.Id, applied.CrowdCheckBuffId);
        var holders = WorldManager.GetAround<Unit>(owner, applied.CrowdRadius)
            .Where(unit => unit.Buffs.CheckBuff(checkBuffId))
            .Append(owner)
            .Distinct()
            .ToList();
        if (!CrowdBuffRules.CrowdReached(holders.Count, applied.CrowdNumber))
            return;

        foreach (var holder in holders.Where(unit => !unit.Buffs.CheckBuff(applied.CrowdBuffId)))
            holder.Buffs.AddBuff(applied.CrowdBuffId, owner);
    }
}
