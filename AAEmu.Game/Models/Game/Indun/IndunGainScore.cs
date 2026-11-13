namespace AAEmu.Game.Models.Game.Indun;

/// <summary>Which instance_gain_rules a doodad phase change scores.</summary>
public static class IndunGainRules
{
    public static IReadOnlyList<uint> RulesFor(
        uint doodadTemplateId,
        uint funcGroupId,
        IEnumerable<InstanceGainRule> rules,
        IEnumerable<InstancePointDoodadPhaseChange> targets)
    {
        var hit = targets
            .Where(t => t.DoodadAlmightyId == doodadTemplateId && t.DoodadFuncGroupId == funcGroupId)
            .Select(t => t.Id)
            .ToHashSet();
        if (hit.Count == 0)
            return [];

        return rules
            .Where(r => r.TargetType == InstanceGainRuleTargetType.InstancePointDoodadPhaseChange && hit.Contains(r.TargetId))
            .Select(r => r.Id)
            .ToList();
    }
}

/// <summary>A copy's points per character per gain rule (the client's gainRuleInfo).</summary>
public sealed class IndunGainScoreboard
{
    private readonly Lock _lock = new();
    private readonly Dictionary<ulong, Dictionary<uint, uint>> _points = [];

    public void Add(ulong characterId, uint gainRuleId, uint points = 1)
    {
        lock (_lock)
        {
            if (!_points.TryGetValue(characterId, out var byRule))
                _points[characterId] = byRule = [];
            byRule[gainRuleId] = byRule.GetValueOrDefault(gainRuleId) + points;
        }
    }

    public IReadOnlyDictionary<ulong, IReadOnlyDictionary<uint, uint>> Snapshot()
    {
        lock (_lock)
        {
            return _points.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyDictionary<uint, uint>)new Dictionary<uint, uint>(kv.Value));
        }
    }
}
