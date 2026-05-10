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
        [FirestoreProperty("pityCounter")] public int PityCounter { get; set; }
        [FirestoreProperty("summonLevel")] public int SummonLevel { get; set; } = 1;
        [FirestoreProperty("summonPullsInLevel")] public int SummonPullsInLevel { get; set; }
        [FirestoreProperty("currentChapter")] public int CurrentChapter { get; set; }
        [FirestoreProperty("currentStage")] public int CurrentStage { get; set; }
        [FirestoreProperty("stats")] public PlayerStatsSnapshotDoc Stats { get; set; }
        [FirestoreProperty("upgrades")] public List<UpgradeLevelEntryDoc> Upgrades { get; set; }
        [FirestoreProperty("equippedWeaponId")] public string EquippedWeaponId { get; set; }
        [FirestoreProperty("ownedWeapons")] public List<OwnedWeaponEntryDoc> OwnedWeapons { get; set; }
        [FirestoreProperty("ownedWeaponIds")] public List<string> OwnedWeaponIds { get; set; }
        [FirestoreProperty("ownedSkillIds")] public List<string> OwnedSkillIds { get; set; }
        [FirestoreProperty("equippedSkillSlots")] public List<string> EquippedSkillSlots { get; set; }
        [FirestoreProperty("dailyMissions")] public List<DailyMissionStateDoc> DailyMissions { get; set; }
        [FirestoreProperty("repeatMissions")] public List<RepeatMissionStateDoc> RepeatMissions { get; set; }
        [FirestoreProperty("attendance")] public AttendanceStateDoc Attendance { get; set; }
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
        [FirestoreProperty("maxHealth")] public float MaxHealth { get; set; }
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
}
