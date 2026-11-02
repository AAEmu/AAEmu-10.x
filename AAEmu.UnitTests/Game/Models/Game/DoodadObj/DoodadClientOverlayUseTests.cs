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
