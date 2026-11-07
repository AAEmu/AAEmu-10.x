using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Stream;

namespace AAEmu.Game.Core.Packets.S2C;

public readonly record struct NameCacheKey(ushort CacheType, ulong Id, byte WorldId, uint ExpeditionId)
{
    public const ushort CharacterName = 1;

    public void Write(PacketStream stream)
    {
        stream.Write(CacheType);
        stream.Write(Id);
        stream.Write(WorldId);
        stream.Write(ExpeditionId);
    }
}

/// <summary>TCCharInfoQueried: the query's key, then the cached info (cacheType, lastCachedTime, name).</summary>
public class TCCharNameQueriedPacket(NameCacheKey key, string name) : StreamPacket(TCOffsets.TCCharNameQueriedPacket)
{
    public override PacketStream Write(PacketStream stream)
    {
        key.Write(stream);
        stream.Write(NameCacheKey.CharacterName);
        stream.Write(0L); // lastCachedTime, set by the client when it stores the entry
        stream.Write(name);

        return stream;
    }
}
