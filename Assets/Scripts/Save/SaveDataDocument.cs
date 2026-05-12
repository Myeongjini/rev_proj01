using System.Collections.Generic;
using Firebase.Firestore;
using WizardGrower.Attendance;
using WizardGrower.Missions;

namespace WizardGrower.Save
{
    [FirestoreData]
    public class SaveDataDocument
    {
        [FirestoreProperty("saveVersion")] public int SaveVersion { get; set; }
        [FirestoreProperty("userId")] public string UserId { get; set; }
        [FirestoreProperty("updatedAtUnixMs")] public long UpdatedAtUnixMs { get; set; }
        [FirestoreProperty("gold")] public int Gold { get; set; }
        [FirestoreProperty("gems")] public int Gems { get; set; } = 300;
        [FirestoreProperty("enhancementStone")] public int EnhancementStone { get; set; }
        [FirestoreProperty("pityCounter")] public int PityCounter { get; set; }
        [FirestoreProperty("summonLevel")] public int SummonLevel { get; set; } = 1;
        [FirestoreProperty("summonPullsInLevel")] public int SummonPullsInLevel { get; set; }
        [FirestoreProperty("playerLevel")] public int PlayerLevel { get; set; } = 1;
        [FirestoreProperty("playerCurrentExp")] public int PlayerCurrentExp { get; set; }
        [FirestoreProperty("currentChapter")] public int CurrentChapter { get; set; }
        [FirestoreProperty("currentStage")] public int CurrentStage { get; set; }
        [FirestoreProperty("stats")] public PlayerStatsSnapshotDoc Stats { get; set; }
        [FirestoreProperty("upgrades")] public List<UpgradeLevelEntryDoc> Upgrades { get; set; }
        [FirestoreProperty("equippedWeaponId")] public string EquippedWeaponId { get; set; }
        [FirestoreProperty("ownedWeapons")] public List<OwnedWeaponEntryDoc> OwnedWeapons { get; set; }
        [FirestoreProperty("ownedArmors")] public List<OwnedArmorEntryDoc> OwnedArmors { get; set; }
        [FirestoreProperty("equippedArmors")] public List<EquippedArmorEntryDoc> EquippedArmors { get; set; }
        [FirestoreProperty("equippedArmorBySlot")] public Dictionary<string, string> EquippedArmorBySlot { get; set; }
        [FirestoreProperty("ownedAccessories")] public List<OwnedAccessoryEntryDoc> OwnedAccessories { get; set; }
        [FirestoreProperty("equippedAccessories")] public List<EquippedAccessoryEntryDoc> EquippedAccessories { get; set; }
        [FirestoreProperty("equippedAccessoryBySlot")] public Dictionary<string, string> EquippedAccessoryBySlot { get; set; }
        [FirestoreProperty("eliteSpawnCounter")] public int EliteSpawnCounter { get; set; }
        [FirestoreProperty("ownedWeaponIds")] public List<string> OwnedWeaponIds { get; set; }
        [FirestoreProperty("ownedSkillIds")] public List<string> OwnedSkillIds { get; set; }
        [FirestoreProperty("equippedSkillSlots")] public List<string> EquippedSkillSlots { get; set; }
        [FirestoreProperty("dailyMissions")] public List<DailyMissionStateDoc> DailyMissions { get; set; }
        [FirestoreProperty("repeatMissions")] public List<RepeatMissionStateDoc> RepeatMissions { get; set; }
        [FirestoreProperty("attendance")] public AttendanceStateDoc Attendance { get; set; }
        [FirestoreProperty("lastSeenAtUtcMs")] public long LastSeenAtUtcMs { get; set; }
        [FirestoreProperty("offlineRewardPending")] public long OfflineRewardPending { get; set; }
        [FirestoreProperty("offlineRewardPendingExp")] public long OfflineRewardPendingExp { get; set; }
        [FirestoreProperty("goldDungeon")] public GoldDungeonStateDoc GoldDungeon { get; set; }
        [FirestoreProperty("expDungeon")] public EXPDungeonStateDoc ExpDungeon { get; set; }
        [FirestoreProperty("enhancementStoneDungeon")] public EnhancementStoneDungeonStateDoc EnhancementStoneDungeon { get; set; }
    }

