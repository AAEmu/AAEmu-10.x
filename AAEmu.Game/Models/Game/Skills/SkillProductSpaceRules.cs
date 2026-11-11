using System;
using System.Collections.Generic;
using System.Linq;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// Whether the bag can take every skill_products row of a cast, and handing them over as all or
/// nothing. Item-to-item "opening" skills
/// (Unidentified Story Quest Infusion 54334 → skill 50835 → infusion 54335, and every other
/// skill_reagents/skill_products pair) used to take the reagent and then hand the product to a bag
/// with no room, where AcquireDefaultItem quietly failed: the item was spent and nothing came back.
/// The products are added before the reagent leaves the bag, so the reagent's own slot does not count.
/// </summary>
public static class SkillProductSpaceRules
{
    /// <summary>
    /// Each product first tops up the stacks of its item already in the bag; what is left over needs
    /// empty slots, and all the products draw those from the same <paramref name="freeSlots"/>. Asking
    /// the bag per item (ItemContainer.SpaceLeftForItem) would hand every product the same empty slots.
    /// </summary>
    /// <param name="products">The cast's skill_products rows.</param>
    /// <param name="stackRoomFor">Units of an item template that still fit on the stacks already in the bag.</param>
    /// <param name="maxStackFor">Max stack size of an item template; 0 or less for an unknown item.</param>
    /// <param name="freeSlots">Empty bag slots.</param>
    public static bool Fits(IEnumerable<SkillProduct> products, Func<uint, int> stackRoomFor,
        Func<uint, int> maxStackFor, int freeSlots)
    {
        if (products == null)
            return true;

        long slotsLeft = Math.Max(0, freeSlots);
        foreach (var group in products.Where(p => p != null && p.Amount > 0).GroupBy(p => p.ItemId))
        {
            var needed = group.Sum(p => (long)p.Amount);
            var remaining = needed - Math.Max(0, stackRoomFor(group.Key));
            if (remaining <= 0)
                continue;

            var maxStack = maxStackFor(group.Key);
            if (maxStack <= 0)
                return false;

            slotsLeft -= (remaining + maxStack - 1) / maxStack;
            if (slotsLeft < 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Hands over the products in order and stops at the first one that is refused. What was handed
    /// over before it is taken back, last one first, so a stopped exchange leaves the bag as it was:
    /// the caller keeps the reagent, and must not leave the player the earlier products for free.
    /// </summary>
    /// <param name="products">The cast's skill_products rows.</param>
    /// <param name="grant">Gives one product; false when the bag refused it.</param>
    /// <param name="takeBack">Removes a product <paramref name="grant"/> gave earlier.</param>
    /// <returns>True when every product was given.</returns>
    public static bool GrantAllOrNone(IEnumerable<SkillProduct> products, Func<SkillProduct, bool> grant,
        Action<SkillProduct> takeBack)
    {
        if (products == null)
            return true;

        var granted = new Stack<SkillProduct>();
        foreach (var product in products.Where(p => p != null && p.Amount > 0))
        {
            if (grant(product))
            {
                granted.Push(product);
                continue;
            }

            while (granted.Count > 0)
                takeBack(granted.Pop());
            return false;
        }

        return true;
    }
}
