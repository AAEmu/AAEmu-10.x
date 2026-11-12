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
    private sealed class HolderState
    {
        public readonly object Gate = new();
        public readonly ConcurrentDictionary<uint, byte> Granted = new();
    }

    /// <summary>Units holding a crowd source, with the crowd buffs granted to them here.</summary>
    private static readonly ConcurrentDictionary<Unit, HolderState> Holders = new();

    public static void OnBuffApplied(Unit owner, BuffTemplate applied)
    {
        if (owner?.Buffs == null || applied == null || applied.CrowdBuffId == 0)
            return;

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
            if (holder.Buffs == null || holder.Region == null)
                WithHolder(holder, _ => false);
            else
                Evaluate(holder);
        }
    }

    internal static bool IsTracked(Unit holder) => Holders.ContainsKey(holder);

    /// <summary>
    /// Runs <paramref name="check"/> as the only check of this holder, so a grant, its cleanup and the
    /// holder's removal cannot interleave with another pass. A false result stops tracking the holder.
    /// </summary>
    internal static void WithHolder(Unit holder, Func<ConcurrentDictionary<uint, byte>, bool> check)
    {
        while (true)
        {
            var state = Holders.GetOrAdd(holder, _ => new HolderState());
            lock (state.Gate)
            {
                // A pass dropped this state while we waited: take the holder's fresh one.
                if (!Holders.TryGetValue(holder, out var current) || !ReferenceEquals(current, state))
                    continue;

                if (!check(state.Granted))
                    Holders.TryRemove(new KeyValuePair<Unit, HolderState>(holder, state));
                return;
            }
        }
    }

    private static void Evaluate(Unit holder) => WithHolder(holder, granted => EvaluateLocked(holder, granted));

    /// <returns>False when the holder has no crowd source and no granted crowd buff left to track.</returns>
    private static bool EvaluateLocked(Unit holder, ConcurrentDictionary<uint, byte> granted)
    {
        var reached = new Dictionary<uint, bool>();
        foreach (var source in holder.Buffs.GetCrowdSources())
        {
            var template = source.Template;
            reached.TryGetValue(template.CrowdBuffId, out var already);
            reached[template.CrowdBuffId] = already || Reached(holder, template);
        }

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
