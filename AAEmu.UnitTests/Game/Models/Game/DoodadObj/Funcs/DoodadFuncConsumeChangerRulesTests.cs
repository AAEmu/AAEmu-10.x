using AAEmu.Game.Models.Game.DoodadObj.Funcs;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncConsumeChangerRulesTests
{
    private const uint StorageTag = 3124;
    private const uint OldPack = 100;
    private const uint NewPack = 200;
    private const uint OtherItem = 300;

    private static readonly HashSet<uint> ItemsWithStorageTag = [OldPack, NewPack];
    private static readonly List<uint> StorageItemList = [OldPack];

    [Test]
    public async Task TaggedItem_IsAccepted_WhenTheListDoesNotHaveIt()
    {
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            NewPack, StorageTag, ItemsWithStorageTag, StorageItemList)).IsTrue();
    }

    [Test]
    public async Task TaggedItem_IsAccepted_WhenTheStorageHasNoList()
    {
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            NewPack, StorageTag, ItemsWithStorageTag, [])).IsTrue();
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            NewPack, StorageTag, ItemsWithStorageTag, null)).IsTrue();
    }

    [Test]
    public async Task ListedItem_IsAccepted_WhenItDoesNotCarryTheTag()
    {
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            OtherItem, StorageTag, ItemsWithStorageTag, [OldPack, OtherItem])).IsTrue();
    }

    [Test]
    public async Task ListedItem_IsAccepted_WhenTheStorageHasNoTag()
    {
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            OldPack, 0, new HashSet<uint>(), StorageItemList)).IsTrue();
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            OldPack, 0, null, StorageItemList)).IsTrue();
    }

    [Test]
    public async Task ItemWithNeitherTagNorListing_IsRefused()
    {
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            OtherItem, StorageTag, ItemsWithStorageTag, StorageItemList)).IsFalse();
    }

    [Test]
    public async Task StorageWithoutTag_DoesNotAcceptByTag()
    {
        // Tag 0 is "no tag": items handed over for it must not open a storage that only has a list.
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            NewPack, 0, ItemsWithStorageTag, StorageItemList)).IsFalse();
    }

    [Test]
    public async Task StorageWithNeitherTagNorList_AcceptsNothing()
    {
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(OldPack, 0, null, null)).IsFalse();
        await Assert.That(DoodadFuncConsumeChangerRules.AcceptsItem(
            OldPack, StorageTag, new HashSet<uint>(), [])).IsFalse();
    }
}
