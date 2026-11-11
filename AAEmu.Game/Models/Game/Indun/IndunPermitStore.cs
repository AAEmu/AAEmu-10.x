using AAEmu.Commons.Utils.DB;

namespace AAEmu.Game.Models.Game.Indun;

/// <summary>Registered dungeon passes, kept so a paid pass outlives a World restart.</summary>
public interface IIndunPermitStore
{
    IReadOnlyList<(uint ZoneGroupId, DateTime At)> Load(uint characterId, DateTime sinceUtc);
    void Add(uint characterId, uint zoneGroupId, DateTime atUtc);

    /// <param name="zoneGroupId">One zone group, or 0 for all of them.</param>
    void Clear(uint characterId, uint zoneGroupId);
}

/// <summary>character_indun_permits.</summary>
public sealed class MySqlIndunPermitStore : IIndunPermitStore
{
    public IReadOnlyList<(uint ZoneGroupId, DateTime At)> Load(uint characterId, DateTime sinceUtc)
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
        command.CommandText = "SELECT `zone_group_id`, `registered_at` FROM `character_indun_permits` WHERE `character_id` = @characterId";
        command.Parameters.AddWithValue("@characterId", characterId);
        using var reader = command.ExecuteReader();
        var rows = new List<(uint, DateTime)>();
        while (reader.Read())
            rows.Add((reader.GetUInt32("zone_group_id"), DateTime.SpecifyKind(reader.GetDateTime("registered_at"), DateTimeKind.Utc)));
        return rows;
    }

    public void Add(uint characterId, uint zoneGroupId, DateTime atUtc)
    {
        using var connection = MySQL.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT IGNORE INTO `character_indun_permits` (`character_id`, `zone_group_id`, `registered_at`) " +
            "VALUES (@characterId, @zoneGroupId, @at)";
        command.Parameters.AddWithValue("@characterId", characterId);
        command.Parameters.AddWithValue("@zoneGroupId", zoneGroupId);
        command.Parameters.AddWithValue("@at", atUtc);
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
