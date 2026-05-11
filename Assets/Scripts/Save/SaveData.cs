using System;
using System.Collections.Generic;
using WizardGrower.Attendance;
using WizardGrower.Dungeons;
using WizardGrower.Missions;
using WizardGrower.Skills;
using WizardGrower.Weapons;

namespace WizardGrower.Save
{
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 6;
        public string userId = "local";
        public long updatedAtUnixMs;

        public int gold;
        public int gems = 300;
        public int pityCounter;
        public int summonLevel = 1;
        public int summonPullsInLevel;
        public int playerLevel = 1;
        public int playerCurrentExp;
        public int currentChapter = 1;
        public int currentStage = 1;

        public PlayerStatsSnapshot stats = new PlayerStatsSnapshot();
        public List<UpgradeLevelEntry> upgrades = new List<UpgradeLevelEntry>();
        public string equippedWeaponId = "common_beginner_staff";
        public List<OwnedWeaponEntry> ownedWeapons = new List<OwnedWeaponEntry> { new OwnedWeaponEntry("common_beginner_staff", 1) };
        public List<string> ownedWeaponIds = new List<string> { "wand_starter" };
        public List<string> ownedSkillIds = new List<string>(SkillId.DefaultOwned);
        public List<string> equippedSkillSlots = new List<string> { SkillId.Meteor, string.Empty, string.Empty, string.Empty, string.Empty };
        public List<DailyMissionState> dailyMissions = new List<DailyMissionState>();
        public List<RepeatMissionState> repeatMissions = new List<RepeatMissionState>();
        public AttendanceState attendance = new AttendanceState();
        public long lastSeenAtUtcMs;
        public long offlineRewardPending;
        public GoldDungeonState goldDungeon = new GoldDungeonState();
    }

    [Serializable]
    public class PlayerStatsSnapshot
    {
        public float attackDamage = 10f;
        public float autoAttackDamage = 10f;
        public float manualAttackDamage = 20f;
        public float autoAttackInterval = 1f;
        public float manualAttackInterval = 0.3f;
        public float criticalChance = 0.1f;
        public float criticalMultiplier = 2f;
        public float armorPenetration;
        public float maxHealth = 100f;
        public float currentHealth = 100f;
    }

    [Serializable]
    public class UpgradeLevelEntry
    {
        public string id;
        public int level;
    }
}
