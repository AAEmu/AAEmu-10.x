using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.DoodadObj.Static;

namespace AAEmu.Game.Core.Packets.C2G;

/// <remarks>
/// <c>u32 type</c>. The 10.0.2.13 client's InterrupSkillPacket: its cancel-current-action path sends
/// CSStopCasting while a cast is running and this otherwise. A right-click while riding a mount lands
/// here (type 0 in the live captures), and before this handler was registered the packet was dropped
/// as unknown, so right-click never dismounted.
/// </remarks>
public class CSInterruptSkillPacket() : GamePacket(CSOffsets.CSInterruptSkillPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var type = stream.ReadUInt32();

        var character = Connection.ActiveChar;
        if (character == null)
            return;

        var mate = character.ParentWorld.MateManager.GetIsMounted(character.ObjId, out var attachPoint);
        if (mate == null)
        {
            Logger.Debug("InterruptSkill type={0} from {1}: not riding a mount, ignored", type, character.Name);
            return;
        }

        Logger.Debug("InterruptSkill type={0} from {1}: dismounting from mount tl {2} seat {3}",
            type, character.Name, mate.TlId, attachPoint);
        character.ParentWorld.MateManager.UnMountMate(character, mate.TlId, attachPoint, AttachUnitReason.None);
    }
}
