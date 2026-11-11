using AAEmu.Game.Models.Game.Slaves;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.World.Transform;

namespace AAEmu.UnitTests.Game.Models.Game.Slaves;

/// <summary>
/// Test-side check of the doodads slaves carry (<c>slave_doodad_bindings</c>) against the attach point offsets of the
/// slaves' models. A bound doodad is placed at the offset its attach point has on the slave's model; without
/// one it stays at the hull origin, which is how a cart's trade pack storage ends up under the cart.
/// </summary>
public static class SlaveAttachPointRules
{
    /// <summary>A doodad binding whose attach point has no offset on its slave's model.</summary>
    public readonly record struct UnplacedBinding(uint SlaveId, uint ModelId, AttachPointKind AttachPoint);

    /// <summary>
    /// The doodad bindings of <paramref name="slaves"/> that cannot be placed: the slave's model has no attach
    /// point table at all, or its table lacks the binding's attach point. Empty when every binding has one.
    /// </summary>
    /// <param name="slaves">The slave templates, each with its model and doodad bindings.</param>
    /// <param name="attachPointsForModel">The attach point table of a model id, or null when it has none.</param>
    public static List<UnplacedBinding> FindUnplacedBindings(
        IEnumerable<SlaveTemplate> slaves,
        Func<uint, IReadOnlyDictionary<AttachPointKind, WorldSpawnPosition>> attachPointsForModel)
    {
        var unplaced = new List<UnplacedBinding>();
        var tables = new Dictionary<uint, IReadOnlyDictionary<AttachPointKind, WorldSpawnPosition>>();

        foreach (var slave in slaves ?? [])
        {
            if (slave == null || slave.DoodadBindings.Count == 0)
                continue;

            if (!tables.TryGetValue(slave.ModelId, out var attachPoints))
            {
                attachPoints = attachPointsForModel(slave.ModelId);
                tables[slave.ModelId] = attachPoints;
            }

            foreach (var binding in slave.DoodadBindings)
            {
                if (attachPoints == null || !attachPoints.ContainsKey(binding.AttachPointId))
                    unplaced.Add(new UnplacedBinding(slave.Id, slave.ModelId, binding.AttachPointId));
            }
        }

        return unplaced;
    }
}
