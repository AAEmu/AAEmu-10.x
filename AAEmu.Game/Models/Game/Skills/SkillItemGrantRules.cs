using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// Whether casting a skill hands out items: a loot pack, a gain-item special, or a skill it casts that does.
/// </summary>
public static class SkillItemGrantRules
{
    public static bool GrantsItems(SkillTemplate skill, Func<uint, SkillTemplate> lookup)
        => GrantsItems(skill, lookup, []);

    private static bool GrantsItems(SkillTemplate skill, Func<uint, SkillTemplate> lookup, HashSet<uint> seen)
    {
        if (skill == null || !seen.Add(skill.Id))
            return false;

        foreach (var effect in skill.Effects)
        {
            switch (effect.Template)
            {
                case GainLootPackItemEffect:
                    return true;
                case SpecialEffect { SpecialEffectTypeId: SpecialType.GainItem or SpecialType.GainItemWithPosImprint or SpecialType.GainItemWithEmblemImprint }:
                    return true;
                case SpecialEffect { SpecialEffectTypeId: SpecialType.SkillUse } use
                    when GrantsItems(lookup((uint)use.Value1), lookup, seen):
                    return true;
            }
        }

        return false;
    }
}
