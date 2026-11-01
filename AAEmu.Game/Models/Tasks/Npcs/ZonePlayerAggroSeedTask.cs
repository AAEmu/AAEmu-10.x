using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;

namespace AAEmu.Game.Models.Tasks.Npcs;

/// <summary>Deferred NPC→player aggro seed after Zone CreateAI / ClearCombat settle.</summary>
public sealed class ZonePlayerAggroSeedTask(uint npcObjId) : Task
{
    public override void Execute()
    {
        if (WorldIntegration.FindUnitAcrossWorlds(npcObjId) is not Npc npc)
            return;

        ZonePlayerAggro.TrySeedNpcTowardNearbyPlayers(npc);
    }
}

/// <summary>Deferred player enter resync — aggressive NPCs already in sight.</summary>
public sealed class ZonePlayerAggroPlayerSeedTask(uint characterObjId) : Task
{
    public override void Execute()
    {
        if (WorldIntegration.FindUnitAcrossWorlds(characterObjId) is not Character character)
            return;

        ZonePlayerAggro.TrySeedNearbyNpcsTowardPlayer(character);
    }
}
