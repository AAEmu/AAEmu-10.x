using System.Numerics;
using AAEmu.Commons.Utils;
using AAEmu.Commons.Utils.DB;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;
using NLog;

namespace AAEmu.Game.Core.Managers;

public class SusManager(IWorldManager worldManager) : Singleton<SusManager>, ISusManager
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    public static string CategoryBot => "Bot";
    public static string CategoryCheating => "Cheat";
    public static string CategoryRmt => "RMT";

    /// <summary>
    /// Highest flat speed in m/s a player on foot can move between two checks without being flagged
    /// </summary>
    public const double PlayerCheckSpeed = 25.0;
    /// <summary>
    /// Highest flat speed in m/s a mount can move between two checks without being flagged
    /// </summary>
    public const double MountCheckSpeed = 27.5;

    // Analyzed from players' packet threads and from the bots' tick thread, the trackers do their own locking
    internal SusMovementTracker LastPlayerPositions { get; } = new();
    internal SusMovementTracker LastPetPositions { get; } = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public bool LogActivity(string category, uint accountId, uint playerId, uint zoneGroup, Vector3 position, string description)
    {
        try
        {
            using var connection = MySQL.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO audit_char_sus (sus_category, sus_account, sus_character, zone_group, x, y, z, description) " +
                "VALUES (@sus_category, @sus_account, @sus_character, @zone_group, @x, @y, @z, @description)";
            command.Parameters.AddWithValue("@sus_category", category);
            command.Parameters.AddWithValue("@sus_account", accountId);
            command.Parameters.AddWithValue("@sus_character", playerId);
            command.Parameters.AddWithValue("@zone_group", zoneGroup);
            command.Parameters.AddWithValue("@x", position.X);
            command.Parameters.AddWithValue("@y", position.Y);
            command.Parameters.AddWithValue("@z", position.Z);
            command.Parameters.AddWithValue("@description", description);
            command.Prepare();
            if (command.ExecuteNonQuery() <= 0)
            {
                Logger.Error($"Saving suspisious activity failed: {category} - {description}");
                return false;
            }
            Logger.Warn(description);
        }
        catch (Exception ex)
        {
            Logger.Error($"Saving suspisious activity failed: {category} - {description} - {ex}");
            return false;
        }
        return true;
    }

    // Virtual so a test can see what the movement checks report without writing to the audit table
    public virtual bool LogActivity(string category, Character player, string description)
    {
        return LogActivity(category, player?.AccountId ?? 0, player?.Id ?? 0, player?.Transform?.ZoneId ?? 0, player?.Transform?.World?.Position ?? Vector3.Zero, description);
    }
    
    public bool LogActivity(string category, string description)
    {
        return LogActivity(category, 0, 0, 0, Vector3.Zero, description);
    }

    #region movement_checks
    /// <summary>
    /// Do some analysis on player movement
    /// </summary>
    /// <param name="player"></param>
    /// <param name="deltaTime"></param>
    public void AnalyzePlayerDeltaMovement(Character player, float deltaTime)
    {
        // var playerCheckSpeed = 5.0 * character.BaseMoveSpeed * character.MoveSpeedMul * 3.0;
        var playerCheckSpeed = PlayerCheckSpeed;
        var observedSpeed = LastPlayerPositions.Analyze(player.Id,
            player.Transform.World.ClonePosition(),
            player.IsRiding == false && player.DisabledSetPosition == false,
            deltaTime,
            playerCheckSpeed);
        if (observedSpeed is not { } speed)
            return;

        // Looks like this player is moving a bit fast, better make a note of it
        LogActivity(CategoryCheating,
            player,
            $"Player {player.Name} seems to be moving a bit fast {speed:F1} m/s (max {playerCheckSpeed:F1})");
        // player.SendMessage($"Speed {speed:F1} m/s (max {playerCheckSpeed:F1}) !!!");
    }

    /// <summary>
    /// Resets current analysis of movement, call after things like teleport to prevent false positives
    /// </summary>
    /// <param name="playerId"></param>
    public void ResetAnalyzePlayerDeltaMovement(uint playerId)
    {
        LastPlayerPositions.Reset(playerId);
    }

    /// <summary>
    /// The server put the player back at the spot it stands on now after its client claimed another (a teleport
    /// confirmation far from the landing it was sent to). The check starts again from this spot, so the client's next
    /// move from where it claimed to be is judged as movement from here.
    /// </summary>
    /// <param name="player"></param>
    public void RestartAnalyzePlayerDeltaMovement(Character player)
    {
        LastPlayerPositions.Restart(player.Id, player.Transform.World.ClonePosition());
    }

    /// <summary>
    /// The server sent the player's client to <paramref name="landing"/> and the client makes the jump itself (an
    /// escape, a closed zone, a battlefield respawn, a GM goto). The check that falls on its arrival there only
    /// remembers the spot, while any other spot is still judged as movement. A move the server writes itself under
    /// the position lock is forgotten with a reset instead.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="landing">World position the client was sent to</param>
    public void ExpectPlayerLanding(uint playerId, Vector3 landing)
    {
        LastPlayerPositions.ExpectLanding(playerId, landing);
    }

    /// <summary>
    /// Do some analysis on mount movement
    /// </summary>
    /// <param name="pet"></param>
    /// <param name="deltaTime"></param>
    public void AnalyzeMountDeltaMovement(Mate pet, float deltaTime)
    {
        // var playerCheckSpeed = 5.0 * character.BaseMoveSpeed * character.MoveSpeedMul * 3.0;
        var playerCheckSpeed = MountCheckSpeed;
        var observedSpeed = LastPetPositions.Analyze(pet.Id,
            pet.Transform.World.ClonePosition(),
            pet.DisabledSetPosition == false,
            deltaTime,
            playerCheckSpeed);
        if (observedSpeed is not { } speed)
            return;

        // Looks like this player is moving a bit fast, better make a note of it
        var petOwner = worldManager.GetCharacterByObjId(pet.OwnerObjId);
        LogActivity(CategoryCheating,
            petOwner,
            $"Pet {pet.Name} from {petOwner?.Name} seems to be moving a bit fast {speed:F1} m/s (max {playerCheckSpeed:F1})");
        // petOwner?.SendMessage($"Pet Speed {speed:F1} m/s (max {playerCheckSpeed:F1}) !!!");
    }

    /// <summary>
    /// Resets current analysis of movement, call after things like teleport to prevent false positives
    /// </summary>
    /// <param name="petId"></param>
    public void ResetAnalyzeMountDeltaMovement(uint petId)
    {
        LastPetPositions.Reset(petId);
    }
    #endregion
}
