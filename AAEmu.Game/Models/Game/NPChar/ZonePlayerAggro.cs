using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Tasks.Npcs;
using NLog;

namespace AAEmu.Game.Models.Game.NPChar;

/// <summary>
/// Seeds Zone AI aggro NPC→Character under ZoneAuthority (same handoff shape as sport-fish).
/// </summary>
public static class ZonePlayerAggro
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>Delay after Create / ClearCombat so dedicate CreateAI / clear settle first.</summary>
    public static readonly TimeSpan SeedDelay = TimeSpan.FromMilliseconds(750);

    public static void ScheduleSeedNpcTowardNearbyPlayers(uint npcObjId)
    {
        if (!WorldIntegration.ZoneAuthority || npcObjId == 0)
            return;

        TaskManager.Instance.Schedule(new ZonePlayerAggroSeedTask(npcObjId), SeedDelay);
    }

    public static void ScheduleSeedNearbyNpcsTowardPlayer(uint characterObjId)
    {
        if (!WorldIntegration.ZoneAuthority || characterObjId == 0)
            return;

        TaskManager.Instance.Schedule(new ZonePlayerAggroPlayerSeedTask(characterObjId), SeedDelay);
    }

    public static int TrySeedNpcTowardNearbyPlayers(Npc npc)
    {
        if (npc == null || !WorldIntegration.ZoneAuthority)
            return 0;

        if (npc.Template?.Aggression != true || npc.Hp <= 0 || npc.IsDead)
            return 0;

        var sightSq = ZonePlayerAggroRules.SightMetresSquared(npc.Template.SightRangeScale);
        var sight = ZonePlayerAggroRules.SightMetres(npc.Template.SightRangeScale);
        var players = WorldManager.GetAround<Character>(npc, sight);
        if (players is not { Count: > 0 })
            return 0;

        var seeded = 0;
        foreach (var player in players)
        {
            if (TrySeedPair(npc, player, sightSq))
                seeded++;
        }

        return seeded;
    }

    public static int TrySeedNearbyNpcsTowardPlayer(Character character)
    {
        if (character == null || !character.IsOnline || !WorldIntegration.ZoneAuthority)
            return 0;

        // Broad scan; per-NPC sight band still gates inside TrySeedPair.
        var npcs = WorldManager.GetAround<Npc>(character, ZonePlayerAggroRules.BaseSightMetres * 4f);
        if (npcs is not { Count: > 0 })
            return 0;

        var seeded = 0;
        foreach (var npc in npcs)
        {
            if (npc.Template?.Aggression != true)
                continue;

            var sightSq = ZonePlayerAggroRules.SightMetresSquared(npc.Template.SightRangeScale);
            if (TrySeedPair(npc, character, sightSq))
                seeded++;
        }

        return seeded;
    }

    private static bool TrySeedPair(Npc npc, Character player, float sightMetresSquared)
    {
        if (npc == null || player == null || !player.IsOnline)
            return false;

        if (npc.ParentWorld != null && player.ParentWorld != null &&
            !ReferenceEquals(npc.ParentWorld, player.ParentWorld))
            return false;

        var dx = npc.Transform.World.Position.X - player.Transform.World.Position.X;
        var dy = npc.Transform.World.Position.Y - player.Transform.World.Position.Y;
        var dz = npc.Transform.World.Position.Z - player.Transform.World.Position.Z;
        var distanceSq = dx * dx + dy * dy + dz * dz;

        if (!ZonePlayerAggroRules.ShouldSeed(
                WorldIntegration.ZoneAuthority,
                npc.Template?.Aggression == true,
                npc.CanAttack(player),
                npc.Hp > 0 && !npc.IsDead,
                player.IsOnline,
                distanceSq,
                sightMetresSquared,
                npc.CurrentTarget,
                player.ObjId,
                npc.ObjId))
            return false;

        // Already holding this player — still refresh the zone table if battle dropped.
        npc.CurrentTarget = player;
        WorldIntegration.RelayTargetChangedToZone?.Invoke(npc.ObjId, player.ObjId, true);
        WorldIntegration.PublishAggro(npc, player, 1, new CastSkill(0, 0));
        WorldIntegration.RelayCombatEngagedToZone?.Invoke(npc.ObjId);
        npc.Events.OnCombatStarted(npc, new OnCombatStartedArgs { Owner = npc, Target = player });

        Logger.Info(
            "ZonePlayerAggro seed npc={0} tpl={1} → player={2} ({3}) distSq={4:F0}",
            npc.ObjId, npc.TemplateId, player.ObjId, player.Name, distanceSq);
        return true;
    }
}
