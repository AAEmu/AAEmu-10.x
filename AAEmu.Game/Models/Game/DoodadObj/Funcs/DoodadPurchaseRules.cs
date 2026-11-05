using System.Numerics;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

/// <summary>
/// The checks a client-confirmed doodad purchase (<c>CSDoodadPurchaseItemPacket</c>) makes before paying:
/// which current function the client named, and whether the buyer can reach the doodad.
/// </summary>
public static class DoodadPurchaseRules
{
    /// <summary>
    /// Added to the func skill's <c>max_range</c>. That range is measured by the client to the doodad's
    /// model, while the server only has the doodad's origin and a player position mirrored from the zone.
    /// Mirage Isle design displays (skill 14569, 4 m) sit on carpets wider than that, so a strict
    /// centre-to-centre check refused purchases the client had already allowed.
    /// </summary>
    public const float ReachSlack = 3f;

    /// <summary>
    /// Height difference tolerated between the buyer and the doodad origin. Display origins sit at the
    /// carpet or table base, and a 3D check against 4 m failed on that offset alone.
    /// </summary>
    public const float VerticalReach = 10f;

    /// <summary>
    /// The current purchase func the client named. The client sends <c>doodad_func_purchases.id</c>
    /// (the func's <c>actual_func_id</c>); the <c>doodad_funcs.id</c> row key is accepted as a fallback.
    /// </summary>
    public static DoodadFunc FindPurchaseFunc(IEnumerable<DoodadFunc> currentFuncs, uint purchaseId)
    {
        if (currentFuncs == null)
            return null;

        DoodadFunc byKey = null;
        foreach (var func in currentFuncs)
        {
            if (func.FuncType != nameof(DoodadFuncPurchase))
                continue;
            if (func.FuncId == purchaseId)
                return func;
            if (func.FuncKey == purchaseId)
                byKey ??= func;
        }

        return byKey;
    }

    /// <summary>
    /// Whether a buyer at <paramref name="buyer"/> can use a doodad at <paramref name="doodad"/> whose
    /// func skill reaches <paramref name="maxRange"/> metres: horizontal distance within the range plus
    /// <see cref="ReachSlack"/>, and within <see cref="VerticalReach"/> in height.
    /// </summary>
    public static bool IsWithinReach(Vector3 buyer, Vector3 doodad, float maxRange, out float horizontal, out float vertical)
    {
        horizontal = Vector2.Distance(new Vector2(buyer.X, buyer.Y), new Vector2(doodad.X, doodad.Y));
        vertical = MathF.Abs(buyer.Z - doodad.Z);
        return horizontal <= MathF.Max(0f, maxRange) + ReachSlack && vertical <= VerticalReach;
    }
}
