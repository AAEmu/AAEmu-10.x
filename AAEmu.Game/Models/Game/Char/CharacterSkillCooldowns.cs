using AAEmu.Commons.Utils.DB;
using MySql.Data.MySqlClient;
using NLog;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>character_skill_cooldowns: a character's running skill cooldowns across logouts.</summary>
public static class CharacterSkillCooldowns
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private static readonly (string Table, string Column, bool IsTag)[] Tables =
    [
        ("character_skill_cooldowns", "skill_id", false),
        ("character_skill_tag_cooldowns", "tag_id", true),
    ];

    /// <summary>Restores the saved timers. False when they could not be read; the caller must not let the character in.</summary>
    public static bool Load(Character character)
    {
        try
        {
            using var connection = MySQL.CreateConnection();
            var now = DateTime.UtcNow;

            foreach (var (table, column, isTag) in Tables)
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"SELECT `{column}`, `duration`, `ends_at` FROM `{table}` WHERE `character_id` = @characterId";
                command.Parameters.AddWithValue("@characterId", character.Id);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetUInt32(column);
                    var duration = reader.GetUInt32("duration");
                    var endsAt = DateTime.SpecifyKind(reader.GetDateTime("ends_at"), DateTimeKind.Utc);
                    var remaining = SkillCooldownPersistRules.RemainingMs(endsAt, now);
                    if (isTag)
                        character.Cooldowns.RestoreTag(id, duration, remaining);
                    else
                        character.Cooldowns.Restore(id, duration, remaining);
                }
            }

            character.SkillCooldownsLoaded = true;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to load skill cooldowns for character {character.Id}");
            return false;
        }
    }

    /// <summary>Replaces the saved rows; only for a character whose cooldowns were loaded, or they would be lost.</summary>
    public static void Save(MySqlConnection connection, MySqlTransaction transaction, Character character)
    {
        if (!character.SkillCooldownsLoaded)
            return;

        var now = DateTime.UtcNow;
        foreach (var (table, column, isTag) in Tables)
        {
            using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = $"DELETE FROM `{table}` WHERE `character_id` = @characterId";
                delete.Parameters.AddWithValue("@characterId", character.Id);
                delete.ExecuteNonQuery();
            }

            var snapshots = isTag
                ? character.Cooldowns.GetActiveTagSnapshots(int.MaxValue)
                : character.Cooldowns.GetActiveSnapshots(int.MaxValue);
            foreach (var snapshot in snapshots)
            {
                if (!SkillCooldownPersistRules.ShouldSave(snapshot))
                    continue;

                using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText =
                    $"INSERT INTO `{table}` (`character_id`, `{column}`, `duration`, `ends_at`) " +
                    "VALUES (@characterId, @id, @duration, @endsAt)";
                insert.Parameters.AddWithValue("@characterId", character.Id);
                insert.Parameters.AddWithValue("@id", snapshot.SkillId);
                insert.Parameters.AddWithValue("@duration", snapshot.Duration);
                insert.Parameters.AddWithValue("@endsAt", now.AddMilliseconds(snapshot.Remaining));
                insert.ExecuteNonQuery();
            }
        }
    }
}
