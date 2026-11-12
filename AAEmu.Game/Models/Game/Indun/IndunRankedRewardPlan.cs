namespace AAEmu.Game.Models.Game.Indun;

/// <summary>Who gets a rank reward and at which rank, fixed when delivery first starts so a retry cannot change it.</summary>
public sealed record IndunRankedRewardPlan(IReadOnlyList<(IndunRewardRecipient Recipient, int Rank)> Entries)
{
    public static IndunRankedRewardPlan Build(
        IReadOnlyList<IndunRewardRecipient> recipients,
        IReadOnlyDictionary<ulong, IReadOnlyDictionary<uint, uint>> gainPoints)
    {
        var ranks = InstanceRewardRankRules.Rank(recipients.Select(r => r.Id), gainPoints);
        return new IndunRankedRewardPlan(recipients.Select(r => (r, ranks[r.Id])).ToArray());
    }
}

/// <summary>One <see cref="IndunRankedRewardPlan"/> per reward kind of a copy.</summary>
public sealed class IndunRankedRewardPlans
{
    private readonly Dictionary<uint, IndunRankedRewardPlan> _plans = [];
    private readonly object _lock = new();

    public IndunRankedRewardPlan GetOrCreate(uint instanceRewardKindId, Func<IndunRankedRewardPlan> build)
    {
        lock (_lock)
        {
            if (_plans.TryGetValue(instanceRewardKindId, out var plan))
                return plan;

            plan = build();
            if (plan.Entries.Count > 0)
                _plans[instanceRewardKindId] = plan;
            return plan;
        }
    }
}
