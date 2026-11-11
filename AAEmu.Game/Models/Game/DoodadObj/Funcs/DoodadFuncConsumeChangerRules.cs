namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

/// <summary>
/// Which items a storage doodad (<c>doodad_func_consume_changers</c>) takes: a cart's trade pack storage, a fish
/// tank, a fish crane.
/// </summary>
public static class DoodadFuncConsumeChangerRules
{
    /// <summary>
    /// A storage accepts an item that carries the storage's tag (<c>doodad_func_consume_changers.tag_id</c>) or
    /// that is in the storage's own list (<c>doodad_func_consume_changer_items</c>). Either one is enough: the
    /// tag covers the items the list was never extended with, and the list is all a storage without a tag has.
    /// </summary>
    /// <param name="itemTemplateId">The item the player wants to store.</param>
    /// <param name="storageTagId">The storage's tag, 0 when it has none.</param>
    /// <param name="itemsWithStorageTag">Every item that carries <paramref name="storageTagId"/>.</param>
    /// <param name="storageItemList">The storage's own item list.</param>
    public static bool AcceptsItem(
        uint itemTemplateId,
        uint storageTagId,
        IReadOnlySet<uint> itemsWithStorageTag,
        IEnumerable<uint> storageItemList)
    {
        if (storageTagId > 0 && itemsWithStorageTag != null && itemsWithStorageTag.Contains(itemTemplateId))
            return true;

        return storageItemList != null && storageItemList.Contains(itemTemplateId);
    }
}
