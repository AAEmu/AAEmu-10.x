using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

/// <summary>
/// A character transform (Daru Transformation) takes the weapons out of play: the weapon slots are disarmed
/// through SCDisarmamentChangedPacket (0x2E9) and the basic attacks are refused for as long as it lasts.
/// </summary>
public class CharTransformCombatRulesTests
{
    [Test]
    public async Task DisarmedSlots_AreMainhandOffhandAndRanged_MusicalStaysArmed()
    {
        EquipmentItemSlot[] expected = [EquipmentItemSlot.Mainhand, EquipmentItemSlot.Offhand, EquipmentItemSlot.Ranged];

        await Assert.That(CharTransformCombatRules.DisarmedSlots).IsEquivalentTo(expected);
    }

    [Test]
    [Arguments(EquipmentItemSlot.Mainhand, true)]
    [Arguments(EquipmentItemSlot.Offhand, true)]
    [Arguments(EquipmentItemSlot.Ranged, true)]
    [Arguments(EquipmentItemSlot.Musical, false)]
    [Arguments(EquipmentItemSlot.Chest, false)]
    public async Task IsSlotDisarmed_WhileTransformed_OnlyForTheWeaponSlots(EquipmentItemSlot slot, bool expected)
    {
        await Assert.That(CharTransformCombatRules.IsSlotDisarmed(isCharTransformed: true, slot)).IsEqualTo(expected);
    }

    [Test]
    public async Task IsSlotDisarmed_WhenNotTransformed_IsFalse()
    {
        await Assert.That(CharTransformCombatRules.IsSlotDisarmed(isCharTransformed: false, EquipmentItemSlot.Ranged)).IsFalse();
    }

    private const byte NoBook = 0;
    private const byte AttackBook = CharTransformCombatRules.AttackSkillBookCategory;
    private const byte GeneralBook = 3;
    private const byte EmotionBook = 5;

    [Test]
    [Arguments(2u, AttackBook)]      // Melee Attack, even though it carries "Transform Skills Exception"
    [Arguments(3u, NoBook)]          // Offhand auto-attack (not a default skill)
    [Arguments(4u, NoBook)]          // Ranged auto-attack (not a default skill)
    [Arguments(16064u, AttackBook)]  // Shoot Arrow: the bow attack that still fired in game on 2026-11-09
    [Arguments(14495u, AttackBook)]  // Kick
    [Arguments(46938u, AttackBook)]  // Shoot Rifle
    [Arguments(39036u, AttackBook)]  // Defiance
    [Arguments(44677u, AttackBook)]  // Nebe's Ice Field (power skill)
    public async Task BlocksSkill_WhileTransformed_RefusesTheAttackTabAndAutoAttacks(uint skillId, byte book)
    {
        await Assert.That(CharTransformCombatRules.BlocksSkill(isCharTransformed: true, skillId, book)).IsTrue();
    }

    [Test]
    [Arguments(20297u, AttackBook)]  // Escape (unstuck teleport) sits on the attack tab but is not combat
    [Arguments(22143u, AttackBook)]  // Rider's Escape
    [Arguments(20256u, AttackBook)]  // Report Suspicious User
    [Arguments(50650u, GeneralBook)] // Daru Transformation itself: casting it again ends the form
    [Arguments(14901u, GeneralBook)] // Use Glider
    [Arguments(14900u, GeneralBook)] // Play Instrument
    [Arguments(13734u, EmotionBook)] // /sit
    [Arguments(16210u, NoBook)]      // Charged Bolt: refused by skill_reqs 417, not by this rule
    public async Task BlocksSkill_WhileTransformed_LeavesUtilityAndNonCombatSkills(uint skillId, byte book)
    {
        await Assert.That(CharTransformCombatRules.BlocksSkill(isCharTransformed: true, skillId, book)).IsFalse();
    }

    [Test]
    public async Task BlocksSkill_WhenNotTransformed_IsFalse()
    {
        await Assert.That(CharTransformCombatRules.BlocksSkill(isCharTransformed: false, 16064, AttackBook)).IsFalse();
    }

    [Test]
    [Arguments(0, true)]     // never told before
    [Arguments(1000, false)] // the client's auto-attack retries about once a second
    [Arguments(4999, false)]
    [Arguments(5000, true)]
    public async Task ShouldNotifyRefusal_AtMostOncePerWindow(int msSinceLast, bool expected)
    {
        var now = new DateTime(2026, 11, 10, 12, 30, 0, DateTimeKind.Utc);
        var last = msSinceLast == 0 ? DateTime.MinValue : now.AddMilliseconds(-msSinceLast);

        await Assert.That(CharTransformCombatRules.ShouldNotifyRefusal(last, now)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(32459u, 50650u)] // Daru Transformation (skill): plot 7031 event 65200 puts 50650 on 4 h at expiry
    [Arguments(32643u, 0u)]     // Daru Transformation from Beyorn's Clinical Potion: no skill cooldown
    [Arguments(1u, 0u)]
    public async Task ExpiryCooldown_OnlyForTheSkillForm(uint buffId, uint expectedSkill)
    {
        var cooldown = CharTransformCombatRules.ExpiryCooldown(buffId);

        await Assert.That(cooldown.SkillId).IsEqualTo(expectedSkill);
        await Assert.That(cooldown.DurationMs).IsEqualTo(expectedSkill == 0 ? 0u : 14_400_000u);
    }

    [Test]
    public async Task DisarmamentPacket_UsesTheClientOpcode()
    {
        var packet = new SCDisarmamentChangedPacket(0x123456, true, CharTransformCombatRules.DisarmedSlots);

        await Assert.That(packet.TypeId).IsEqualTo((ushort)0x2E9);
    }

    [Test]
    public async Task DisarmamentPacket_WritesUnitFlagThenFourSlotsPaddedWithMinusOne()
    {
        var stream = new SCDisarmamentChangedPacket(0x123456, true, CharTransformCombatRules.DisarmedSlots)
            .Write(new PacketStream());
        stream.Rollback();

        await Assert.That(stream.ReadBc()).IsEqualTo(0x123456u);
        await Assert.That(stream.ReadBoolean()).IsTrue();
        sbyte[] expected = [15, 16, 17, -1];
        foreach (var slot in expected)
            await Assert.That(stream.ReadSByte()).IsEqualTo(slot);
        await Assert.That(stream.LeftBytes).IsEqualTo(0);
    }

    [Test]
    public async Task DisarmamentPacket_Release_WritesFalseWithTheSameSlots()
    {
        var stream = new SCDisarmamentChangedPacket(7, false, CharTransformCombatRules.DisarmedSlots)
            .Write(new PacketStream());
        stream.Rollback();

        stream.ReadBc();
        await Assert.That(stream.ReadBoolean()).IsFalse();
        await Assert.That(stream.ReadSByte()).IsEqualTo((sbyte)15);
    }

    [Test]
    public async Task DisarmamentPacket_MoreThanFourSlots_Throws()
    {
        EquipmentItemSlot[] five =
        [
            EquipmentItemSlot.Mainhand, EquipmentItemSlot.Offhand, EquipmentItemSlot.Ranged,
            EquipmentItemSlot.Musical, EquipmentItemSlot.Head
        ];

        await Assert.That(() => new SCDisarmamentChangedPacket(1, true, five)).Throws<ArgumentException>();
    }
}
