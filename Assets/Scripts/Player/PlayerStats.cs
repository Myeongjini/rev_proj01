using System;
using UnityEngine;
using WizardGrower.Armor;
using WizardGrower.Save;
using WizardGrower.Weapons;

namespace WizardGrower.Player
{
    [Serializable]
    public class PlayerStats
    {
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float autoAttackInterval = 1f;
        [SerializeField] private float manualAttackInterval = 0.3f;
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.1f;
        [SerializeField] private float criticalMultiplier = 2f;
        [SerializeField] private float armorPenetration = 0f;
        [SerializeField] private float defense = 0f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float combatPower = 10f;

        private PlayerStatsSnapshot baseSnapshot;
        private WeaponStats? equippedStats;
        private ArmorStats equippedArmorStats;

        public event Action Changed;
        public event Action HealthChanged;

        public float AttackDamage => attackDamage;
        public float AutoAttackInterval => autoAttackInterval;
        public float ManualAttackInterval => manualAttackInterval;
        public float CriticalChance => criticalChance;
        public float CriticalMultiplier => criticalMultiplier;
        public float ArmorPenetration => armorPenetration;
        public float Defense => defense;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float CombatPower => combatPower;

        public void AddAttackDamage(float amount)
        {
            EnsureBaseSnapshot();
            baseSnapshot.attackDamage += amount;
            RecomputeWithEquipment(equippedStats, equippedArmorStats);
        }

        public void AddAutoFireRate(float amount)
        {
            EnsureBaseSnapshot();
            baseSnapshot.autoAttackInterval = Mathf.Max(0.05f, baseSnapshot.autoAttackInterval - amount);
            RecomputeWithEquipment(equippedStats, equippedArmorStats);
        }

        public void AddManualFireRate(float amount)
        {
            EnsureBaseSnapshot();
            baseSnapshot.manualAttackInterval = Mathf.Max(0.05f, baseSnapshot.manualAttackInterval - amount);
            RecomputeWithEquipment(equippedStats, equippedArmorStats);
        }

        public void AddCriticalChance(float amount)
        {
            EnsureBaseSnapshot();
            baseSnapshot.criticalChance = Mathf.Clamp01(baseSnapshot.criticalChance + amount);
            RecomputeWithEquipment(equippedStats, equippedArmorStats);
        }

        public void AddCriticalMultiplier(float amount)
        {
            EnsureBaseSnapshot();
            baseSnapshot.criticalMultiplier = Mathf.Max(1f, baseSnapshot.criticalMultiplier + amount);
            RecomputeWithEquipment(equippedStats, equippedArmorStats);
        }

        public void AddArmorPenetration(float amount)
        {
            EnsureBaseSnapshot();
            baseSnapshot.armorPenetration = Mathf.Max(0f, baseSnapshot.armorPenetration + amount);
            RecomputeWithEquipment(equippedStats, equippedArmorStats);
        }

        public void AddMaxHealth(float amount)
        {
            EnsureBaseSnapshot();
            baseSnapshot.maxHealth = Mathf.Max(1f, baseSnapshot.maxHealth + amount);
            baseSnapshot.currentHealth = Mathf.Clamp(baseSnapshot.currentHealth + amount, 0f, baseSnapshot.maxHealth);
            RecomputeWithEquipment(equippedStats, equippedArmorStats);
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            EnsureBaseSnapshot();
            baseSnapshot.currentHealth = Mathf.Clamp(currentHealth, 0f, baseSnapshot.maxHealth);
            HealthChanged?.Invoke();
            Changed?.Invoke();
        }

        public void TakeHealth(float amount)
        {
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            EnsureBaseSnapshot();
            baseSnapshot.currentHealth = Mathf.Clamp(currentHealth, 0f, baseSnapshot.maxHealth);
            HealthChanged?.Invoke();
            Changed?.Invoke();
        }

        public void ApplySnapshot(PlayerStatsSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            equippedStats = null;
            equippedArmorStats = default;
            baseSnapshot = NormalizeSnapshot(snapshot);
            ApplyRuntimeSnapshot(baseSnapshot);
            RecalculateCombatPower();
            HealthChanged?.Invoke();
        }

        public PlayerStatsSnapshot CaptureSnapshot()
        {
            EnsureBaseSnapshot();
            baseSnapshot.currentHealth = Mathf.Clamp(currentHealth, 0f, baseSnapshot.maxHealth);
            return CloneSnapshot(baseSnapshot);
        }

        public void RecomputeWithEquipped(WeaponStats? equipped)
        {
            RecomputeWithEquipment(equipped, equippedArmorStats);
        }

