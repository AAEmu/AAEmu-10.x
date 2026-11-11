using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Static;
using AAEmu.UnitTests.Utils;

using Microsoft.Data.Sqlite;

namespace AAEmu.UnitTests.Game.GameData;

[NotInParallel]
public class MateGameDataTests
{
    [Test]
    public async Task Load_ResolvesNpcCatalogKeysAndRejectsMissing()
    {
        using var connection = CreateConnection(includeRecoveryColumns: true);
        var data = new MateGameData();

        data.Load(connection);

        using var relation = connection.CreateCommand();
        relation.CommandText = "SELECT npc_id FROM item_summon_mates WHERE item_id = $item";
        relation.Parameters.AddWithValue("$item", 501u);
        var npcId = Convert.ToUInt32(relation.ExecuteScalar());
        var summonTemplate = new SummonMateTemplate { Id = 501, NpcId = npcId };
        var state = data.GetRecoveryState(summonTemplate);
        await Assert.That(state.MateReviveDelay).IsEqualTo(7);
        await Assert.That(state.MateReviveHpPercent).IsEqualTo(11);
        await Assert.That(state.MateReviveMpPercent).IsEqualTo(13);
        var unrelatedNpcState = data.GetRecoveryState(202);
        await Assert.That(unrelatedNpcState.MateReviveDelay).IsEqualTo(17);
        await Assert.That(unrelatedNpcState.MateReviveHpPercent).IsEqualTo(19);
        await Assert.That(unrelatedNpcState.MateReviveMpPercent).IsEqualTo(23);
        Assert.Throws<KeyNotFoundException>(() => data.GetRecoveryState(303));
    }

    [Test]
    public async Task ResolveSummonMate_UsesRealItemRelationAndNpcTemplate()
    {
        using var connection = CreateConnection(includeRecoveryColumns: true);
        var data = new MateGameData();
        data.Load(connection);

        var itemManager = new ItemManager(null, null, null, null, null, null);
        foreach (var template in ItemManager.LoadSummonMateTemplates(connection))
            itemManager.SetTemplateForTest(template);

        var npcManager = new NpcManager(null, null, null, null, null);
        npcManager.GetAllTemplates()[101] = new NpcTemplate
        {
            Id = 101,
            Name = "synthetic-mate-npc",
            Level = 9
        };

        using var itemScope = new SingletonScope<ItemManager>(itemManager);
        using var npcScope = new SingletonScope<NpcManager>(npcManager);
        using var dataScope = new SingletonScope<MateGameData>(data);
        var owner = new Character(new UnitCustomModelParams()) { Id = 77 };
        var resolved = new CharacterMates(owner).ResolveSummonMate(501);

        await Assert.That(resolved.ItemTemplate.NpcId).IsEqualTo(101u);
        await Assert.That(resolved.NpcTemplate.Id).IsEqualTo(101u);
        await Assert.That(resolved.RecoveryState.MateReviveDelay).IsEqualTo(7);
        await Assert.That(resolved.RecoveryState.MateReviveHpPercent).IsEqualTo(11);
        await Assert.That(resolved.RecoveryState.MateReviveMpPercent).IsEqualTo(13);
    }

    [Test]
    [Arguments(68u, 0u, 15626u)]  // Blueglass: row 69, 1300 m
    [Arguments(66u, 0u, 15623u)]  // Grenwolf: row 74, 1080 m
    [Arguments(71u, 0u, 15757u)]  // Golden Fable: row 72, 1000 m
    [Arguments(99u, 0u, 20075u)]  // Amed's cannon: row 179, Loka
    [Arguments(66u, 179u, 15623u)] // Grenwolf asking for the Loka row keeps its own
    public async Task GetMountAttachedSkills_EachHumanCannonLaunchesWithItsOwnJump(uint slaveId, uint clientRow,
        uint expectedRiderSkill)
    {
        using var connection = CreateConnection(includeRecoveryColumns: true);
        using (var seed = connection.CreateCommand())
        {
            // the shipped cannon rows: four mount_skills share skill 16650, mount_attached_skills gives each
            // its own Driver (1) helper, slave_mount_skills says which cannon carries which
            seed.CommandText = """
                INSERT INTO mount_skills(id, name, skill_id) VALUES
                    (69, '1300m', 16650), (72, 'golden fable', 16650), (74, '1080m', 16650), (81, 'exit', 16813),
                    (179, 'loka', 16650);
                INSERT INTO mount_attached_skills(id, mount_skill_id, attach_point_id, skill_id) VALUES
                    (234, 69, 1, 15626), (235, 72, 1, 15757), (236, 74, 1, 15623), (241, 81, 1, 16826),
                    (289, 179, 1, 20075);
                INSERT INTO slave_mount_skills(id, slave_id, mount_skill_id) VALUES
                    (189, 68, 69), (190, 68, 81), (193, 66, 74), (194, 66, 81), (187, 71, 72), (188, 71, 81),
                    (172, 99, 179), (173, 99, 81);
                """;
            seed.ExecuteNonQuery();
        }

        var data = new MateGameData();
        data.Load(connection);
        var cannon = new Slave { TemplateId = slaveId };

        await Assert.That(data.GetMountAttachedSkills(16650, AttachPointKind.Driver, cannon, clientRow))
            .IsEqualTo(expectedRiderSkill);
        await Assert.That(data.GetMountAttachedSkills(16813, AttachPointKind.Driver, cannon, 81)).IsEqualTo(16826u);
    }

