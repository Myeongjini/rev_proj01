using System;
using UnityEngine;
using WizardGrower.Save;

namespace WizardGrower.Player
{
    [Serializable]
    public class PlayerStats
    {
        [SerializeField] private float autoAttackDamage = 10f;
        [SerializeField] private float manualAttackDamage = 20f;
        [SerializeField] private float autoAttackInterval = 1f;
        [SerializeField] private float manualAttackInterval = 0.3f;
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.1f;
        [SerializeField] private float criticalMultiplier = 2f;
        [SerializeField] private float armorPenetration = 0f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float combatPower = 10f;

        public event Action Changed;
        public event Action HealthChanged;

        public float AutoAttackDamage => autoAttackDamage;
        public float ManualAttackDamage => manualAttackDamage;
        public float AutoAttackInterval => autoAttackInterval;
        public float ManualAttackInterval => manualAttackInterval;
        public float CriticalChance => criticalChance;
        public float CriticalMultiplier => criticalMultiplier;
        public float ArmorPenetration => armorPenetration;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float CombatPower => combatPower;

        public void AddAutoDamage(float amount)
        {
            autoAttackDamage += amount;
            RecalculateCombatPower();
        }

        public void AddManualDamage(float amount)
        {
            manualAttackDamage += amount;
            RecalculateCombatPower();
        }

        public void AddAutoFireRate(float amount)
        {
            autoAttackInterval = Mathf.Max(0.05f, autoAttackInterval - amount);
            RecalculateCombatPower();
        }

        public void AddManualFireRate(float amount)
        {
            manualAttackInterval = Mathf.Max(0.05f, manualAttackInterval - amount);
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

        public void AddArmorPenetration(float amount)
        {
            armorPenetration += amount;
            RecalculateCombatPower();
        }

        public void AddMaxHealth(float amount)
        {
            maxHealth = Mathf.Max(1f, maxHealth + amount);
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            HealthChanged?.Invoke();
            RecalculateCombatPower();
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke();
            Changed?.Invoke();
        }

        public void TakeHealth(float amount)
        {
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            HealthChanged?.Invoke();
            Changed?.Invoke();
        }

        public void ApplySnapshot(PlayerStatsSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            autoAttackDamage = snapshot.autoAttackDamage;
            manualAttackDamage = snapshot.manualAttackDamage;
            autoAttackInterval = Mathf.Max(0.05f, snapshot.autoAttackInterval);
            manualAttackInterval = Mathf.Max(0.05f, snapshot.manualAttackInterval);
            criticalChance = Mathf.Clamp01(snapshot.criticalChance);
            criticalMultiplier = Mathf.Max(1f, snapshot.criticalMultiplier);
            armorPenetration = Mathf.Max(0f, snapshot.armorPenetration);
            maxHealth = Mathf.Max(1f, snapshot.maxHealth);
            currentHealth = Mathf.Clamp(snapshot.currentHealth <= 0f ? maxHealth : snapshot.currentHealth, 0f, maxHealth);
            RecalculateCombatPower();
            HealthChanged?.Invoke();
        }

        public PlayerStatsSnapshot CaptureSnapshot()
        {
            return new PlayerStatsSnapshot
            {
                autoAttackDamage = autoAttackDamage,
                manualAttackDamage = manualAttackDamage,
                autoAttackInterval = autoAttackInterval,
                manualAttackInterval = manualAttackInterval,
                criticalChance = criticalChance,
                criticalMultiplier = criticalMultiplier,
                armorPenetration = armorPenetration,
                maxHealth = maxHealth,
                currentHealth = currentHealth
            };
        }

        private void RecalculateCombatPower()
        {
            combatPower = autoAttackDamage * (1f + criticalChance * (criticalMultiplier - 1f));
            Changed?.Invoke();
        }
    }
}
