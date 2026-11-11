using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.DoodadObj;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

/// <summary>
/// The doodad's wire "family" field (after plantTime) gates FAMILY-permission funcs (perm 10) in the
/// client, e.g. the Ironwood family tree 11541's roles, Start and harvest. It carries the owner's family.
/// </summary>
public sealed class DoodadWireFamilyTests
{
    private const uint OwnerId = 1006;

    [Test]
    public async Task PlayerOwnedDoodadCarriesTheOwnersFamily()
    {
        var families = Mock.Of<IFamilyManager>();
        families.GetFamilyOfCharacter(OwnerId).Returns(42u);
        var doodad = new Doodad { OwnerType = DoodadOwnerType.Character, OwnerId = OwnerId };

        await Assert.That(doodad.WireFamilyId(families.Object)).IsEqualTo(42u);
    }

    [Test]
    public async Task OwnerWithoutFamilySendsZero()
    {
        var families = Mock.Of<IFamilyManager>();
        families.GetFamilyOfCharacter(OwnerId).Returns(0u);
        var doodad = new Doodad { OwnerType = DoodadOwnerType.Housing, OwnerId = OwnerId };

        await Assert.That(doodad.WireFamilyId(families.Object)).IsEqualTo(0u);
    }

    [Test]
    public async Task SystemDoodadNeverAsksForAFamily()
    {
        var families = Mock.Of<IFamilyManager>();
        families.GetFamilyOfCharacter(OwnerId).Returns(42u);
        var doodad = new Doodad { OwnerType = DoodadOwnerType.System, OwnerId = OwnerId };

        await Assert.That(doodad.WireFamilyId(families.Object)).IsEqualTo(0u);
    }
}
