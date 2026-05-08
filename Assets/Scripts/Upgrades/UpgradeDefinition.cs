using System;

namespace WizardGrower.Upgrades
{
    public enum UpgradeType
    {
        AttackDamage,
        AttackSpeed,
        CriticalChance,
        CriticalMultiplier,
        ArmorPenetration,
        MaxHealth,
        Mana,
        AutoDamage,
        ManualDamage,
        AutoFireRate,
        ManualFireRate,
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
