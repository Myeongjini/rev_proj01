using System;
using UnityEngine;

namespace WizardGrower.Player
{
    [Serializable]
    public class PlayerStats
    {
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float autoAttackInterval = 1f;
        [SerializeField] private float manualAttackMultiplier = 2f;
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.1f;
        [SerializeField] private float criticalMultiplier = 2f;
        [SerializeField] private float combatPower = 10f;

        public event Action Changed;

        public float AttackDamage => attackDamage;
        public float AutoAttackInterval => autoAttackInterval;
        public float ManualAttackMultiplier => manualAttackMultiplier;
        public float CriticalChance => criticalChance;
        public float CriticalMultiplier => criticalMultiplier;
        public float CombatPower => combatPower;

        public void AddAttack(float amount)
        {
            attackDamage += amount;
            RecalculateCombatPower();
        }

        public void AddCriticalChance(float amount)
        {
            criticalChance = Mathf.Clamp01(criticalChance + amount);
            RecalculateCombatPower();
        }

        public void AddCriticalMultiplier(float amount)
        {
            criticalMultiplier += amount;
            RecalculateCombatPower();
        }

        private void RecalculateCombatPower()
        {
            combatPower = attackDamage * (1f + criticalChance * (criticalMultiplier - 1f));
            Changed?.Invoke();
        }
    }
}
