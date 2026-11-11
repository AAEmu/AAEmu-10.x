using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadClientOverlayUseTests
{
    /// <summary>
    /// "The Captured Child" (quest 7145): the Abducted Child (doodad 14309) is a client doodad.
    /// Feeding the antidote on one player's overlay phase moved the shared phase to the rescued
    /// step, so every other player got "You cannot use this" (live 2026-11-02).
    /// </summary>
    [Test]
    public async Task OverlayUse_AdvancesOnlyThatViewer()
    {
        var doodad = new Doodad
        {
            Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] }
        };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "Rescuer" };
        var other = new Character(new UnitCustomModelParams()) { Id = 8, ObjId = 80, Name = "Waiting" };
        var sharedPhase = doodad.FuncGroupId;
        var func = new DoodadFunc { NextPhase = 41990 };

        var stop = doodad.DoFuncWithApply(user, func, (_, owner) => owner.ToNextPhase = true, user);

        await Assert.That(stop).IsTrue();
        await Assert.That(doodad.FuncGroupId).IsEqualTo(sharedPhase);
        await Assert.That(doodad.GetPhaseFor(user)).IsEqualTo(41990u);
        await Assert.That(doodad.GetPhaseFor(other)).IsEqualTo(sharedPhase);
        await Assert.That(doodad.ToNextPhase).IsFalse();
    }

    /// <summary>
    /// "One Last Job" (quest 7147): Ayle (client doodad 14246) ends on a use func with next_phase -1.
    /// One player's antidote deleted the shared Ayle for everyone (live 2026-11-02).
    /// </summary>
    [Test]
    public async Task FinalUseByPlayer_KeepsSharedClientDoodad()
    {
        var doodad = new Doodad
        {
            Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] }
        };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "Trouble" };
        var sharedPhase = doodad.FuncGroupId;
        var func = new DoodadFunc { NextPhase = -1 };

        var stop = doodad.DoFuncWithApply(user, func, (_, owner) => owner.ToNextPhase = true, user);

        await Assert.That(stop).IsTrue();
        await Assert.That(doodad.ToNextPhase).IsFalse();
        await Assert.That(doodad.FuncGroupId).IsEqualTo(sharedPhase);
    }

    /// <summary>
    /// The final use consumes that player's own copy: the same player cannot use it a second time,
    /// while every other player still can.
    /// </summary>
    [Test]
    public async Task FinalUseByPlayer_CannotBeRepeatedByTheSamePlayer()
    {
        var doodad = new Doodad
        {
            Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] }
        };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "First" };
        var other = new Character(new UnitCustomModelParams()) { Id = 8, ObjId = 80, Name = "Second" };
        var func = new DoodadFunc { NextPhase = -1 };
        var applied = new List<uint>();
        void Apply(BaseUnit caster, Doodad owner)
        {
            applied.Add(caster.Id);
            owner.ToNextPhase = true;
        }

        doodad.DoFuncWithApply(user, func, Apply);
        await Assert.That(doodad.IsConsumedFor(user)).IsTrue();
        await Assert.That(doodad.IsConsumedFor(other)).IsFalse();

        var stop = doodad.DoFuncWithApply(user, func, Apply);

        await Assert.That(stop).IsTrue();
        await Assert.That(applied).IsEquivalentTo(new[] { 7u });

        doodad.DoFuncWithApply(other, func, Apply);

        await Assert.That(applied).IsEquivalentTo(new[] { 7u, 8u });
        await Assert.That(doodad.IsConsumedFor(other)).IsTrue();
    }

    /// <summary>
    /// QuestReact moving the player's view to another phase ends the consumed state: the doodad is
    /// theirs to see, and use, again in the phase they moved to.
    /// </summary>
    [Test]
    public async Task ConsumedCopy_ReturnsWhenThatViewersPhaseMovesOn()
    {
        var doodad = new Doodad
        {
            Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] }
        };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "First" };
        var func = new DoodadFunc { NextPhase = -1 };
        var applied = 0;
        void Apply(BaseUnit caster, Doodad owner)
        {
            applied++;
            owner.ToNextPhase = true;
        }

        doodad.DoFuncWithApply(user, func, Apply);
        await Assert.That(doodad.IsConsumedFor(user)).IsTrue();

        doodad.SetQuestReactViewerPhase(user.ObjId, 41872);
        doodad.RefreshQuestReactFor(user);

        await Assert.That(doodad.IsConsumedFor(user)).IsFalse();
        await Assert.That(doodad.GetPhaseFor(user)).IsEqualTo(41872u);

        doodad.DoFuncWithApply(user, func, Apply);
        await Assert.That(applied).IsEqualTo(2);
    }

    /// <summary>
    /// The used-up mark belongs to the character, not to their stay in range: walking out of the
    /// region and back, or logging out and in (a new ObjId), does not hand the copy back.
    /// </summary>
    [Test]
    public async Task ConsumedCopy_StaysConsumedAfterLeavingRangeAndRelogging()
    {
        var doodad = new Doodad
        {
            Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] }
        };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "First" };
        var func = new DoodadFunc { NextPhase = -1 };
        var applied = 0;
        void Apply(BaseUnit caster, Doodad owner)
        {
            applied++;
            owner.ToNextPhase = true;
        }

        doodad.DoFuncWithApply(user, func, Apply);

        // Out of range and back in range.
        doodad.RemoveVisibleObject(user);
        doodad.AddVisibleObject(user);

        await Assert.That(doodad.IsConsumedFor(user)).IsTrue();
        doodad.DoFuncWithApply(user, func, Apply);
        await Assert.That(applied).IsEqualTo(1);

        // Logged out and back in: the same character with a new ObjId.
        doodad.RemoveVisibleObject(user);
        var relogged = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 71, Name = "First" };
        doodad.AddVisibleObject(relogged);

        await Assert.That(doodad.IsConsumedFor(relogged)).IsTrue();
        doodad.DoFuncWithApply(relogged, func, Apply);
        await Assert.That(applied).IsEqualTo(1);
    }

    /// <summary>
    /// The batched creates sent on entering a region leave out the copy that viewer used up, and
    /// only for that viewer.
    /// </summary>
    [Test]
    public async Task CreatedFor_LeavesOutOnlyTheCopyThatViewerUsedUp()
    {
        var used = new Doodad { Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] } };
        var untouched = new Doodad { Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] } };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "First" };
        var other = new Character(new UnitCustomModelParams()) { Id = 8, ObjId = 80, Name = "Second" };
        var relogged = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 71, Name = "First" };

        used.DoFuncWithApply(user, new DoodadFunc { NextPhase = -1 }, (_, owner) => owner.ToNextPhase = true);

        await Assert.That(Doodad.CreatedFor([used, untouched], user)).IsEquivalentTo(new[] { untouched });
        await Assert.That(Doodad.CreatedFor([used, untouched], relogged)).IsEquivalentTo(new[] { untouched });
        await Assert.That(Doodad.CreatedFor([used, untouched], other)).IsEquivalentTo(new[] { used, untouched });
    }

    /// <summary>
    /// A func that starts a skill of its own leaves the consuming to that skill. A skill that fails
    /// (missing reagent, mana, range) leaves the copy usable for the retry.
    /// </summary>
    [Test]
    public async Task FinalUse_WhoseSkillFails_LeavesTheCopyUsable()
    {
        var doodad = new Doodad
        {
            Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] }
        };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "First" };
        var func = new DoodadFunc { NextPhase = -1 };
        var applied = 0;
        Action<bool> skillOutcome = null;
        void Apply(BaseUnit caster, Doodad owner)
        {
            applied++;
            skillOutcome = owner.DeferFinalUseConsume(caster, func.NextPhase);
            owner.ToNextPhase = true;
        }

        doodad.DoFuncWithApply(user, func, Apply);

        await Assert.That(skillOutcome).IsNotNull();
        await Assert.That(doodad.IsConsumedFor(user)).IsFalse();

        skillOutcome(false);

        await Assert.That(doodad.IsConsumedFor(user)).IsFalse();
        doodad.DoFuncWithApply(user, func, Apply);
        await Assert.That(applied).IsEqualTo(2);
    }

    [Test]
    public async Task FinalUse_WhoseSkillSucceeds_ConsumesTheCopy()
    {
        var doodad = new Doodad
        {
            Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] }
        };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "First" };
        var func = new DoodadFunc { NextPhase = -1 };
        var applied = 0;
        Action<bool> skillOutcome = null;
        void Apply(BaseUnit caster, Doodad owner)
        {
            applied++;
            skillOutcome = owner.DeferFinalUseConsume(caster, func.NextPhase);
            owner.ToNextPhase = true;
        }

        doodad.DoFuncWithApply(user, func, Apply);
        skillOutcome(true);

        await Assert.That(doodad.IsConsumedFor(user)).IsTrue();
        doodad.DoFuncWithApply(user, func, Apply);
        await Assert.That(applied).IsEqualTo(1);
    }

    /// <summary>
    /// Only a player's final use of a client doodad waits for the func's skill; every other use
    /// completes as before.
    /// </summary>
    [Test]
    public async Task DeferFinalUseConsume_OnlyForAPlayersFinalUseOfAClientDoodad()
    {
        var clientDoodad = new Doodad { Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] } };
        var worldDoodad = new Doodad { Template = new DoodadTemplate { ClientDoodad = false, FuncGroups = [] } };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "First" };

        await Assert.That(clientDoodad.DeferFinalUseConsume(user, 41990)).IsNull();
        await Assert.That(clientDoodad.DeferFinalUseConsume(null, -1)).IsNull();
        await Assert.That(worldDoodad.DeferFinalUseConsume(user, -1)).IsNull();
        await Assert.That(clientDoodad.DeferFinalUseConsume(user, -1)).IsNotNull();
    }

    /// <summary>
    /// A use that already moved the player's own view (their quest step changed while the func ran)
    /// leaves the doodad visible to them in the phase they moved to.
    /// </summary>
    [Test]
    public async Task FinalUse_ThatAlreadyMovedTheViewersPhase_DoesNotConsumeTheirCopy()
    {
        var doodad = new Doodad
        {
            Template = new DoodadTemplate { ClientDoodad = true, FuncGroups = [] }
        };
        var user = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 70, Name = "First" };
        var func = new DoodadFunc { NextPhase = -1 };

        doodad.DoFuncWithApply(user, func, (_, owner) =>
        {
            owner.SetQuestReactViewerPhase(user.ObjId, 41872);
            owner.ToNextPhase = true;
        });

        await Assert.That(doodad.IsConsumedFor(user)).IsFalse();
        await Assert.That(doodad.GetPhaseFor(user)).IsEqualTo(41872u);
    }

    [Test]
    [Arguments(true, true, false)]
    [Arguments(true, false, true)]
    [Arguments(false, true, true)]
    public async Task ShouldConsumeOnFinalUse_KeepsClientDoodadForPlayers(
        bool clientDoodad, bool usedByPlayer, bool expected)
    {
        await Assert.That(DoodadQuestReactRules.ShouldConsumeOnFinalUse(clientDoodad, usedByPlayer))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments(true, 41989u, 42002u, true)]
    [Arguments(true, 42002u, 42002u, false)]
    [Arguments(false, 41989u, 42002u, false)]
    public async Task ShouldAdvanceViewerOnly_OnlyForClientDoodadOverlay(
        bool clientDoodad, uint usePhase, uint sharedPhase, bool expected)
    {
        await Assert.That(DoodadQuestReactRules.ShouldAdvanceViewerOnly(clientDoodad, usePhase, sharedPhase))
            .IsEqualTo(expected);
    }
}
