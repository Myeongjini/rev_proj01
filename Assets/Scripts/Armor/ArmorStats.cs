using System;

namespace WizardGrower.Armor
{
    [Serializable]
    public struct ArmorStats
    {
        public float defense;
        public float criticalChance;
        public float criticalMultiplier;
        public float maxHealth;
        public float maxMana;
        public float attackDamageBonus;

        public void Add(ArmorStats other)
        {
            defense += other.defense;
            criticalChance += other.criticalChance;
            criticalMultiplier += other.criticalMultiplier;
            maxHealth += other.maxHealth;
            maxMana += other.maxMana;
            attackDamageBonus += other.attackDamageBonus;
        }
    }
}
