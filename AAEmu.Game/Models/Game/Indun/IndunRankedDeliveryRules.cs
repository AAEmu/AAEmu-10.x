using AAEmu.Game.Core.Managers;

namespace AAEmu.Game.Models.Game.Indun;

/// <summary>A rank reward is per player: it is complete only when every recipient got or already had theirs.</summary>
public static class IndunRankedDeliveryRules
{
    public static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(15);
    public const int MaxRetries = 20;

    /// <summary>Only a failed mail can succeed later; a rank without an authored reward never will.</summary>
    public static bool IsRetryable(IndunRewardDeliveryResult recipientResult)
        => recipientResult == IndunRewardDeliveryResult.Failed;

    public static bool MayRetryAgain(int attemptsSoFar) => attemptsSoFar < MaxRetries;

    public static IndunRewardDeliveryResult Outcome(int delivered, int claimed, int failed)
    {
        if (failed > 0)
            return delivered + claimed > 0 ? IndunRewardDeliveryResult.PartiallyDelivered : IndunRewardDeliveryResult.Failed;
        if (delivered > 0)
            return IndunRewardDeliveryResult.Delivered;
        return claimed > 0 ? IndunRewardDeliveryResult.AlreadyClaimed : IndunRewardDeliveryResult.NoRecipients;
    }

    public static bool NeedsRetry(IndunRewardDeliveryResult result)
        => result is IndunRewardDeliveryResult.PartiallyDelivered or IndunRewardDeliveryResult.Failed;
}
