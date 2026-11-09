using AAEmu.Game.Models.Game.DoodadObj.Funcs;
using AAEmu.Game.Models.Game.DoodadObj.Static;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncAttachmentTests
{
    private const int SeatAnim = 7;

    [Test]
    [Arguments(BondKind.BondInvalid, SeatAnim, false, false)] // bed, lounger, Hero throne
    [Arguments(BondKind.BondInvalid, SeatAnim, true, true)]   // jetski / speedboat driver
    [Arguments(BondKind.BondInvalid, 0, false, true)]         // classic ship seat
    [Arguments(BondKind.None, 0, false, true)]                // ship seat without a bond row
    [Arguments(BondKind.BondChairSingle, 0, false, false)]    // bonded chair
    [Arguments(BondKind.BondBedSingle, SeatAnim, true, false)] // bonded seat on a slave stays furniture
    public async Task ShouldBindSlaveSeat_PicksFurnitureOrSlave(
        BondKind bondKind, int animActionId, bool hasVehicleSeat, bool expected)
    {
        await Assert.That(DoodadFuncAttachment.ShouldBindSlaveSeat(bondKind, animActionId, hasVehicleSeat))
            .IsEqualTo(expected);
    }
}
