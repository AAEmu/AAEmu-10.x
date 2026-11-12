using AAEmu.Commons.Utils.DB;

namespace AAEmu.Game.Models.Game.Indun;

/// <summary>Registered dungeon passes, kept so a paid pass outlives a World restart.</summary>
public interface IIndunPermitStore
{
    /// <summary>Each pass with the time it was spent entering, or null while unspent.</summary>
    IReadOnlyList<(uint ZoneGroupId, DateTime At, DateTime? SpentAt)> Load(uint characterId, DateTime sinceUtc);

    /// <returns>False when a pass already exists at that exact time, so nothing was written.</returns>
    bool Add(uint characterId, uint zoneGroupId, DateTime atUtc);

    /// <summary>Marks the character's oldest unspent pass for that zone group as used.</summary>
    void MarkSpent(uint characterId, uint zoneGroupId, DateTime spentAtUtc);

    /// <param name="zoneGroupId">One zone group, or 0 for all of them.</param>
    void Clear(uint characterId, uint zoneGroupId);
}

/// <summary>character_indun_permits.</summary>
public sealed class MySqlIndunPermitStore : IIndunPermitStore
{
    public IReadOnlyList<(uint ZoneGroupId, DateTime At, DateTime? SpentAt)> Load(uint characterId, DateTime sinceUtc)
    {
        using var connection = MySQL.CreateConnection();
        using (var prune = connection.CreateCommand())
        {
            prune.CommandText = "DELETE FROM `character_indun_permits` WHERE `character_id` = @characterId AND `registered_at` < @since";
            prune.Parameters.AddWithValue("@characterId", characterId);
            prune.Parameters.AddWithValue("@since", sinceUtc);
            prune.ExecuteNonQuery();
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT `zone_group_id`, `registered_at`, `spent_at` FROM `character_indun_permits` WHERE `character_id` = @characterId";
        command.Parameters.AddWithValue("@characterId", characterId);
        using var reader = command.ExecuteReader();
        var rows = new List<(uint, DateTime, DateTime?)>();
        var spentColumn = reader.GetOrdinal("spent_at");
        while (reader.Read())
            rows.Add((
                reader.GetUInt32("zone_group_id"),
                DateTime.SpecifyKind(reader.GetDateTime("registered_at"), DateTimeKind.Utc),
                reader.IsDBNull(spentColumn) ? null : DateTime.SpecifyKind(reader.GetDateTime(spentColumn), DateTimeKind.Utc)));
        return rows;
    }

    public bool Add(uint characterId, uint zoneGroupId, DateTime atUtc)
    {
        using var connection = MySQL.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT IGNORE INTO `character_indun_permits` (`character_id`, `zone_group_id`, `registered_at`) " +
            "VALUES (@characterId, @zoneGroupId, @at)";
        command.Parameters.AddWithValue("@characterId", characterId);
        command.Parameters.AddWithValue("@zoneGroupId", zoneGroupId);
        command.Parameters.AddWithValue("@at", atUtc);
        return command.ExecuteNonQuery() > 0;
    }

    public void MarkSpent(uint characterId, uint zoneGroupId, DateTime spentAtUtc)
    {
        using var connection = MySQL.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE `character_indun_permits` SET `spent_at` = @at " +
            "WHERE `character_id` = @characterId AND `zone_group_id` = @zoneGroupId AND `spent_at` IS NULL " +
            "ORDER BY `registered_at` LIMIT 1";
        command.Parameters.AddWithValue("@characterId", characterId);
        command.Parameters.AddWithValue("@zoneGroupId", zoneGroupId);
        command.Parameters.AddWithValue("@at", spentAtUtc);
        command.ExecuteNonQuery();
    }

    public void Clear(uint characterId, uint zoneGroupId)
    {
        using var connection = MySQL.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = zoneGroupId == 0
            ? "DELETE FROM `character_indun_permits` WHERE `character_id` = @characterId"
            : "DELETE FROM `character_indun_permits` WHERE `character_id` = @characterId AND `zone_group_id` = @zoneGroupId";
        command.Parameters.AddWithValue("@characterId", characterId);
        command.Parameters.AddWithValue("@zoneGroupId", zoneGroupId);
        command.ExecuteNonQuery();
    }
}