        public void RecomputeWithEquipment(WeaponStats? equipped, ArmorStats armorStats)
        {
            EnsureBaseSnapshot();
            equippedStats = equipped;
            equippedArmorStats = armorStats;
            PlayerStatsSnapshot composed = ArmorStatComposer.Recompute(baseSnapshot, equippedStats, equippedArmorStats);
            ApplyRuntimeSnapshot(composed);
            RecalculateCombatPower();
            HealthChanged?.Invoke();
        }

        private void RecalculateCombatPower()
        {
            combatPower = attackDamage * (1f + criticalChance * (criticalMultiplier - 1f)) * (1f / Mathf.Max(0.05f, autoAttackInterval))
                + armorPenetration * 2f
                + defense * 1.5f
                + maxHealth * 0.1f;
            Changed?.Invoke();
        }

        private void EnsureBaseSnapshot()
        {
            if (baseSnapshot != null)
                return;

            baseSnapshot = NormalizeSnapshot(new PlayerStatsSnapshot
            {
                attackDamage = attackDamage,
                autoAttackDamage = attackDamage,
                manualAttackDamage = attackDamage * 2f,
                autoAttackInterval = autoAttackInterval,
                manualAttackInterval = manualAttackInterval,
                criticalChance = criticalChance,
                criticalMultiplier = criticalMultiplier,
                armorPenetration = armorPenetration,
                defense = defense,
                maxHealth = maxHealth,
                currentHealth = currentHealth
            });
        }

        private void ApplyRuntimeSnapshot(PlayerStatsSnapshot snapshot)
        {
            PlayerStatsSnapshot normalized = NormalizeSnapshot(snapshot);
            attackDamage = normalized.attackDamage;
            autoAttackInterval = normalized.autoAttackInterval;
            manualAttackInterval = normalized.manualAttackInterval;
            criticalChance = normalized.criticalChance;
            criticalMultiplier = normalized.criticalMultiplier;
            armorPenetration = normalized.armorPenetration;
            defense = normalized.defense;
            maxHealth = normalized.maxHealth;
            currentHealth = normalized.currentHealth;
        }

        private static PlayerStatsSnapshot NormalizeSnapshot(PlayerStatsSnapshot snapshot)
        {
            PlayerStatsSnapshot source = snapshot ?? new PlayerStatsSnapshot();
            float normalizedMaxHealth = Mathf.Max(1f, source.maxHealth);
            float normalizedAttack = source.attackDamage > 0f
                ? source.attackDamage
                : (source.autoAttackDamage > 0f ? source.autoAttackDamage : 10f);
            return new PlayerStatsSnapshot
            {
                attackDamage = normalizedAttack,
                autoAttackDamage = source.autoAttackDamage > 0f ? source.autoAttackDamage : normalizedAttack,
                manualAttackDamage = source.manualAttackDamage > 0f ? source.manualAttackDamage : normalizedAttack * 2f,
                autoAttackInterval = Mathf.Max(0.05f, source.autoAttackInterval),
                manualAttackInterval = Mathf.Max(0.05f, source.manualAttackInterval),
                criticalChance = Mathf.Clamp01(source.criticalChance),
                criticalMultiplier = Mathf.Max(1f, source.criticalMultiplier),
                armorPenetration = Mathf.Max(0f, source.armorPenetration),
                defense = Mathf.Max(0f, source.defense),
                maxHealth = normalizedMaxHealth,
                maxMana = Mathf.Max(0f, source.maxMana),
                currentHealth = Mathf.Clamp(source.currentHealth <= 0f ? normalizedMaxHealth : source.currentHealth, 0f, normalizedMaxHealth)
            };
        }

        private static PlayerStatsSnapshot CloneSnapshot(PlayerStatsSnapshot source)
        {
            return new PlayerStatsSnapshot
            {
                attackDamage = source.attackDamage,
                autoAttackDamage = source.attackDamage,
                manualAttackDamage = source.attackDamage * 2f,
                autoAttackInterval = source.autoAttackInterval,
                manualAttackInterval = source.manualAttackInterval,
                criticalChance = source.criticalChance,
                criticalMultiplier = source.criticalMultiplier,
                armorPenetration = source.armorPenetration,
                defense = source.defense,
                maxHealth = source.maxHealth,
                maxMana = source.maxMana,
                currentHealth = source.currentHealth
            };
        }

        internal void SetCombatPower(float value)
        {
            combatPower = Mathf.Max(0f, value);
        }
    }
}
