using System.Numerics;

namespace AAEmu.Game.Models.Game.Teleport;

/// <summary>A landing the server sent a client to: the world spot, and the world and instance it is in.</summary>
public readonly record struct ExpectedTeleportLanding(uint WorldId, uint InstanceId, Vector3 Position);

/// <summary>How a client's <c>CSTeleportEnded</c> compares with the landings it was sent to.</summary>
public enum TeleportEndedCheck : byte
{
    /// <summary>No landing waits for this confirmation in the character's world and instance: applied as reported.</summary>
    NoneExpected,

    /// <summary>The reported spot is within the tolerance of a landing that waits: applied as reported.</summary>
    WithinTolerance,

    /// <summary>The reported spot is farther than the tolerance from every landing that waits.</summary>
    FarOff,
}

/// <summary>
/// The landings <c>SkillTeleportLanding</c> sent one character's client to that it has not confirmed yet. A client
/// confirms a landing with <c>CSTeleportEnded</c> and reports the spot it stands on, which the server used to take
/// as it came; a modified client could name any spot. Every landing is kept until the next confirmation, as a
/// second landing can be sent before the client confirmed the first, and the confirmation counts as honest when it
/// is near any of them. The landings are given up when the position lock is released (<c>DisabledSetPosition</c>):
/// a confirmation, an instance load's arrival or a resurrection ends the wait, so a later move the server makes some
/// other way is never measured against them. Players' packet threads and the threads that land characters (the
/// bots' tick, a zone start) meet here, so every call holds the lock.
/// </summary>
public sealed class TeleportLandingWatch
{
    /// <summary>Landings kept at most; a longer run of unconfirmed landings drops the oldest.</summary>
    public const int MaxWaiting = 4;

    private readonly object _lock = new();
    private readonly List<ExpectedTeleportLanding> _waiting = [];

    /// <summary>Number of landings waiting for a confirmation.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _waiting.Count;
        }
    }

    /// <summary>The client was sent to <paramref name="landing"/> and will confirm it.</summary>
    public void Expect(ExpectedTeleportLanding landing)
    {
        lock (_lock)
        {
            if (_waiting.Count >= MaxWaiting)
                _waiting.RemoveAt(0);
            _waiting.Add(landing);
        }
    }

    /// <summary>Gives up every landing waiting for a confirmation.</summary>
    public void Forget()
    {
        lock (_lock)
            _waiting.Clear();
    }

    /// <summary>
    /// Takes the landings a confirmation answers and judges the spot it reports. Only landings in the character's
    /// world and instance count; with none of them, or a <paramref name="tolerance"/> of 0 or less, the check is
    /// off (<see cref="TeleportEndedCheck.NoneExpected"/>). Within <paramref name="tolerance"/> metres (3D) of any of
    /// them is honest. Farther from all of them is <see cref="TeleportEndedCheck.FarOff"/>, and
    /// <paramref name="sentSpot"/> is the latest of them, where the server last put the character.
    /// Every waiting landing is taken either way.
    /// </summary>
    public TeleportEndedCheck Take(uint worldId, uint instanceId, Vector3 reported, float tolerance, out Vector3 sentSpot)
    {
        List<ExpectedTeleportLanding> waiting;
        lock (_lock)
        {
            waiting = [.. _waiting];
            _waiting.Clear();
        }

        return Judge(waiting, worldId, instanceId, reported, tolerance, out sentSpot);
    }

    /// <summary>The judgement <see cref="Take"/> makes, on the landings given oldest first.</summary>
    public static TeleportEndedCheck Judge(IReadOnlyList<ExpectedTeleportLanding> waiting, uint worldId, uint instanceId,
        Vector3 reported, float tolerance, out Vector3 sentSpot)
    {
        sentSpot = reported;
        if (tolerance <= 0f)
            return TeleportEndedCheck.NoneExpected;

        ExpectedTeleportLanding? latest = null;
        foreach (var landing in waiting)
        {
            if (landing.WorldId != worldId || landing.InstanceId != instanceId)
                continue;
            if (Vector3.Distance(landing.Position, reported) <= tolerance)
            {
                sentSpot = landing.Position;
                return TeleportEndedCheck.WithinTolerance;
            }

            latest = landing;
        }

        if (latest is not { } far)
            return TeleportEndedCheck.NoneExpected;

        sentSpot = far.Position;
        return TeleportEndedCheck.FarOff;
    }
}
