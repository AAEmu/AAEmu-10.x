using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadKeptChoiceTests
{
    private sealed class KeepingDoodad : Doodad
    {
        internal override bool KeepsAsChoice(DoodadFunc func) => true;
    }

    [Test]
    public async Task AKeptChoice_DoesNotAskForAPhaseChange()
    {
        // A kept choice used to leave ToNextPhase set, so Use re-ran the unchanged phase and restarted its timer.
        var doodad = new KeepingDoodad { Template = new DoodadTemplate { FuncGroups = [] } };
        var character = new Character(new UnitCustomModelParams()) { Id = 7, Name = "Tester" };
        var choice = new DoodadFunc { FuncType = nameof(DoodadFuncUse), NextPhase = -1 };

        doodad.DoFuncWithApply(character, choice, (_, owner) => owner.ToNextPhase = true);

        await Assert.That(doodad.ToNextPhase).IsFalse();
    }
}
