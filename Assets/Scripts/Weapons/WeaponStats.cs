using System;

namespace WizardGrower.Weapons
{
    [Serializable]
    public struct WeaponStats
    {
        public float autoAttackDamage;
        public float manualAttackDamage;
        public float autoFireRateBonus;
        public float criticalChance;
        public float criticalMultiplier;
        public float armorPenetration;
        public float maxHealth;
    }
}
