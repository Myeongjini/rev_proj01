using System.Collections.Generic;
using Firebase.Firestore;

namespace WizardGrower.Save
{
    [FirestoreData]
    public class SaveDataDocument
    {
        [FirestoreProperty("saveVersion")] public int SaveVersion { get; set; }
        [FirestoreProperty("userId")] public string UserId { get; set; }
        [FirestoreProperty("updatedAtUnixMs")] public long UpdatedAtUnixMs { get; set; }
        [FirestoreProperty("gold")] public int Gold { get; set; }
        [FirestoreProperty("currentChapter")] public int CurrentChapter { get; set; }
        [FirestoreProperty("currentStage")] public int CurrentStage { get; set; }
        [FirestoreProperty("stats")] public PlayerStatsSnapshotDoc Stats { get; set; }
        [FirestoreProperty("upgrades")] public List<UpgradeLevelEntryDoc> Upgrades { get; set; }
        [FirestoreProperty("equippedWeaponId")] public string EquippedWeaponId { get; set; }
        [FirestoreProperty("ownedWeaponIds")] public List<string> OwnedWeaponIds { get; set; }
    }

    [FirestoreData]
    public class PlayerStatsSnapshotDoc
    {
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
}
