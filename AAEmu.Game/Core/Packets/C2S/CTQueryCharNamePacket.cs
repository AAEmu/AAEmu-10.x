using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Stream;
using AAEmu.Game.Core.Packets.S2C;

namespace AAEmu.Game.Core.Packets.C2S;

/// <summary>
/// Name-cache query (CTQueryCharInfo). The key is cacheType (1 = character name, 2 = abilities),
/// character id, worldId and an expedition id; the answer has to echo it unchanged.
/// </summary>
public class CTQueryCharNamePacket() : StreamPacket(CTOffsets.CTQueryCharNamePacket)
{
    public override void Read(PacketStream stream)
    {
        var key = new NameCacheKey(stream.ReadUInt16(), stream.ReadUInt64(), stream.ReadByte(), stream.ReadUInt32());
        if (key.CacheType != NameCacheKey.CharacterName)
        {
            Logger.Debug($"QueryCharInfo: cache type {key.CacheType} for {key.Id} not answered");
            return;
        }

        var name = NameManager.Instance.GetCharacterName((uint)key.Id);
        if (name != null)
            Connection.SendPacket(new TCCharNameQueriedPacket(key, name));

        Logger.Debug($"QueryCharName, Id: {key.Id}, Name: {name}");
    }
}
