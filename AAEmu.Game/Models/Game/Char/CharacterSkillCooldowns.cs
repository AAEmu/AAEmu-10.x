using AAEmu.Commons.Utils.DB;
using AAEmu.Game.Core.Managers;
using MySql.Data.MySqlClient;
using NLog;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>character_skill_cooldowns: a character's running skill cooldowns across logouts.</summary>
public static class CharacterSkillCooldowns
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public static void Load(Character character)
    {
        try
        {
            using var connection = MySQL.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT `skill_id`, `duration`, `ends_at` FROM `character_skill_cooldowns` WHERE `character_id` = @characterId";
            command.Parameters.AddWithValue("@characterId", character.Id);

            var now = DateTime.UtcNow;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var skillId = reader.GetUInt32("skill_id");
                var endsAt = DateTime.SpecifyKind(reader.GetDateTime("ends_at"), DateTimeKind.Utc);
                character.Cooldowns.Restore(skillId, reader.GetUInt32("duration"),
                    SkillCooldownPersistRules.RemainingMs(endsAt, now),
                    SkillManager.Instance.GetSkillTemplate(skillId)?.CooldownTags);
            }

            character.SkillCooldownsLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to load skill cooldowns for character {character.Id}");
        }
    }

    /// <summary>Replaces the saved rows; only for a character whose cooldowns were loaded, or they would be lost.</summary>
    public static void Save(MySqlConnection connection, MySqlTransaction transaction, Character character)
    {
        if (!character.SkillCooldownsLoaded)
            return;

        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM `character_skill_cooldowns` WHERE `character_id` = @characterId";
            delete.Parameters.AddWithValue("@characterId", character.Id);
            delete.ExecuteNonQuery();
        }

        var now = DateTime.UtcNow;
        foreach (var snapshot in character.Cooldowns.GetActiveSnapshots(int.MaxValue))
        {
            if (!SkillCooldownPersistRules.ShouldSave(snapshot))
                continue;

            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                "INSERT INTO `character_skill_cooldowns` (`character_id`, `skill_id`, `duration`, `ends_at`) " +
                "VALUES (@characterId, @skillId, @duration, @endsAt)";
            insert.Parameters.AddWithValue("@characterId", character.Id);
            insert.Parameters.AddWithValue("@skillId", snapshot.SkillId);
            insert.Parameters.AddWithValue("@duration", snapshot.Duration);
            insert.Parameters.AddWithValue("@endsAt", now.AddMilliseconds(snapshot.Remaining));
            insert.ExecuteNonQuery();
        }
    }
}