    [FirestoreData]
    public class PlayerStatsSnapshotDoc
    {
        [FirestoreProperty("attackDamage")] public float AttackDamage { get; set; }
        [FirestoreProperty("autoAttackDamage")] public float AutoAttackDamage { get; set; }
        [FirestoreProperty("manualAttackDamage")] public float ManualAttackDamage { get; set; }
        [FirestoreProperty("autoAttackInterval")] public float AutoAttackInterval { get; set; }
        [FirestoreProperty("manualAttackInterval")] public float ManualAttackInterval { get; set; }
        [FirestoreProperty("criticalChance")] public float CriticalChance { get; set; }
        [FirestoreProperty("criticalMultiplier")] public float CriticalMultiplier { get; set; }
        [FirestoreProperty("armorPenetration")] public float ArmorPenetration { get; set; }
        [FirestoreProperty("defense")] public float Defense { get; set; }
        [FirestoreProperty("maxHealth")] public float MaxHealth { get; set; }
        [FirestoreProperty("maxMana")] public float MaxMana { get; set; }
        [FirestoreProperty("currentHealth")] public float CurrentHealth { get; set; }
    }

    [FirestoreData]
    public class UpgradeLevelEntryDoc
    {
        [FirestoreProperty("id")] public string Id { get; set; }
        [FirestoreProperty("level")] public int Level { get; set; }
    }

    [FirestoreData]
    public class OwnedWeaponEntryDoc
    {
        [FirestoreProperty("weaponId")] public string WeaponId { get; set; }
        [FirestoreProperty("count")] public int Count { get; set; }
    }

    [FirestoreData]
    public class OwnedArmorEntryDoc
    {
        [FirestoreProperty("armorId")] public string ArmorId { get; set; }
        [FirestoreProperty("count")] public int Count { get; set; }
    }

    [FirestoreData]
    public class EquippedArmorEntryDoc
    {
        [FirestoreProperty("slot")] public string Slot { get; set; }
        [FirestoreProperty("armorId")] public string ArmorId { get; set; }
    }

    [FirestoreData]
    public class OwnedAccessoryEntryDoc
    {
        [FirestoreProperty("accessoryId")] public string AccessoryId { get; set; }
        [FirestoreProperty("count")] public int Count { get; set; }
    }

    [FirestoreData]
    public class EquippedAccessoryEntryDoc
    {
        [FirestoreProperty("slot")] public string Slot { get; set; }
        [FirestoreProperty("accessoryId")] public string AccessoryId { get; set; }
    }

    [FirestoreData]
    public class DailyMissionStateDoc
    {
        [FirestoreProperty("missionId")] public string MissionId { get; set; }
        [FirestoreProperty("progress")] public int Progress { get; set; }
        [FirestoreProperty("claimed")] public bool Claimed { get; set; }
        [FirestoreProperty("lastResetUtcMs")] public long LastResetUtcMs { get; set; }
    }

    [FirestoreData]
    public class RepeatMissionStateDoc
    {
        [FirestoreProperty("missionId")] public string MissionId { get; set; }
        [FirestoreProperty("currentTargetN")] public int CurrentTargetN { get; set; }
        [FirestoreProperty("runningCounter")] public int RunningCounter { get; set; }
    }

    [FirestoreData]
    public class AttendanceStateDoc
    {
        [FirestoreProperty("currentDayIndex")] public int CurrentDayIndex { get; set; } = 1;
        [FirestoreProperty("lastClaimedUtcMs")] public long LastClaimedUtcMs { get; set; }
        [FirestoreProperty("totalCheckIns")] public int TotalCheckIns { get; set; }
    }

    [FirestoreData]
    public class GoldDungeonStateDoc
    {
        [FirestoreProperty("lastEntryDateUtcMs")] public long LastEntryDateUtcMs { get; set; }
        [FirestoreProperty("todayEntryCount")] public int TodayEntryCount { get; set; }
        [FirestoreProperty("bestScore")] public long BestScore { get; set; }
    }

    [FirestoreData]
    public class EXPDungeonStateDoc
    {
        [FirestoreProperty("lastEntryDateUtcMs")] public long LastEntryDateUtcMs { get; set; }
        [FirestoreProperty("todayEntryCount")] public int TodayEntryCount { get; set; }
        [FirestoreProperty("bestScore")] public long BestScore { get; set; }
    }

    [FirestoreData]
    public class EnhancementStoneDungeonStateDoc
    {
        [FirestoreProperty("lastEntryDateUtcMs")] public long LastEntryDateUtcMs { get; set; }
        [FirestoreProperty("todayEntryCount")] public int TodayEntryCount { get; set; }
        [FirestoreProperty("bestScore")] public long BestScore { get; set; }
    }
}
