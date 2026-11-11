using System.Collections.Concurrent;

namespace AAEmu.World.Core.Relay;

/// <summary>
/// A dungeon copy's zone keeps the ids of units it removed for as long as the copy lives, so World must not
/// hand them to new units there; they go back to the pool when the copy closes. Open-world ids are not held.
/// </summary>
public sealed class CopyObjectIdHold
{
    private readonly ConcurrentDictionary<(uint ZoneId, uint InstanceId), ConcurrentDictionary<uint, byte>> _held = new();

    public bool TryHold(uint zoneId, uint instanceId, uint objId)
    {
        if (instanceId == 0 || objId == 0)
            return false;
        _held.GetOrAdd((zoneId, instanceId), _ => new ConcurrentDictionary<uint, byte>())[objId] = 0;
        return true;
    }

    public IReadOnlyCollection<uint> Release(uint zoneId, uint instanceId) =>
        _held.TryRemove((zoneId, instanceId), out var ids) ? ids.Keys.ToArray() : [];
}
