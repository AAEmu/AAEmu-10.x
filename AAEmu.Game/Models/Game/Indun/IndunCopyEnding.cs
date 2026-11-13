namespace AAEmu.Game.Models.Game.Indun;

/// <summary>What a copy's clock asks for on one tick.</summary>
public readonly record struct IndunCopyStep(IndunInstancePhase Phase, int SecondsLeft, int? WarnSeconds, bool Close);

/// <summary>The outcome of asking a copy to take one more player.</summary>
public enum IndunEntryVerdict { Admitted, CopyClosing, OverLimit }

/// <summary>
/// The end of a scripted copy: a finished script cuts the play phase short, the end window is counted down
/// with warnings, then the copy closes. Holds the state only; the copy acts on each <see cref="IndunCopyStep"/>.
/// </summary>
public sealed class IndunCopyEnding(IReadOnlyList<int> warningSeconds)
{
    private int _lastWarning = int.MaxValue;
    private bool _closing;

    public DateTime? ScriptEndedUtc { get; private set; }

    /// <summary>Set by the step that closes the copy; from then on nobody may enter it.</summary>
    public bool IsClosing => _closing;

    /// <summary>
    /// Takes an entry unless the copy is closing; <paramref name="charge"/> only runs for a copy that is
    /// still open, so a refused entry is never charged. Call under the copy's lock.
    /// </summary>
    public IndunEntryVerdict TryAdmit(Func<bool> charge)
        => _closing ? IndunEntryVerdict.CopyClosing
            : charge() ? IndunEntryVerdict.Admitted : IndunEntryVerdict.OverLimit;

    public void MarkScriptEnded(DateTime now) => ScriptEndedUtc ??= now;

    public IndunCopyStep Step(IndunZoneOption option, DateTime copyStartUtc, DateTime now)
    {
        var (phase, secondsLeft) = IndunInstancePhaseRules.StateAt(option, copyStartUtc, now, ScriptEndedUtc);

        int? warn = null;
        if (phase == IndunInstancePhase.End
            && IndunCloseWarningRules.Next(secondsLeft, _lastWarning, warningSeconds) is { } due)
        {
            _lastWarning = due;
            warn = due;
        }

        var close = phase == IndunInstancePhase.Finished && !_closing;
        if (close)
            _closing = true;

        return new IndunCopyStep(phase, secondsLeft, warn, close);
    }
}
