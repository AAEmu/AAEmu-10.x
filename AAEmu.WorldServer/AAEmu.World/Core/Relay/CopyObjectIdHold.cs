using System.Runtime.CompilerServices;

namespace AAEmu.World.Core.Relay;

/// <summary>
/// A dungeon copy's zone keeps the ids of units it removed for as long as the copy lives, so World must not
/// hand them to new units there; they go back to the pool when the copy closes. Only copies the zone host
/// supervisor started are held: open-world and long-lived system instance ids are not.
/// Holds belong to the zone connection (the owner) that removed the unit. Instance ids are recycled, so a late
/// disconnect of an old copy must not touch a new copy that reuses its id.
/// </summary>
public sealed class CopyObjectIdHold(Func<uint, bool> isSupervisedCopy)
{
    private readonly object _lock = new();
    private readonly Dictionary<object, HashSet<uint>> _held = new(ReferenceEqualityComparer.Instance);
    private readonly ConditionalWeakTable<object, object> _closed = [];

    /// <summary>False when the id is not held; an owner already drained takes no new holds.</summary>
    public bool TryHold(object owner, uint instanceId, uint objId)
    {
        if (instanceId == 0 || objId == 0 || !isSupervisedCopy(instanceId))
            return false;

        lock (_lock)
        {
            if (_closed.TryGetValue(owner, out _))
                return false;
            if (!_held.TryGetValue(owner, out var ids))
                _held[owner] = ids = [];
            ids.Add(objId);
            return true;
        }
    }

    /// <summary>Stops holding an id that is live again, so closing the copy does not release it.</summary>
    public void Unhold(object owner, uint objId)
    {
        lock (_lock)
        {
            if (_held.TryGetValue(owner, out var ids))
                ids.Remove(objId);
        }
    }

    /// <summary>Closes the owner to new holds and returns what it held.</summary>
    public IReadOnlyCollection<uint> Release(object owner, uint instanceId)
    {
        if (instanceId == 0)
            return [];

        lock (_lock)
        {
            _closed.AddOrUpdate(owner, owner);
            return _held.Remove(owner, out var ids) ? [.. ids] : [];
        }
    }
}
