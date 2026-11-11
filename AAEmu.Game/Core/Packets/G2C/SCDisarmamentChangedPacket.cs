using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Core.Packets.G2C;

/// <summary>Disarms or re-arms equipment slots: bc unitId, bool use, 4 x sbyte slot (-1 = none).</summary>
public class SCDisarmamentChangedPacket : GamePacket
{
    private const int SlotCount = 4;

    private readonly uint _objId;
    private readonly bool _use;
    private readonly sbyte[] _slots;

    public SCDisarmamentChangedPacket(uint objId, bool use, IReadOnlyList<EquipmentItemSlot> slots)
        : base(SCOffsets.SCDisarmamentChangedPacket, 1)
    {
        ArgumentNullException.ThrowIfNull(slots);
        if (slots.Count > SlotCount)
            throw new ArgumentException($"The client holds at most {SlotCount} disarmed slots", nameof(slots));

        _objId = objId;
        _use = use;
        _slots = new sbyte[SlotCount];
        for (var i = 0; i < SlotCount; i++)
            _slots[i] = i < slots.Count ? (sbyte)slots[i] : (sbyte)-1;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId);
        stream.Write(_use);
        foreach (var slot in _slots)
            stream.Write(slot);
        return stream;
    }
}