    [Test]
    public async Task Load_MissingRecoveryColumns_FailsLoudly()
    {
        using var connection = CreateConnection(includeRecoveryColumns: false);
        var data = new MateGameData();

        Assert.Throws<SqliteException>(() => data.Load(connection));
        await Assert.That(data).IsNotNull();
    }

    private static SqliteConnection CreateConnection(bool includeRecoveryColumns)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = includeRecoveryColumns
            ? """
              CREATE TABLE npc_mount_skills (id INTEGER PRIMARY KEY, npc_id INTEGER NOT NULL, mount_skill_id INTEGER NOT NULL);
              CREATE TABLE mount_skills (id INTEGER PRIMARY KEY, name TEXT NOT NULL, skill_id INTEGER NOT NULL);
              CREATE TABLE mount_attached_skills (id INTEGER PRIMARY KEY, mount_skill_id INTEGER NOT NULL, attach_point_id INTEGER NOT NULL, skill_id INTEGER NOT NULL);
              CREATE TABLE slave_mount_skills (id INTEGER PRIMARY KEY, slave_id INTEGER NOT NULL, mount_skill_id INTEGER NOT NULL);
              CREATE TABLE mate_equip_slot_packs (id INTEGER PRIMARY KEY, mate_type_id INTEGER NOT NULL, head INTEGER NOT NULL, chest INTEGER NOT NULL, waist INTEGER NOT NULL, feet INTEGER NOT NULL);
              CREATE TABLE mate_equip_pack_groups (id INTEGER PRIMARY KEY, npc_id INTEGER NOT NULL, mate_equip_pack_id INTEGER NOT NULL);
              CREATE TABLE mate_equip_pack_items (id INTEGER PRIMARY KEY, mate_equip_pack_id INTEGER NOT NULL, item_id INTEGER NOT NULL);
              CREATE TABLE item_summon_mates (id INTEGER PRIMARY KEY, item_id INTEGER NOT NULL, npc_id INTEGER NOT NULL);
              CREATE TABLE npcs (
                  id INTEGER PRIMARY KEY,
                  model_id INTEGER NOT NULL DEFAULT 0,
                  mate_revive_delay INTEGER NOT NULL,
                  mate_revive_hp_percent INTEGER NOT NULL,
                  mate_revive_mp_percent INTEGER NOT NULL);
              CREATE TABLE model_bindings (owner_id INTEGER NOT NULL, owner_type TEXT NOT NULL, attach_point_id INTEGER NOT NULL);
              INSERT INTO npcs(id, mate_revive_delay, mate_revive_hp_percent, mate_revive_mp_percent)
              VALUES (101, 7, 11, 13), (202, 17, 19, 23);
              INSERT INTO item_summon_mates(id, item_id, npc_id) VALUES (1, 501, 101), (2, 502, 303);
              """
            : """
              CREATE TABLE npc_mount_skills (id INTEGER PRIMARY KEY, npc_id INTEGER NOT NULL, mount_skill_id INTEGER NOT NULL);
              CREATE TABLE mount_skills (id INTEGER PRIMARY KEY, name TEXT NOT NULL, skill_id INTEGER NOT NULL);
              CREATE TABLE mount_attached_skills (id INTEGER PRIMARY KEY, mount_skill_id INTEGER NOT NULL, attach_point_id INTEGER NOT NULL, skill_id INTEGER NOT NULL);
              CREATE TABLE slave_mount_skills (id INTEGER PRIMARY KEY, slave_id INTEGER NOT NULL, mount_skill_id INTEGER NOT NULL);
              CREATE TABLE mate_equip_slot_packs (id INTEGER PRIMARY KEY, mate_type_id INTEGER NOT NULL, head INTEGER NOT NULL, chest INTEGER NOT NULL, waist INTEGER NOT NULL, feet INTEGER NOT NULL);
              CREATE TABLE mate_equip_pack_groups (id INTEGER PRIMARY KEY, npc_id INTEGER NOT NULL, mate_equip_pack_id INTEGER NOT NULL);
              CREATE TABLE mate_equip_pack_items (id INTEGER PRIMARY KEY, mate_equip_pack_id INTEGER NOT NULL, item_id INTEGER NOT NULL);
              CREATE TABLE item_summon_mates (id INTEGER PRIMARY KEY, item_id INTEGER NOT NULL, npc_id INTEGER NOT NULL);
              CREATE TABLE npcs (id INTEGER PRIMARY KEY, model_id INTEGER NOT NULL DEFAULT 0);
              CREATE TABLE model_bindings (owner_id INTEGER NOT NULL, owner_type TEXT NOT NULL, attach_point_id INTEGER NOT NULL);
              """;
        command.ExecuteNonQuery();
        return connection;
    }
}
