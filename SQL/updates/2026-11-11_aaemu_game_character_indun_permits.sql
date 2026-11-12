-- Registered dungeon passes: the pass item is used up when it is registered, so the registration must outlive a restart.
CREATE TABLE IF NOT EXISTS `character_indun_permits` (
  `character_id`  INT UNSIGNED NOT NULL COMMENT 'Character who registered the pass',
  `zone_group_id` INT UNSIGNED NOT NULL COMMENT 'indun_zones.zone_group_id',
  `registered_at` DATETIME(3) NOT NULL COMMENT 'UTC time the pass was registered',
  `spent_at`      DATETIME(3) NULL COMMENT 'UTC time the pass was used to enter; NULL while unspent',
  PRIMARY KEY (`character_id`, `zone_group_id`, `registered_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Registered dungeon passes';
