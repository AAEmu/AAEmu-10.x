using AAEmu.Game.Core.Packets;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Char.Templates;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects;

/// <summary>char_transform_effects: shows a character as another race (Daru Transformation) via the visual-race override.</summary>
public class CharTransformEffect : EffectTemplate
{
    public int CharRaceId { get; set; }
    public int CharGenderId { get; set; }
    public bool IsTransform { get; set; }

    public override bool OnActionTime => false;

    public override void Apply(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj, EffectSource source, SkillObject skillObject, DateTime time,
        CompressedGamePackets packetBuilder = null)
    {
        if (target is not Character character || character.ModelParams == null)
            return;

        character.IsCharTransformed = IsTransform;
        character.CharTransformRace = IsTransform ? (byte)CharRaceId : (byte)0;
        character.CharTransformGender = IsTransform ? (byte)CharGenderId : (byte)0;
        if (!IsTransform)
            VisualRaceBodyParts.ClearVisualRace(character.ModelParams);

        Logger.Debug($"CharTransformEffect: {character.Name} transform {IsTransform} race {CharRaceId} gender {CharGenderId}");

        if (ShouldAnnounce(character))
            AnnounceForm(character);
    }

    // A buff restored at login runs before the client has the player unit; CSNotifyInGame announces it instead.
    public static bool ShouldAnnounce(Character character) => character.IsOnline;

    public static void AnnounceForm(Character character)
    {
        var transformed = character.IsCharTransformed;
        var parts = FormParts(character, GetForm);

        character.BroadcastPacket(
            new SCCharacterVisualRaceModifiedPacket(character.Id, parts, character.ModelParams), true);
        character.BroadcastPacket(
            new SCDisarmamentChangedPacket(character.ObjId, transformed, CharTransformCombatRules.DisarmedSlots), true);

        // Same items, but isCharTransform makes the client rebuild the model for the form.
        var weapons = CharTransformCombatRules.DisarmedSlots
            .Select(slot => ((byte)slot, character.Equipment?.GetItemBySlot((int)slot)))
            .ToArray();
        character.BroadcastPacket(new SCUnitEquipmentsChangedPacket(character.ObjId, weapons, transformed), true);

    }

    /// <summary>A newly arriving observer: the unit state carries the real body parts, so send the form's.</summary>
    public static void ShowFormTo(Character character, Character observer) => ShowFormTo(character, observer, GetForm);

    internal static void ShowFormTo(Character character, Character observer, Func<Race, Gender, CharacterTemplate> getForm)
    {
        if (!character.IsCharTransformed)
            return;

        observer.SendPacket(new SCCharacterVisualRaceModifiedPacket(
            character.Id, FormParts(character, getForm), character.ModelParams));
        observer.SendPacket(new SCDisarmamentChangedPacket(character.ObjId, true, CharTransformCombatRules.DisarmedSlots));
    }

    private static CharacterTemplate GetForm(Race race, Gender gender) =>
        Core.Managers.UnitManagers.CharacterManager.Instance.GetTemplate(race, gender);

    private static VisualRaceBodyParts FormParts(Character character, Func<Race, Gender, CharacterTemplate> getForm)
    {
        if (!character.IsCharTransformed)
        {
            VisualRaceBodyParts.ClearVisualRace(character.ModelParams);
            return VisualRaceBodyParts.FromEquipment(slot =>
                character.Equipment?.GetItemBySlot((int)slot)?.TemplateId ?? 0u);
        }

        var form = getForm((Race)character.CharTransformRace, (Gender)character.CharTransformGender);
        if (form == null)
        {
            Logger.Warn($"CharTransformEffect: no characters template for race {character.CharTransformRace} gender {character.CharTransformGender}");
            return VisualRaceBodyParts.None;
        }

        VisualRaceBodyParts.ApplyVisualRace(character.ModelParams, character.CharTransformRace, character.CharTransformGender);
        return VisualRaceBodyParts.FromTemplate(form.FaceItemId, form.Items);
    }
}
