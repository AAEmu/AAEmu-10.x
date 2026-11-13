using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Containers;

namespace AAEmu.UnitTests.Game.Models.Game.Items;

/// <summary>
/// The 10.0 Daru costume slots: <c>race_cosplay</c> (equip slot type 33) wears in slot 32 (performance)
/// or slot 33 (appearance). A performance costume's synthesis counts only during Daru Transformation, and an
/// appearance slot is cosmetic.
/// </summary>
public class CostumeSlotRulesTests
{
    [Test]
    [Arguments("Stabilizer", 28)] // enum_equip_slot 28..33
    [Arguments("Undershirt2", 29)]
    [Arguments("Underpants2", 30)]
    [Arguments("CosplayLooks", 31)]
    [Arguments("RaceCosplay", 32)]
    [Arguments("RaceCosplayLooks", 33)]
    public async Task EquipSlots_MatchTheClientEnum(string name, int expected)
    {
        await Assert.That((int)Enum.Parse<EquipmentItemSlot>(name)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("Stabilizer", 32)] // enum_equip_slot_types 32 stabilizer, 33 race_cosplay
    [Arguments("RaceCosplay", 33)]
    public async Task EquipSlotTypes_MatchTheClientEnum(string name, int expected)
    {
        await Assert.That((int)Enum.Parse<EquipmentItemSlotType>(name)).IsEqualTo(expected);
    }

    [Test]
    public async Task EquipmentContainer_HoldsEveryWireSlot()
    {
        var container = new EquipmentContainer(0, SlotType.Equipment, false, null);

        await Assert.That(container.ContainerSize).IsEqualTo(EquipmentSerializer.SlotCount);
    }

    [Test]
    [Arguments(28, 34)] // a container saved before slots 28-33 existed grows to the full table
    [Arguments(34, 34)]
    [Arguments(40, 40)] // never shrinks a larger stored size
    public async Task LoadedEquipmentSize_IsNeverBelowTheSlotTable(int stored, int expected)
    {
        await Assert.That(EquipmentContainer.ResolveLoadedSize(stored)).IsEqualTo(expected);
    }

    [Test]
    public async Task RaceCosplay_WearsInThePerformanceSlotOrTheAppearanceSlot()
    {
        var slots = EquipmentContainer.GetAllowedGearSlots(EquipmentItemSlotType.RaceCosplay);

        await Assert.That(slots).IsEquivalentTo(new[] { EquipmentItemSlot.RaceCosplay, EquipmentItemSlot.RaceCosplayLooks });
    }

    [Test]
    public async Task OrdinaryCosplay_StaysInItsOwnSlot()
    {
        var slots = EquipmentContainer.GetAllowedGearSlots(EquipmentItemSlotType.Cosplay);

        await Assert.That(slots).IsEquivalentTo(new[] { EquipmentItemSlot.Cosplay });
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task PerformanceSlot_CountsOnlyWhileTransformed(bool transformed)
    {
        await Assert.That(CostumeSlotRules.CountsTowardStats(EquipmentItemSlot.RaceCosplay, transformed)).IsEqualTo(transformed);
    }

    [Test]
    [Arguments(EquipmentItemSlot.RaceCosplayLooks)]
    [Arguments(EquipmentItemSlot.CosplayLooks)]
    public async Task AppearanceSlots_NeverCount(EquipmentItemSlot slot)
    {
        await Assert.That(CostumeSlotRules.CountsTowardStats(slot, isCharTransformed: true)).IsFalse();
        await Assert.That(CostumeSlotRules.CountsTowardStats(slot, isCharTransformed: false)).IsFalse();
    }

    [Test]
    [Arguments(EquipmentItemSlot.Chest)]
    [Arguments(EquipmentItemSlot.Mainhand)]
    [Arguments(EquipmentItemSlot.Cosplay)]
    [Arguments(EquipmentItemSlot.Backpack)]
    public async Task OtherSlots_CountWhetherOrNotTransformed(EquipmentItemSlot slot)
    {
        await Assert.That(CostumeSlotRules.CountsTowardStats(slot, isCharTransformed: false)).IsTrue();
        await Assert.That(CostumeSlotRules.CountsTowardStats(slot, isCharTransformed: true)).IsTrue();
    }
}
