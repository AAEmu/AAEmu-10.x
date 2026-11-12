namespace AAEmu.World.Core.Relay;

/// <summary>
/// A dungeon copy's zone keeps the ids of units it removed for as long as the copy lives, so World must not
/// hand them to new units there; they go back to the pool when the copy closes. Only copies the zone host
/// supervisor started are held: open-world and long-lived system instance ids are not.
/// </summary>
public sealed class CopyObjectIdHold(Func<uint, bool> isSupervisedCopy)
{
    private readonly object _lock = new();
    private readonly Dictionary<(uint ZoneId, uint InstanceId), HashSet<uint>> _held = [];
    private readonly HashSet<(uint ZoneId, uint InstanceId)> _closed = [];

    /// <summary>False when the id is not held; a copy already drained takes no new holds.</summary>
    public bool TryHold(uint zoneId, uint instanceId, uint objId)
    {
        if (instanceId == 0 || objId == 0 || !isSupervisedCopy(instanceId))
            return false;

        lock (_lock)
        {
            if (_closed.Contains((zoneId, instanceId)))
                return false;
            if (!_held.TryGetValue((zoneId, instanceId), out var ids))
                _held[(zoneId, instanceId)] = ids = [];
            ids.Add(objId);
            return true;
        }
    }

    /// <summary>Closes the copy to new holds and returns what it held.</summary>
    public IReadOnlyCollection<uint> Release(uint zoneId, uint instanceId)
    {
        if (instanceId == 0 || !isSupervisedCopy(instanceId))
            return [];

        lock (_lock)
        {
            _closed.Add((zoneId, instanceId));
            return _held.Remove((zoneId, instanceId), out var ids) ? [.. ids] : [];
        }
    }
}
