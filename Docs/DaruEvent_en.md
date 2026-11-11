# Daru Event

The 10.0 Daru event: the Daru Transformation, its costumes and trade bonus, and the Island of Abundance life
dungeon. The island itself has its own page: [IslandOfAbundance_en.md](IslandOfAbundance_en.md).

## Setup

| Step | Required | What |
|---|---|---|
| Server data patch | yes | `SQL/patches/compact/2026-11-11-daru-event-merchant-goods.sql` on the server's `compact.sqlite3` |
| Island level files | yes, for the island | `worlds/instance_life_dungeon_daru/` (`cells/*/world_server/*`, `level_design/...`) extracted from the game pak into `ZoneGameDataRoot` |
| Zone host for copies | yes, for the island | World `Config.json` `ZoneHost.Enabled` with `Executable` and `NativeDll` set, so World launches a host per dungeon copy |
| Client patch | opt-in | the same `merchant_goods` statement on the client's game DB, re-packed, only if your client hides the goods |
| `aaemu_game` migration | none | no MySQL table changes |

## Daru Transformation

Skill 50650 (`char_transform_effects` race 9) turns the character into a Daru for 30 minutes.

| Behaviour | Server mechanism |
|---|---|
| The caster and onlookers see a Daru | Visual-race override (`SCCharacterVisualRaceModified` 0x273) with the Daru template's body parts; `ModelId` is unchanged, so the caster never goes invisible |
| Weapons sheathed, no combat | `SCDisarmamentChanged` 0x2E9 for Mainhand/Offhand/Ranged; `CharTransformCombatRules` refuses auto-attacks and attack-tab skills (`default_skills.skill_book_category_id` 2) except Escape, Rider's Escape and Report |
| Class and racial skills refused | `skill_reqs` 417 (tag 5947) is enforced as authored |
| Login while transformed | The form is announced after `CSNotifyInGame`, never before the unit exists |
| Cancel puts the 4 h cooldown on | Plot 7031 as authored |

## Costumes

| Slot | Rule (`CostumeSlotRules`) |
|---|---|
| 32 race_cosplay (performance) | Equippable; its synthesis effects count only while transformed |
| 33 race_cosplaylooks (appearance) | Equippable, cosmetic only |

The equipment container grows to the 34 wire slots. Synthesis effects expire after the item's
`rnd_attr_unit_modifier_lifetime` (`ItemRndAttrLifetimeRules`), and the Costume Cleaner (special effect 124)
restarts that window.

## Trade packs on a donkey

A donkey (`const_tags` 60, npc tag 5958) driven by a transformed rider carrying a trade pack moves 25 % faster
(`CharTransformMountRules`). The client and zone data hold no amount, so the bonus is World-side.

## Event goods

Preservation Bag, Pocket Watch and Costume Cleaner are sold once the server data patch is applied. The Pocket
Watch resets the transformation cooldown, and the Coin of Abundance exchange uses the normal crafts at doodad 7402.

## Island of Abundance and dungeon entry

- Daily entries and passes are counted per character and shown in the dungeon list as soon as they are charged.
- Each dungeon copy runs in its own zone host. Object ids freed inside a copy stay reserved until the copy closes.
- A finished copy warns at `Dungeons.CloseWarningSeconds` (default 60 and 10 s) and then returns its players.
- See [IslandOfAbundance_en.md](IslandOfAbundance_en.md) for the event flow, the rewards and the known gaps.

## Known gaps

- The natural-expiry cooldown depends on plot 7031 staying alive for 30 minutes; a relog or restart in between
  loses it.
- Island chest grades: only precious (Songsong escapes) and broken (Songsong dies) occur; see the island page.
- Entry counts and passes live in memory and reset when World restarts.

## Testing aids

- `/item add self 54614 1` gives an island Pass; `/indun reset 158` clears the daily entries and passes.
- `/towerdef wave 202 4` inside the island copy jumps to the defence step.
