using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadKeptChoiceTests
{
    private const string Use = nameof(DoodadFuncUse);

    private sealed class KeepingDoodad : Doodad
    {
        internal override bool KeepsAsChoice(DoodadFunc func) => true;
    }

    // A choice use (next_phase -1) beside a use that advances the doodad, as on the Contaminated Well.
    private static bool Keeps(uint funcSkillId, uint scheduledSkillId, params SkillTemplate[] skills)
    {
        var choice = new DoodadFunc { FuncId = 1, FuncType = Use, NextPhase = -1, SkillId = funcSkillId };
        var advance = new DoodadFunc { FuncId = 2, FuncType = Use, NextPhase = 9_000_502 };
        var templates = new Dictionary<uint, DoodadFuncTemplate>
        {
            [1] = new DoodadFuncUse { SkillId = scheduledSkillId },
            [2] = new DoodadFuncUse()
        };
        return Doodad.KeepsAsChoice(choice, [choice, advance], (id, _) => templates.GetValueOrDefault(id),
            id => skills.FirstOrDefault(s => s.Id == id));
    }

    private static SkillTemplate GrantingSkill(uint id) => new()
    {
        Id = id,
        Effects = [new SkillEffect { Template = new GainLootPackItemEffect { LootPackId = 1 } }]
    };

    [Test]
    public async Task AKeptChoice_DoesNotAskForAPhaseChange()
    {
        // A kept choice used to leave ToNextPhase set, so Use re-ran the unchanged phase and restarted its timer.
        var doodad = new KeepingDoodad { Template = new DoodadTemplate { FuncGroups = [] } };
        var character = new Character(new UnitCustomModelParams()) { Id = 7, Name = "Tester" };
        var choice = new DoodadFunc { FuncType = Use, NextPhase = -1 };

        doodad.DoFuncWithApply(character, choice, (_, owner) => owner.ToNextPhase = true);

        await Assert.That(doodad.ToNextPhase).IsFalse();
    }

    [Test]
    public async Task AChoice_WhoseInteractionSkillGrantsNothing_IsKept()
    {
        await Assert.That(Keeps(0, 0)).IsTrue();
    }

    [Test]
    public async Task AChoice_WhoseFollowUpSkillGrantsItems_IsConsumed()
    {
        // The follow-up skill is DoodadFuncUse.SkillId, a different column from doodad_funcs.func_skill_id.
        await Assert.That(Keeps(0, 500, GrantingSkill(500))).IsFalse();
    }

    [Test]
    public async Task AChoice_WhoseFuncSkillGrantsItems_IsConsumed()
    {
        await Assert.That(Keeps(501, 0, GrantingSkill(501))).IsFalse();
    }
}
