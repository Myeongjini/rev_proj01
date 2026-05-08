using System;
using System.Collections.Generic;
using WizardGrower.Weapons;

namespace WizardGrower.Save
{
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 3;
        public string userId = "local";
        public long updatedAtUnixMs;

        public int gold;
        public int gems = 300;
        public int pityCounter;
        public int currentChapter = 1;
        public int currentStage = 1;

        public PlayerStatsSnapshot stats = new PlayerStatsSnapshot();
        public List<UpgradeLevelEntry> upgrades = new List<UpgradeLevelEntry>();
        public string equippedWeaponId = "common_beginner_staff";
        public List<OwnedWeaponEntry> ownedWeapons = new List<OwnedWeaponEntry> { new OwnedWeaponEntry("common_beginner_staff", 1) };
        public List<string> ownedWeaponIds = new List<string> { "wand_starter" };
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
