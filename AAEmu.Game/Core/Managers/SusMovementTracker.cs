using System.Numerics;

namespace AAEmu.Game.Core.Managers;

/// <summary>
/// Last seen position and skip time per object id for the movement speed checks of <see cref="SusManager"/>.
/// Players' packet threads and the bots' tick thread analyze and reset at the same time, so every call holds the
/// lock for its whole read-modify-write of an entry. Nothing slow runs under it: the caller logs a flagged speed
/// after the call has returned.
/// </summary>
public class SusMovementTracker
{
    /// <summary>
    /// Seconds an id is left alone after it was flagged
    /// </summary>
    public const float SkipTimeAfterFlag = 15f;

    private readonly object _lock = new();
    private readonly Dictionary<uint, (Vector3 pos, float skipTime)> _lastPositions = [];

    /// <summary>
    /// Number of ids currently tracked
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _lastPositions.Count;
        }
    }

    /// <summary>
    /// Records the new position of an id and returns the observed flat speed if it is above maxSpeed.
    /// The first observation of an id (or the first after a reset) only remembers the position.
    /// </summary>
    /// <param name="id">Character or mate Id</param>
    /// <param name="position">Current world position</param>
    /// <param name="mayCheck">False while the speed can't be judged (riding, position disabled), the position is still recorded</param>
    /// <param name="deltaTime">Seconds since the previous observation</param>
    /// <param name="maxSpeed">Highest flat speed in m/s that is not flagged</param>
    /// <returns>The observed speed in m/s if it should be reported, otherwise null</returns>
    public float? Analyze(uint id, Vector3 position, bool mayCheck, float deltaTime, double maxSpeed)
    {
        lock (_lock)
        {
            if (!_lastPositions.TryGetValue(id, out var last))
            {
                _lastPositions.Add(id, (position, 0f));
                return null;
            }

            var deltaPos = position - last.pos;
            var deltaFlatPos = deltaPos with { Z = 0 };
            float? flaggedSpeed = null;

            if (mayCheck &&
                deltaFlatPos != Vector3.Zero &&
                last.skipTime <= 0f)
            {
                var observedSpeed = deltaFlatPos.Length() / deltaTime;
                if (observedSpeed > maxSpeed)
                {
                    last.skipTime = SkipTimeAfterFlag;
                    flaggedSpeed = observedSpeed;
                }
            }

            last.skipTime -= deltaTime;
            _lastPositions[id] = (position, last.skipTime);
            return flaggedSpeed;
        }
    }

    /// <summary>
    /// Forgets an id, its next observation only remembers the position again
    /// </summary>
    /// <param name="id"></param>
    public void Reset(uint id)
    {
        lock (_lock)
            _ = _lastPositions.Remove(id);
    }

    /// <summary>
    /// Reads the tracked state of an id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="position">Last recorded position</param>
    /// <param name="skipTime">Seconds left before the id is checked again, zero or less when it will be</param>
    /// <returns>False if the id isn't tracked</returns>
    public bool TryGet(uint id, out Vector3 position, out float skipTime)
    {
        lock (_lock)
        {
            if (_lastPositions.TryGetValue(id, out var last))
            {
                position = last.pos;
                skipTime = last.skipTime;
                return true;
            }
        }

        position = Vector3.Zero;
        skipTime = 0f;
        return false;
    }
}
