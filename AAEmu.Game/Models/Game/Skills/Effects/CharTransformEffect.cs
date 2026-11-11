using AAEmu.Game.Core.Packets;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
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
        var parts = VisualRaceBodyParts.None;
        if (transformed)
        {
            var form = Core.Managers.UnitManagers.CharacterManager.Instance
                .GetTemplate((Race)character.CharTransformRace, (Gender)character.CharTransformGender);
            if (form == null)
            {
                Logger.Warn($"CharTransformEffect: no characters template for race {character.CharTransformRace} gender {character.CharTransformGender}");
            }
            else
            {
                VisualRaceBodyParts.ApplyVisualRace(character.ModelParams, character.CharTransformRace, character.CharTransformGender);
                parts = VisualRaceBodyParts.FromTemplate(form.FaceItemId, form.Items);
            }
        }
        else
        {
            VisualRaceBodyParts.ClearVisualRace(character.ModelParams);
            parts = VisualRaceBodyParts.FromEquipment(slot =>
                character.Equipment?.GetItemBySlot((int)slot)?.TemplateId ?? 0u);
        }

        character.BroadcastPacket(
            new SCCharacterVisualRaceModifiedPacket(character.Id, parts, character.ModelParams), true);
        character.BroadcastPacket(
            new SCDisarmamentChangedPacket(character.ObjId, transformed, CharTransformCombatRules.DisarmedSlots), true);

        // Same items, but isCharTransform makes the client rebuild the model for the form.
        var weapons = CharTransformCombatRules.DisarmedSlots
            .Select(slot => ((byte)slot, character.Equipment?.GetItemBySlot((int)slot)))
            .ToArray();
        character.BroadcastPacket(new SCUnitEquipmentsChangedPacket(character.ObjId, weapons, transformed), true);

        if (character.Equipment?.GetItemBySlot((int)EquipmentItemSlot.RaceCosplay) != null)
            character.UpdateGearBonuses(null, null);
        CharTransformMountRules.Refresh(character);
    }
}
