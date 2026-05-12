using UnityEngine;
using WizardGrower.Enhancement;
using WizardGrower.Save;

namespace WizardGrower.Weapons
{
    public static class WeaponStatComposer
    {
        /// <summary>
        /// Returns a final stat snapshot from base stats plus one equipped weapon.
        ///
        /// | field                | base op | equipped op                         | clamp |
        /// |----------------------|---------|-------------------------------------|-------|
        /// | attackDamage         | base.x  | + equipped.attackDamage             | none  |
        /// | autoAttackInterval   | base.x  | - equipped.attackSpeedBonus         | >=0.05|
        /// | criticalChance       | base.x  | + equipped.criticalChance           | 0..1  |
        /// | criticalMultiplier   | base.x  | + equipped.criticalMultiplier       | >=1   |
        /// | armorPenetration     | base.x  | + equipped.armorPenetration         | >=0   |
        /// | maxHealth            | base.x  | + equipped.maxHealth                | >=1   |
        /// | currentHealth        | base.x  | unchanged                           | <=max |
        /// | manualAttackInterval | base.x  | unchanged                           | >=0.05|
        /// </summary>
        public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? equipped)
        {
            return Recompute(baseSnapshot, equipped, 0);
        }

        public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? equipped, int enhancementLevel)
        {
            PlayerStatsSnapshot source = baseSnapshot ?? new PlayerStatsSnapshot();
            PlayerStatsSnapshot result = new PlayerStatsSnapshot
            {
                attackDamage = source.attackDamage > 0f ? source.attackDamage : (source.autoAttackDamage > 0f ? source.autoAttackDamage : 10f),
                autoAttackDamage = source.autoAttackDamage,
                manualAttackDamage = source.manualAttackDamage,
                autoAttackInterval = Mathf.Max(0.05f, source.autoAttackInterval),
                manualAttackInterval = Mathf.Max(0.05f, source.manualAttackInterval),
                criticalChance = Mathf.Clamp01(source.criticalChance),
                criticalMultiplier = Mathf.Max(1f, source.criticalMultiplier),
                armorPenetration = Mathf.Max(0f, source.armorPenetration),
                defense = Mathf.Max(0f, source.defense),
                maxHealth = Mathf.Max(1f, source.maxHealth),
                maxMana = Mathf.Max(0f, source.maxMana),
                currentHealth = source.currentHealth
            };

            if (equipped.HasValue)
            {
                WeaponStats bonus = ApplyEnhancement(equipped.Value, enhancementLevel);
                result.attackDamage += bonus.attackDamage;
                result.autoAttackDamage = result.attackDamage;
                result.manualAttackDamage = result.attackDamage * 2f;
                result.autoAttackInterval = Mathf.Max(0.05f, result.autoAttackInterval - bonus.attackSpeedBonus);
                result.criticalChance = Mathf.Clamp01(result.criticalChance + bonus.criticalChance);
                result.criticalMultiplier = Mathf.Max(1f, result.criticalMultiplier + bonus.criticalMultiplier);
                result.armorPenetration = Mathf.Max(0f, result.armorPenetration + bonus.armorPenetration);
                result.maxHealth = Mathf.Max(1f, result.maxHealth + bonus.maxHealth);
                result.maxMana = Mathf.Max(0f, result.maxMana + bonus.maxMana);
            }

            result.currentHealth = Mathf.Clamp(result.currentHealth <= 0f ? result.maxHealth : result.currentHealth, 0f, result.maxHealth);
            return result;
        }

        public static WeaponStats ApplyEnhancement(WeaponStats stats, int enhancementLevel)
        {
            float multiplier = EnhancementCostCalculator.GetStatMultiplier(enhancementLevel);
            stats.attackDamage *= multiplier;
            stats.attackSpeedBonus *= multiplier;
            stats.criticalChance *= multiplier;
            stats.criticalMultiplier *= multiplier;
            stats.armorPenetration *= multiplier;
            stats.maxHealth *= multiplier;
            stats.maxMana *= multiplier;
            return stats;
        }
    }
}
