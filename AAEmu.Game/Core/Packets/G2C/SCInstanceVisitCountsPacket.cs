using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCInstanceVisitCountsPacket(IReadOnlyList<InstanceVisitCountRecord> records = null)
    : GamePacket(SCOffsets.SCInstanceVisitCountsPacket, 1)
{
    /// <summary>The client's visit-count list holds this many rows.</summary>
    public const int MaxRows = 50;

    private readonly IReadOnlyList<InstanceVisitCountRecord> _records = records ?? [];

    public override PacketStream Write(PacketStream stream)
    {
        var rows = _records.Take(MaxRows).ToList();
        stream.Write(rows.Count);
        foreach (var row in rows)
            WriteVisitRow(stream, row);

        return stream;
    }

    internal static void WriteVisitRow(PacketStream stream, InstanceVisitCountRecord row)
    {
        stream.Write(row.ZoneId);
        stream.Write(row.Data);
        stream.Write(row.UsedCount);
        stream.Write(row.ResetCount);
        stream.Write(row.PermittedCount);
    }
}
