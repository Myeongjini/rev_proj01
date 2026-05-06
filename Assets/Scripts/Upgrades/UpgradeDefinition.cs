using System;

namespace WizardGrower.Upgrades
{
    public enum UpgradeType
    {
        AutoDamage,
        ManualDamage,
        AutoFireRate,
        ManualFireRate,
        CriticalChance,
        CriticalMultiplier,
        ArmorPenetration,
        MaxHealth,
        Mana,
    }

    [Serializable]
    public class UpgradeDefinition
    {
        public string id;
        public string displayName;
        public UpgradeType type;
        public int baseCost;
        public float value;
        public float costScale = 1.45f;
    }
}
