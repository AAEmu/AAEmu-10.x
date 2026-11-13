-- Skill cooldowns that outlive a logout (e.g. a plot-authored 4 h cooldown).
CREATE TABLE IF NOT EXISTS `character_skill_cooldowns` (
  `character_id` INT UNSIGNED NOT NULL COMMENT 'Character who owns this cooldown',
  `skill_id`     INT UNSIGNED NOT NULL COMMENT 'SkillTemplate.Id from game data',
  `duration`     INT UNSIGNED NOT NULL COMMENT 'Original cooldown length in milliseconds',
  `ends_at`      DATETIME(3) NOT NULL COMMENT 'UTC time the cooldown ends; keeps running offline',
  PRIMARY KEY (`character_id`, `skill_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Running skill cooldowns across sessions';

-- Cooldown tags keep their own end time: a skill's timer can be shortened while the tag it shares is not.
CREATE TABLE IF NOT EXISTS `character_skill_tag_cooldowns` (
  `character_id` INT UNSIGNED NOT NULL COMMENT 'Character who owns this cooldown',
  `tag_id`       INT UNSIGNED NOT NULL COMMENT 'Cooldown tag id from skills.cooldown_tag_id',
  `duration`     INT UNSIGNED NOT NULL COMMENT 'Original cooldown length in milliseconds',
  `ends_at`      DATETIME(3) NOT NULL COMMENT 'UTC time the tag cooldown ends; keeps running offline',
  PRIMARY KEY (`character_id`, `tag_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Running skill cooldown tags across sessions';
