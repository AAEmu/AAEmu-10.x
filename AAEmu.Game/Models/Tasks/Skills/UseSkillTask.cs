using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Static;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Tasks.Skills;

public class UseSkillTask : Task
{
    private readonly Skill _skill;
    private readonly BaseUnit _caster;
    private readonly SkillCaster _casterCaster;
    private readonly SkillCastTarget _targetCaster;
    private readonly SkillObject _skillObject;
    private readonly Action<SkillResult> _onResult;

    /// <param name="onResult">Told how the skill use went, <see cref="SkillResult.Failure"/> when it threw.</param>
    public UseSkillTask(Skill skill, BaseUnit caster, SkillCaster casterCaster, BaseUnit target, SkillCastTarget targetCaster, SkillObject skillObject,
        Action<SkillResult> onResult = null)
    {
        _skill = skill;
        _caster = caster;
        _casterCaster = casterCaster;
        _targetCaster = targetCaster;
        _skillObject = skillObject;
        _onResult = onResult;
    }

    public override void Execute()
    {
        var result = SkillResult.Failure;
        try
        {
            result = _skill.Use(_caster, _casterCaster, _targetCaster, _skillObject, true, out _);
        }
        finally
        {
            _onResult?.Invoke(result);
        }
    }
}
