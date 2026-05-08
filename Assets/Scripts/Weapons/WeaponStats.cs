using System;

namespace WizardGrower.Weapons
{
    [Serializable]
    public struct WeaponStats
    {
        public float attackDamage;
        public float attackSpeedBonus;
        public float criticalChance;
        public float criticalMultiplier;
        public float armorPenetration;
        public float maxHealth;
        public float maxMana;
    }
}
