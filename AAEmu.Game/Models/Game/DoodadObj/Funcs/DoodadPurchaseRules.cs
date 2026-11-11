using System.Numerics;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

/// <summary>
/// The second reach check of a client-confirmed doodad purchase (<c>CSDoodadPurchaseItemPacket</c>).
/// Every purchase is first measured against its func skill's <c>max_range</c>; this one applies only to
/// purchases paid in an item that <c>World.DoodadCoinPurchaseExtraReachMeters</c> names, and only after
/// that first check has refused.
/// </summary>
public static class DoodadPurchaseRules
{
    /// <summary>
    /// The extra horizontal reach configured for purchases paid in <paramref name="coinItemId"/>
    /// (<c>doodad_func_purchases.coin_item_id</c>). There is none for a purchase that is not paid in an
    /// item, for an item the configuration does not name, or for a value that is not a positive number:
    /// such a purchase keeps the func skill's range alone.
    /// </summary>
    public static bool TryGetExtraReach(
        IReadOnlyDictionary<uint, float> extraReachByCoinItem, uint coinItemId, out float extraReach)
    {
        extraReach = 0f;
        if (coinItemId == 0 || extraReachByCoinItem == null ||
            !extraReachByCoinItem.TryGetValue(coinItemId, out var configured) ||
            !float.IsFinite(configured) || configured <= 0f)
            return false;

        extraReach = configured;
        return true;
    }

    /// <summary>
    /// Whether a buyer at <paramref name="buyer"/> reaches a doodad at <paramref name="doodad"/> with
    /// <paramref name="extraReach"/> metres added to the func skill's <paramref name="maxRange"/>. The
    /// extra reach is horizontal only: the height difference still has to be within
    /// <paramref name="maxRange"/>, which is as far above or below the doodad as the skill's own range
    /// lets a buyer stand.
    /// </summary>
    public static bool IsWithinExtraReach(Vector3 buyer, Vector3 doodad, float maxRange, float extraReach)
    {
        var range = MathF.Max(0f, maxRange);
        var horizontal = Vector2.Distance(new Vector2(buyer.X, buyer.Y), new Vector2(doodad.X, doodad.Y));
        var vertical = MathF.Abs(buyer.Z - doodad.Z);
        return horizontal <= range + extraReach && vertical <= range;
    }
}
