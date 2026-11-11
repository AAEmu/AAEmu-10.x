using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

public class IndunRankedDeliveryRulesTests
{
    [Test]
    [Arguments(2, 0, 0, IndunRewardDeliveryResult.Delivered)]
    [Arguments(1, 1, 0, IndunRewardDeliveryResult.Delivered)]
    [Arguments(0, 2, 0, IndunRewardDeliveryResult.AlreadyClaimed)]
    [Arguments(0, 0, 0, IndunRewardDeliveryResult.NoRecipients)]
    public async Task EveryoneServed_IsComplete(int delivered, int claimed, int failed, IndunRewardDeliveryResult expected)
    {
        await Assert.That(IndunRankedDeliveryRules.Outcome(delivered, claimed, failed)).IsEqualTo(expected);
    }

    [Test]
    public async Task AFailedRecipient_LeavesTheRewardOpenForARetry()
    {
        await Assert.That(IndunRankedDeliveryRules.Outcome(1, 0, 1)).IsEqualTo(IndunRewardDeliveryResult.PartiallyDelivered);
        await Assert.That(IndunRankedDeliveryRules.Outcome(0, 1, 1)).IsEqualTo(IndunRewardDeliveryResult.PartiallyDelivered);
        await Assert.That(IndunRankedDeliveryRules.Outcome(0, 0, 2)).IsEqualTo(IndunRewardDeliveryResult.Failed);
        await Assert.That(IndunRankedDeliveryRules.NeedsRetry(IndunRewardDeliveryResult.PartiallyDelivered)).IsTrue();
        await Assert.That(IndunRankedDeliveryRules.NeedsRetry(IndunRewardDeliveryResult.Failed)).IsTrue();
        await Assert.That(IndunRankedDeliveryRules.NeedsRetry(IndunRewardDeliveryResult.Delivered)).IsFalse();
        await Assert.That(IndunRankedDeliveryRules.NeedsRetry(IndunRewardDeliveryResult.NoRecipients)).IsFalse();
    }

    [Test]
    public async Task OnlyAFailedMailIsWorthRetrying()
    {
        // A rank without an authored reward row cannot succeed later: it is not a failure to retry.
        await Assert.That(IndunRankedDeliveryRules.IsRetryable(IndunRewardDeliveryResult.Failed)).IsTrue();
        await Assert.That(IndunRankedDeliveryRules.IsRetryable(IndunRewardDeliveryResult.SelectionUnavailable)).IsFalse();
        await Assert.That(IndunRankedDeliveryRules.IsRetryable(IndunRewardDeliveryResult.NoRecipients)).IsFalse();
    }

    [Test]
    public async Task Retries_StopAfterTheCap()
    {
        await Assert.That(IndunRankedDeliveryRules.MayRetryAgain(IndunRankedDeliveryRules.MaxRetries - 1)).IsTrue();
        await Assert.That(IndunRankedDeliveryRules.MayRetryAgain(IndunRankedDeliveryRules.MaxRetries)).IsFalse();
    }
}
