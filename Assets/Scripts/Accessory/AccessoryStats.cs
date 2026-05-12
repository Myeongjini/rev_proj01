using System;

namespace WizardGrower.Accessory
{
    [Serializable]
    public struct AccessoryStats
    {
        public float attackDamage;
        public float criticalChance;
        public float criticalMultiplier;
        public float maxHealth;
        public float maxMana;
        public float armorPenetration;
        public float defense;

        public void Add(AccessoryStats other)
        {
            attackDamage += other.attackDamage;
            criticalChance += other.criticalChance;
            criticalMultiplier += other.criticalMultiplier;
            maxHealth += other.maxHealth;
            maxMana += other.maxMana;
            armorPenetration += other.armorPenetration;
            defense += other.defense;
        }
    }
}
