# Island of Abundance

Vocation dungeon for up to 4 players (instance catalog 80, zone group 158, zone 410, world
`instance_life_dungeon_daru`). Players queue with an Island of Abundance Pass, harvest crops as Daru for points,
then defend Greenman Songsong. Rewards are a chest (Songsong survives) and a ranked mail (always).

The island exercises the dungeon entry, zone host, scoring, copy ending, crowd buff, doodad and generic NPC/skill
changes that are submitted as separate pull requests; it needs all of them to run end to end.

## Setup

| Step | What |
|---|---|
| Island level files | `worlds/instance_life_dungeon_daru/` (`cells/*/world_server/*`, `level_design/...`) extracted from the game pak into `ZoneGameDataRoot` |
| Zone host for copies | World `Config.json` `ZoneHost.Enabled` with `Executable` and `NativeDll` set, so World launches a host per dungeon copy |
| `aaemu_game` migration | `SQL/updates/2026-11-11_aaemu_game_character_indun_permits.sql` (applied at start) |

## Player flow

1. Queue with a Pass (item 54614). Matchmaking forms a group, or a solo match after the minimum wait, and
   invites the players. A vehicle out blocks the accept on the client.
2. Each player gets a role buff: Farmer, Woodcutter, Miner or Gatherer. Each role harvests one crop type.
3. Harvest phase (204 s). Mature and Shining crops score 1 point each. Small and Fully-Grown crops score through
   hidden counters: a point for every 3 small or 2 medium harvests. Shining crops last 30 s.
   Two Shining harvests within 50 s turn Productivity into Incredible Productivity, which unlocks the T skill.
4. "Harvesting will end in 30 seconds", then the crops are cleared and portals open (180 s). The portal leads to
   the Trembling Cactus.
5. At the cactus each player picks a role: Kill the Bugs! (Horizontal Slash) or Defeat the Stonecrabs! (Slam).
   The two roles exclude each other. Once two players hold a role, they get Ready to Help!, and Help the Cactus!
   summons Frightened Greenman Songsong.
6. Songsong is stunned for 180 s while Moss Bug and Moss Stonecrab waves attack it. Players protect it with their
   role skill.
7. After 180 s Songsong escapes: the chest is graded by the remaining Shining Hoverbloom Flowers, then Songsong
   takes buff 32835 and kills itself with 51225; the waves despawn with it. If the waves kill Songsong first there
   is no chest.
8. Songsong's death sends the reward mail, ranked by harvest points. The copy then counts down a 180 s end window
   with close warnings, returns the players to where they queued and closes.

## Content

| What | Ids |
|---|---|
| Event (`tower_defs`) | 202, progs 793, 794, 797 (harvest 204 s), 799 (30 s), 800 (portal 180 s), 801 (3 s) |
| Role buffs (grant the harvest skill requirement) | 32832 role, 32691 Farmer, 32694 Woodcutter, 32695 Miner, 32696 Gatherer |
| Crop doodads | 18138-18153 (Small/Fully-Grown/Mature/Shining per crop) |
| Crop spawner NPCs | 22078-22085 (OnSpawn plot summons the crops) |
| Score counters | doodads 18158-18165, hit skills 51182 (grade 3) / 51185 (grade 2), buffs 32760 / 32761 |
| Score source | `instance_gain_rules` of instance 80, targets in `instance_point_doodad_phase_changes` |
| Crop clout | `doodad_func_clouts` 5328/5329 (`target_max_count` 1) give the score buff to the harvester |
| Productivity | buff 32704 (rule 7, max 2, 50 s) transforms into 32705, required by skill 51194 |
| Crop eraser | NPC 22108, OnSpawn skill 51200, plot 7146 (RemoveDoodad per crop template, 120 m) |
| Trembling Cactus | doodad 18171: 51138 Kill the Bugs! / 51139 Defeat the Stonecrabs! (`next_phase` -1), 51150 Help the Cactus! |
| Ready to Help! | crowd buff: 32721 (crowd_number 2, radius 100) grants 32722 |
| Songsong | NPC 22076, initial buff 32711 (stun + root, 180 s), OnSpawn skill 51149, plot 7127 |
| Waves | spawn effects 4915-4931: Moss Bug 22088, Moss Stonecrab 22089, shining 22112/22113; 7-15 m, 100° arcs, `despawn_on_creator_death` |
| End of defence | 32711 timeout -> 51190 (chest grading on flowers 18192-18194 and Reward Doodad 18197) -> 51225 Songsong's Escape, ticked by 32835 (self-harm until death) |
| Reward mail | indun event 354 (Songsong killed) -> action 408, rank-backed `instance_rewards` kind 7 |

Rank rewards: rank 1-4 get 10, 9, 8, 5x item 54656; everyone gets 1x item 43338.

## Chest grading

Three Shining Darurium Flowers (18192-18194) stand by Songsong; the Reward Doodad (18197) counts how many wilt.

