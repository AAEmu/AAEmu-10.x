using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>
/// Changes how a character is drawn (a transform such as Daru Transformation), for the owner and for
/// onlookers alike (10.0.2.13 <c>SCCharacterVisualRaceModifiedPacket</c>, 0x273).
/// </summary>
/// <remarks>
/// 10.0.2.13 serializer: u64 character id, six u32 body-part item ids, then the appearance block (the same
/// block SCCharacterGenderAndModelModifiedPacket and unit state carry). The client handler writes the six ids
/// into its equipment records Face, Body, Beard, Tail, Hair and Horns, in that order. It then copies the
/// appearance and draws <c>VisualRace</c>/<c>VisualGender</c> when VisualRace is non-zero, otherwise the real
/// race and gender. Unlike a re-sent unit state, this is safe to send to the owner.
/// </remarks>
public class SCCharacterVisualRaceModifiedPacket(ulong characterId, VisualRaceBodyParts parts, UnitCustomModelParams appearance)
    : GamePacket(SCOffsets.SCCharacterVisualRaceModifiedPacket, 1)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(characterId);
        stream.Write(parts.Face);
        stream.Write(parts.Body);
        stream.Write(parts.Beard);
        stream.Write(parts.Tail);
        stream.Write(parts.Hair);
        stream.Write(parts.Horns);
        stream.Write(appearance);
        return stream;
    }
}
