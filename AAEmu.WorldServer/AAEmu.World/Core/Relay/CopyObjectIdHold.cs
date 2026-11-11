using System.Collections.Concurrent;

namespace AAEmu.World.Core.Relay;

/// <summary>
/// A dungeon copy's zone keeps the ids of units it removed for as long as the copy lives, so World must not
/// hand them to new units there; they go back to the pool when the copy closes. Only copies the zone host
/// supervisor started are held: open-world and long-lived system instance ids are not.
/// </summary>
public sealed class CopyObjectIdHold(Func<uint, bool> isSupervisedCopy)
{
    private readonly ConcurrentDictionary<(uint ZoneId, uint InstanceId), ConcurrentDictionary<uint, byte>> _held = new();

    public bool TryHold(uint zoneId, uint instanceId, uint objId)
    {
        if (instanceId == 0 || objId == 0 || !isSupervisedCopy(instanceId))
            return false;
        _held.GetOrAdd((zoneId, instanceId), _ => new ConcurrentDictionary<uint, byte>())[objId] = 0;
        return true;
    }

    public IReadOnlyCollection<uint> Release(uint zoneId, uint instanceId) =>
        _held.TryRemove((zoneId, instanceId), out var ids) ? ids.Keys.ToArray() : [];
}
