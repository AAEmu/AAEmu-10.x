using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>
/// The six body-part item ids SCCharacterVisualRaceModifiedPacket carries, in the order the 10.0.2.13
/// client writes them into its equipment records: Face (19), Body (24), Beard (25), Tail (23),
/// Hair (20), Horns (the Reserved slot, 22).
/// </summary>
public sealed record VisualRaceBodyParts(uint Face, uint Body, uint Beard, uint Tail, uint Hair, uint Horns)
{
    public static VisualRaceBodyParts None { get; } = new(0, 0, 0, 0, 0, 0);

    /// <summary>
    /// A transform form's parts from its <c>characters</c> template: <paramref name="faceItemId"/> is
    /// <c>face_item_id</c>, and <paramref name="templateItems"/> is the template's <c>item_body_parts</c> array,
    /// indexed by <c>slot_type_id - 23</c> (Face, Hair, Glasses, Horns, Tail, Body, Beard).
    /// </summary>
    public static VisualRaceBodyParts FromTemplate(uint faceItemId, IReadOnlyList<uint> templateItems)
    {
        uint At(int index) => templateItems != null && index < templateItems.Count ? templateItems[index] : 0u;

        return new VisualRaceBodyParts(
            Face: faceItemId != 0 ? faceItemId : At(0),
            Body: At(5),
            Beard: At(6),
            Tail: At(4),
            Hair: At(1),
            Horns: At(3));
    }

    /// <summary>The character's own parts, read from its equipped appearance slots.</summary>
    public static VisualRaceBodyParts FromEquipment(Func<EquipmentItemSlot, uint> templateIdBySlot) =>
        new(
            Face: templateIdBySlot(EquipmentItemSlot.Face),
            Body: templateIdBySlot(EquipmentItemSlot.Body),
            Beard: templateIdBySlot(EquipmentItemSlot.Beard),
            Tail: templateIdBySlot(EquipmentItemSlot.Tail),
            Hair: templateIdBySlot(EquipmentItemSlot.Hair),
            Horns: templateIdBySlot(EquipmentItemSlot.Reserved));

    /// <summary>
    /// Sets the visual-race override. The client draws VisualRace/VisualGender instead of the real race
    /// while VisualRace is non-zero. Expiry stays 0, because the server ends the form itself.
    /// </summary>
    public static void ApplyVisualRace(UnitCustomModelParams appearance, byte race, byte gender)
    {
        appearance.VisualRace = race;
        appearance.VisualGender = gender;
        appearance.VisualRaceExpiredTime = 0;
    }

    /// <summary>
    /// Load-time cleanup: a skill transform (an override with no expiry) does not survive a relog, because
    /// the buff that would end it is not there to do so. A timed visual race (a race-change item) keeps its
    /// expiry and stays.
    /// </summary>
    public static void ClearTransientVisualRace(UnitCustomModelParams appearance)
    {
        if (appearance.VisualRace != 0 && appearance.VisualRaceExpiredTime == 0)
            ClearVisualRace(appearance);
    }

    /// <summary>Removes the visual-race override, so the client draws the real race again.</summary>
    public static void ClearVisualRace(UnitCustomModelParams appearance)
    {
        appearance.VisualRace = 0;
        appearance.VisualGender = 0;
        appearance.VisualRaceExpiredTime = 0;
    }
}
