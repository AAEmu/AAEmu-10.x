using AAEmu.Commons.Utils;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Slaves;
using AAEmu.Game.Models.Game.World.Transform;

namespace AAEmu.UnitTests.Game.Models.Game.Slaves;

/// <summary>
/// Guards <see cref="SlaveAttachPointRules"/> and the cart entries of the shipped
/// <c>Data/slave_attach_points.json</c>. The bindings themselves live in the game database, which is not
/// part of the repository, so the tests carry the trade cart rows as a fixture.
/// </summary>
public class SlaveAttachPointRulesTests
{
    private const uint TradePackStorage = 3446;

    /// <summary>
    /// The slaves that carry storage on the cart models, with the attach points their
    /// <c>slave_doodad_bindings</c> rows use: slave id, model id, bound doodad, attach points.
    /// </summary>
    private static readonly (uint SlaveId, uint ModelId, uint DoodadId, int[] AttachPoints)[] CartBindings =
    [
        (60, 1008, TradePackStorage, [9, 10, 11, 12]), // Farm Wagon
        (555, 1714, TradePackStorage, [9, 10]), // Farm Cart
        (575, 1714, 9446, [9]), // siege shield on the Farm Cart model
        (836, 2490, TradePackStorage, [9, 10]), // Farm Cart, dyeable
        (837, 2489, TradePackStorage, [9, 10, 11, 12]), // Farm Wagon, dyeable
    ];

    private static List<SlaveTemplate> CartSlaves() =>
        CartBindings.Select(row => Slave(row.SlaveId, row.ModelId, row.DoodadId, row.AttachPoints)).ToList();

    private static SlaveTemplate Slave(uint slaveId, uint modelId, uint doodadId, params int[] attachPoints)
    {
        var slave = new SlaveTemplate { Id = slaveId, ModelId = modelId };
        foreach (var attachPoint in attachPoints)
        {
            slave.DoodadBindings.Add(new SlaveDoodadBindings
            {
                OwnerId = slaveId,
                OwnerType = "Slave",
                AttachPointId = (AttachPointKind)attachPoint,
                DoodadId = doodadId,
            });
        }

        return slave;
    }

    private static Dictionary<AttachPointKind, WorldSpawnPosition> Points(params int[] attachPoints) =>
        attachPoints.ToDictionary(point => (AttachPointKind)point, _ => new WorldSpawnPosition());

    private static Func<uint, IReadOnlyDictionary<AttachPointKind, WorldSpawnPosition>> Lookup(
        Dictionary<uint, Dictionary<AttachPointKind, WorldSpawnPosition>> table) =>
        modelId => table.GetValueOrDefault(modelId);

    private static List<SlaveModelAttachPoint> LoadShippedTable()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "slave_attach_points.json");
        if (!JsonHelper.TryDeserializeObject(File.ReadAllText(path), out List<SlaveModelAttachPoint> entries, out var error))
            throw new InvalidDataException($"{path} does not parse", error);
        return entries;
    }

    [Test]
    public async Task EveryBindingWithAnOffset_IsPlaced()
    {
        var table = new Dictionary<uint, Dictionary<AttachPointKind, WorldSpawnPosition>>
        {
            [1714] = Points(9, 10),
        };

        var unplaced = SlaveAttachPointRules.FindUnplacedBindings(
            [Slave(555, 1714, TradePackStorage, 9, 10), Slave(575, 1714, 9446, 9)], Lookup(table));

        await Assert.That(unplaced.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TableKeyedToAModelNoSlaveUses_LeavesTheSlaveUnplaced()
    {
        // The Farm Cart offsets were once filed under a model id no slave has, so the cart had none.
        var table = new Dictionary<uint, Dictionary<AttachPointKind, WorldSpawnPosition>>
        {
            [8000009] = Points(9, 10),
        };

        var unplaced = SlaveAttachPointRules.FindUnplacedBindings(
            [Slave(555, 1714, TradePackStorage, 9, 10)], Lookup(table));

        await Assert.That(unplaced).IsEquivalentTo(new[]
        {
            new SlaveAttachPointRules.UnplacedBinding(555, 1714, AttachPointKind.Cannon0),
            new SlaveAttachPointRules.UnplacedBinding(555, 1714, AttachPointKind.Cannon1),
        });
    }

    [Test]
    public async Task TableWithoutTheBoundPoint_ReportsOnlyThatPoint()
    {
        var table = new Dictionary<uint, Dictionary<AttachPointKind, WorldSpawnPosition>>
        {
            [1008] = Points(9, 10, 11),
        };

        var unplaced = SlaveAttachPointRules.FindUnplacedBindings(
            [Slave(60, 1008, TradePackStorage, 9, 10, 11, 12)], Lookup(table));

        await Assert.That(unplaced).IsEquivalentTo(new[]
        {
            new SlaveAttachPointRules.UnplacedBinding(60, 1008, AttachPointKind.Cannon3),
        });
    }

    [Test]
    public async Task SlavesSharingAModel_AreEachChecked()
    {
        var table = new Dictionary<uint, Dictionary<AttachPointKind, WorldSpawnPosition>>
        {
            [1714] = Points(10),
        };

        var unplaced = SlaveAttachPointRules.FindUnplacedBindings(
            [Slave(555, 1714, TradePackStorage, 9, 10), Slave(575, 1714, 9446, 9)], Lookup(table));

        await Assert.That(unplaced).IsEquivalentTo(new[]
        {
            new SlaveAttachPointRules.UnplacedBinding(555, 1714, AttachPointKind.Cannon0),
            new SlaveAttachPointRules.UnplacedBinding(575, 1714, AttachPointKind.Cannon0),
        });
    }

    [Test]
    public async Task SlaveWithoutBindings_NeedsNoTable()
    {
        var asked = new List<uint>();

        var unplaced = SlaveAttachPointRules.FindUnplacedBindings(
            [new SlaveTemplate { Id = 1, ModelId = 42 }],
            modelId =>
            {
                asked.Add(modelId);
                return null;
            });

        await Assert.That(unplaced.Count).IsEqualTo(0);
        await Assert.That(asked.Count).IsEqualTo(0);
        await Assert.That(SlaveAttachPointRules.FindUnplacedBindings(null, _ => null).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ShippedTable_ListsEachModelOnce()
    {
        // The loader keeps the last entry of a model id, so a copied entry that kept its id would silently
        // replace the one it was copied from.
        var repeated = LoadShippedTable()
            .GroupBy(entry => entry.ModelId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        await Assert.That(string.Join(", ", repeated)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ShippedTable_PlacesEveryCartBinding()
    {
        var table = LoadShippedTable().ToDictionary(entry => entry.ModelId, entry => entry.AttachPoints);

        var unplaced = SlaveAttachPointRules.FindUnplacedBindings(CartSlaves(), Lookup(table));

        await Assert.That(string.Join(", ", unplaced)).IsEqualTo(string.Empty);
    }
}
