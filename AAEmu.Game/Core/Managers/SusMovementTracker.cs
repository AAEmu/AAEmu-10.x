using System.Numerics;

namespace AAEmu.Game.Core.Managers;

/// <summary>
/// Last seen position and skip time per object id for the movement speed checks of <see cref="SusManager"/>, and
/// the landing the server expects an id at after it sent the client there.
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

    /// <summary>
    /// Checks an expected landing is waited for. Moves the client sent before it made the jump still arrive from
    /// the old spot for about one round trip, and one check at most falls on them.
    /// </summary>
    public const int ExpectedLandingChecks = 3;

    private readonly object _lock = new();
    private readonly Dictionary<uint, (Vector3 pos, float skipTime)> _lastPositions = [];
    private readonly Dictionary<uint, (Vector3 landing, int checksLeft)> _expectedLandings = [];

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
    /// The first observation of an id (or the first after a reset) only remembers the position, and so does the one
    /// that reaches an expected landing (see <see cref="ExpectLanding"/>).
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
            var landed = ReachesExpectedLanding(id, position, deltaTime, maxSpeed);

            if (!_lastPositions.TryGetValue(id, out var last))
            {
                _lastPositions.Add(id, (position, 0f));
                return null;
            }

            var deltaPos = position - last.pos;
            var deltaFlatPos = deltaPos with { Z = 0 };
            float? flaggedSpeed = null;

            if (mayCheck &&
                !landed &&
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
        {
            _ = _lastPositions.Remove(id);
            _ = _expectedLandings.Remove(id);
        }
    }

    /// <summary>
    /// Forgets an id and records <paramref name="position"/> as the last position it was seen at, ready to be checked:
    /// the next observation is judged as movement from there.
    /// </summary>
    /// <param name="id">Character or mate Id</param>
    /// <param name="position">World position the server put the id at</param>
    public void Restart(uint id, Vector3 position)
    {
        lock (_lock)
        {
            _ = _expectedLandings.Remove(id);
            _lastPositions[id] = (position, 0f);
        }
    }

    /// <summary>
    /// The server has sent the id's client to <paramref name="landing"/> without writing that position itself: the
    /// client makes the jump when the packet arrives. The first observation that reaches the landing (no faster
    /// than the limit, measured from it) only remembers the position. Observations anywhere else are judged from
    /// the last recorded position as usual, so a client that does not go there gains nothing. The landing is given
    /// up after <see cref="ExpectedLandingChecks"/> of them, and replaces one still expected.
    /// </summary>
    /// <param name="id">Character or mate Id</param>
    /// <param name="landing">World position the client was sent to</param>
    public void ExpectLanding(uint id, Vector3 landing)
    {
        lock (_lock)
            _expectedLandings[id] = (landing, ExpectedLandingChecks);
    }

    /// <summary>
    /// Reads the landing expected for an id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="landing">World position the client was sent to</param>
    /// <returns>False if no landing is expected</returns>
    public bool TryGetExpectedLanding(uint id, out Vector3 landing)
    {
        lock (_lock)
        {
            if (_expectedLandings.TryGetValue(id, out var expected))
            {
                landing = expected.landing;
                return true;
            }
        }

        landing = Vector3.Zero;
        return false;
    }

    /// <summary>
    /// Under the lock. True when the id has an expected landing and the observation reaches it, which is then done
    /// with. An observation that does not reach it uses up one of its checks.
    /// </summary>
    private bool ReachesExpectedLanding(uint id, Vector3 position, float deltaTime, double maxSpeed)
    {
        if (!_expectedLandings.TryGetValue(id, out var expected))
            return false;

        var fromLanding = (position - expected.landing) with { Z = 0 };
        if (fromLanding.Length() / deltaTime <= maxSpeed)
        {
            _ = _expectedLandings.Remove(id);
            return true;
        }

        expected.checksLeft--;
        if (expected.checksLeft <= 0)
            _ = _expectedLandings.Remove(id);
        else
            _expectedLandings[id] = expected;
        return false;
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
