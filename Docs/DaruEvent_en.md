# Daru Event

The 10.0 Daru event: the Daru Transformation, its costumes, and the Island of Abundance life dungeon. The island
has its own page: [IslandOfAbundance_en.md](IslandOfAbundance_en.md).

## Setup

| Step | Required | What |
|---|---|---|
| Server data patch | if your data ships them disabled | `SQL/patches/compact/2026-11-11-daru-event-merchant-goods.sql` on the server's `compact.sqlite3` |
| `aaemu_game` migration | yes (applied at start) | `SQL/updates/2026-11-11_aaemu_game_character_skill_cooldowns.sql` |
| Client patch | opt-in | the same `merchant_goods` statement on the client's game DB, re-packed, only if your client hides the goods |

## Daru Transformation

Skill 50650 (`char_transform_effects` race 9) turns the character into a Daru for 30 minutes.

| Behaviour | Server mechanism |
|---|---|
| The caster and onlookers see a Daru | Visual-race override (`SCCharacterVisualRaceModified` 0x273) with the Daru template's body parts; `ModelId` is unchanged, so the caster never goes invisible. Players who come into view later get the form too |
| Weapons sheathed, no combat | `SCDisarmamentChanged` 0x2E9 for Mainhand/Offhand/Ranged; `CharTransformCombatRules` refuses auto-attacks and attack-tab skills (`default_skills.skill_book_category_id` 2) except those whose effects are all special effects (Escape, Rider's Escape, Report) |
| Class and racial skills refused | `skill_reqs` 417 (tag 5947) is enforced as authored |
| Login while transformed | The form is announced after `CSNotifyInGame`, never before the unit exists |
| 4 h cooldown after the form ends | Plot 7031 as authored; skill cooldowns are saved (`character_skill_cooldowns`), so it survives a relog |

## Costumes

| Slot | Rule (`CostumeSlotRules`) |
|---|---|
| 32 race_cosplay (performance) | Equippable; its synthesis effects count only while transformed |
| 33 race_cosplaylooks (appearance) | Equippable, cosmetic only |

The equipment container grows to the 34 wire slots. Synthesis effects expire after the item's
`rnd_attr_unit_modifier_lifetime` (`ItemRndAttrLifetimeRules`); a worn piece loses them as soon as they run out.
The Costume Cleaner (special effect 124) restarts that window; a rejected recharge keeps the cleaner.

## Trade packs on a donkey

Const tag `donkey_class_to_check_transformation` (npc tag 5958) marks the donkeys that do not slow down when a Daru
rider wears a backpack. The data authors no server-side bonus for it, so the server does nothing here.

## Event goods

Preservation Bag, Pocket Watch and Costume Cleaner are sold once their `merchant_goods` rows are enabled. The
Pocket Watch resets the transformation cooldown, and the Coin of Abundance exchange uses the normal crafts at
doodad 7402.

## Known gaps

- Island chest grades: only precious (Songsong escapes) and broken (Songsong dies) occur; see the island page.
