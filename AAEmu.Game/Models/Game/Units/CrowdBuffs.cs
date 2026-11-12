using System.Collections.Concurrent;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Models.Game.Units;

/// <summary>Grants and takes back crowd buffs per holder (<see cref="CrowdBuffRules"/>).</summary>
public static class CrowdBuffs
{
    /// <summary>Units holding a crowd source, with the crowd buffs granted to them here.</summary>
    private static readonly ConcurrentDictionary<Unit, ConcurrentDictionary<uint, byte>> Holders = new();

    public static void OnBuffApplied(Unit owner, BuffTemplate applied)
    {
        if (owner?.Buffs == null || applied == null || applied.CrowdBuffId == 0)
            return;

        Holders.TryAdd(owner, new ConcurrentDictionary<uint, byte>());
        Evaluate(owner);

        foreach (var neighbour in WorldManager.GetAround<Unit>(owner, applied.CrowdRadius))
        {
            if (Holders.ContainsKey(neighbour))
                Evaluate(neighbour);
        }
    }

    /// <summary>Periodic pass: holders who gathered or split up since the source buff landed.</summary>
    public static void Recheck()
    {
        foreach (var holder in Holders.Keys)
        {
            if (holder.Buffs == null || holder.Region == null || !Evaluate(holder))
                Holders.TryRemove(holder, out _);
        }
    }

    /// <returns>False when the holder has no crowd source and no granted crowd buff left to track.</returns>
    private static bool Evaluate(Unit holder)
    {
        var reached = new Dictionary<uint, bool>();
        foreach (var source in holder.Buffs.GetCrowdSources())
        {
            var template = source.Template;
            reached.TryGetValue(template.CrowdBuffId, out var already);
            reached[template.CrowdBuffId] = already || Reached(holder, template);
        }

        var granted = Holders.GetOrAdd(holder, _ => new ConcurrentDictionary<uint, byte>());
        foreach (var (crowdBuffId, isReached) in reached)
        {
            if (isReached && !holder.Buffs.CheckBuff(crowdBuffId))
            {
                granted[crowdBuffId] = 0;
                holder.Buffs.AddBuff(crowdBuffId, holder);
            }
        }

        foreach (var crowdBuffId in granted.Keys)
        {
            var stillHeld = holder.Buffs.CheckBuff(crowdBuffId);
            var isReached = reached.GetValueOrDefault(crowdBuffId);
            var duration = SkillManager.Instance.GetBuffTemplate(crowdBuffId)?.Duration ?? 0;

            if (stillHeld && CrowdBuffRules.RemovesGranted(isReached, duration))
                holder.Buffs.RemoveBuff(crowdBuffId);

            if (!isReached && (!stillHeld || CrowdBuffRules.RemovesGranted(isReached, duration)))
                granted.TryRemove(crowdBuffId, out _);
        }

        return reached.Count > 0 || !granted.IsEmpty;
    }

    /// <summary>The player a unit acts for; a summoned NPC carries only the player's id.</summary>
    internal static Character ResolveOwner(Unit holder, Func<uint, Character> findCharacter)
    {
        var owner = holder.GetOwnerCharacter();
        if (owner == null && holder is Npc { OwnerId: > 0 } summon)
            owner = findCharacter(summon.OwnerId);
        return owner != null && owner != holder ? owner : null;
    }

    private static bool Reached(Unit holder, BuffTemplate source)
    {
        var check = new CrowdCheck(
            CrowdBuffRules.CheckBuffId(source.Id, source.CrowdCheckBuffId, source.CrowdCheckBuffTagId),
            source.CrowdCheckBuffTagId, source.CrowdNumber, source.CrowdFriendly, source.CrowdHostile, source.CrowdCheckOwner);

        return CrowdBuffRules.Reached<Unit>(holder, ResolveOwner(holder, id => WorldManager.Instance.GetCharacterById(id)),
            WorldManager.GetAround<Unit>(holder, source.CrowdRadius),
            unit => unit.Buffs != null && CrowdBuffRules.HoldsCheck(check, unit.Buffs.CheckBuff, unit.Buffs.CheckBuffTag),
            holder.GetRelationStateTo, check);
    }
}
