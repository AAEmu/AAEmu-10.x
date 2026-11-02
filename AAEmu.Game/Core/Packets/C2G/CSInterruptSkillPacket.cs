using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

/// <remarks>
/// <c>u32 type</c>. The 10.0.2.13 client's InterrupSkillPacket: its cancel-current-action path sends
/// CSStopCasting while a cast is running and this otherwise (type 0 in every live capture, mounted or not).
/// Registered so it stops arriving as an unknown opcode; it does not dismount. Right-clicking the mount
/// you ride arrives as CSStartInteraction on the mate.
/// </remarks>
public class CSInterruptSkillPacket() : GamePacket(CSOffsets.CSInterruptSkillPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var type = stream.ReadUInt32();

        Logger.Debug("InterruptSkill type={0} from {1}", type, Connection.ActiveChar?.Name);
    }
}