| Event | Flower | Reward Doodad 18197 |
|---|---|---|
| Songsong casts 51187 (3-star failure) | 18192 wilts | 53106 -> 53107 |
| Songsong casts 51188 (2-star failure) | 18193 wilts | 53107 -> 53108 |
| Escape grading 51190 | flowers stay lit | 53106 precious (22100), 53107 common (22101), 53108 old (22102) |
| Songsong dies first: on_death 51183 | all wilt | -> 53162 broken (22111) |

Opening the chest (51193) spawns the chest NPC in brackets 1 m ahead and the doodad goes invisible, removing
itself after an hour. 51187/51188 are in Songsong's AI list (`npc_ai_params` 4964) after 43759 "do nothing", and
are flagged usable while stunned.

## Server behaviour

These are generic mechanisms; the Island is the content that exercises them.

- **Dungeon copies run every NPC OnSpawn plot** (`OnSpawnPlotWorldGate`): the zone host fires none in a copy.
- **NPC scripts are not stopped by stun, sleep or silence** (`Skill.ScriptedCast`): OnSpawn, OnDeath and OnDespawn
  npc skills and plot/buff skill-use casts. An NPC's combat, idle and alert picks still are.
- **Area count cuts keep the cast target** (`SkillAreaRules.KeepNearest`): a harvest lands on the crop, not on the
  spawner NPC standing on it.
- **Clouts honour `target_max_count`, the caster first** (`AreaTriggerCloutRules.Targets`).
- **A stay option keeps a doodad that also offers an advancing option** (`DoodadStayOnPhaseRules`).
- **Rule 7 buffs transform on the application that reaches the ceiling** (`BuffStackRules.TransformsOnArrival`).
- **Crowd buffs** are granted when enough holders stand together (`CrowdBuffRules`, `CrowdBuffs`).
- **Zone-authored spawn effects scatter** over the row's angle and distance range, and despawn with their creator
  (`CreatorBoundSpawns`).
- **Instance event runs end after their last step** (`TowerDefRunEndRules`); the copy then opens its end window
  (`IndunCopyEnding`), warns at `Dungeons.CloseWarningSeconds` and closes.
- **Rank-backed instance rewards** are delivered per player by gain points (`InstanceRewardRankRules`,
  `IndunRewardDeliveryService.DeliverRanked`).
- **Self-destructs land on scripted casters**: a Source/SourceOnce damage row may hit its own non-player caster
  (`DamageEffect.AllowsCanAttackBypass`).
- **Plot summons at a location sit on the ground** (`SummonedDoodadPlacement`): World has no ground for the island
  (it is map objects over sea-floor terrain), so a summon at a spawner's zone spawn point uses that point less the
  dedicate's 0.5 m lift, however the fly-model spawner bobs.
- **A doodad the client still shows after the server removed it** is dropped on that client's first use
  (`StaleDoodadTargetRules`).
- **On-death NPC skills of a dungeon copy's NPCs run on World** (`OnSpawnPlotWorldGate.ShouldRunDeathSkills`): the
  copy's zone host fires no npc skills. Open-world NPCs the zone spawned keep theirs on the zone. Songsong's 51183
  gives the broken chest when the waves win.
- **A caster's death stops every plot it was running** (`PlotCasterDeathRules`): Songsong's OnSpawn plot 7127
  loops 66104 -> 66104 every 12 s and used to keep summoning waves at its removed body. On-death plots still run.
- **A charged dungeon entry is pushed to the client at once** (`SCInstanceVisitCountChange` from
  `Dungeon` via `IndunManager.GetVisitRow`); before, the client saw it only at the next character select.
- **Login out of an unhostable zone** goes to the return district (`LoginPlacement`).

## Testing aids

- `/item add self 54614 1` gives a Pass; `/indun reset 158` clears the daily entry cap.
- `/towerdef wave 202 4` inside the copy jumps to the portal step. The HUD timer keeps its own count.
- `/buff 32722` gives Ready to Help! when testing solo.

## Known gaps

- No flower wilts while Songsong lives. The only live caster of 51187/51188 is Songsong's AI (`npc_ai_params`
  4964: lists 43759, 51187, 51188; `np_skills` in_combat/in_idle); buff 32769 (51188) is marked unused. The AI
  pipe `phase_base` (pak `scripts/ai/goalpipes/pipe_manager_x2.lua`) uses `USF_PICK_BASE_SKILL_IN_BAD_CASE`,
  and the zone only ever casts the base skill 43759: stunned it casts nothing, unstunned 43759 every 3 s. Zone
  and World databases match for these rows. 51187/51188 differ from working skills by a self-centred 100 m area
  with no cap and usable-while-stunned bits; the skill list selection happens inside the zone host.
  Parked 2026-11-11: with `+ai_skill_debug 2 +ai_DrawGoals 1` on the copy host, two stunned runs printed no
  `--AI Skill` or `COPUseSkill` line although the zone held Songsong's aggro, so the picker is not reached. The data
  authors no trigger for the failures, and World does not invent one. Result: the escape always gives the precious
  chest; the common and old grades never occur. Next lead if resumed: remove 32711 mid-defence and read the trace.
