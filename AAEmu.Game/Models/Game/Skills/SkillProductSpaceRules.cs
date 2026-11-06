using System;
using System.Collections.Generic;
using System.Linq;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// Whether the bag can take every skill_products row of a cast. Item-to-item "opening" skills
/// (Unidentified Story Quest Infusion 54334 → skill 50835 → infusion 54335, and every other
/// skill_reagents/skill_products pair) used to take the reagent and then hand the product to a bag
/// with no room, where AcquireDefaultItem quietly failed: the item was spent and nothing came back.
/// The products are added before the reagent leaves the bag, so the reagent's own slot does not count.
/// </summary>
public static class SkillProductSpaceRules
{
    /// <param name="products">The cast's skill_products rows.</param>
    /// <param name="spaceLeftFor">Units of an item template the bag can still take (ItemContainer.SpaceLeftForItem).</param>
    public static bool Fits(IEnumerable<SkillProduct> products, Func<uint, int> spaceLeftFor)
    {
        if (products == null)
            return true;

        foreach (var group in products.Where(p => p != null && p.Amount > 0).GroupBy(p => p.ItemId))
        {
            var needed = group.Sum(p => (long)p.Amount);
            if (spaceLeftFor(group.Key) < needed)
                return false;
        }

        return true;
    }
}
