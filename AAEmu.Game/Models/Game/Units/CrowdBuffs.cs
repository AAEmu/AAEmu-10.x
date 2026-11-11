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
        var holders = CrowdBuffRules.Recipients(owner, WorldManager.GetAround<Unit>(owner, applied.CrowdRadius),
            unit => unit.Buffs.CheckBuff(checkBuffId), applied.CrowdNumber);

        foreach (var holder in holders.Where(unit => !unit.Buffs.CheckBuff(applied.CrowdBuffId)))
            holder.Buffs.AddBuff(applied.CrowdBuffId, owner);
    }

    /// <summary>The check above only runs when a buff lands; holders who gather later are caught here.</summary>
    public static void Recheck(IEnumerable<Unit> units)
    {
        foreach (var unit in units)
        {
            if (unit?.Buffs == null)
                continue;

            foreach (var source in unit.Buffs.GetCrowdSources())
            {
                if (CrowdBuffRules.NeedsRecheck(source.Template.CrowdBuffId, unit.Buffs.CheckBuff(source.Template.CrowdBuffId)))
                    OnBuffApplied(unit, source.Template);
            }
        }
    }
}
