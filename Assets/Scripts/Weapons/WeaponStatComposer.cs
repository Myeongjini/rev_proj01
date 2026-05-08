using UnityEngine;
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
        /// | autoAttackDamage     | base.x  | + equipped.autoAttackDamage         | none  |
        /// | manualAttackDamage   | base.x  | + equipped.manualAttackDamage       | none  |
        /// | autoAttackInterval   | base.x  | - equipped.autoFireRateBonus        | >=0.05|
        /// | criticalChance       | base.x  | + equipped.criticalChance           | 0..1  |
        /// | criticalMultiplier   | base.x  | + equipped.criticalMultiplier       | >=1   |
        /// | armorPenetration     | base.x  | + equipped.armorPenetration         | >=0   |
        /// | maxHealth            | base.x  | + equipped.maxHealth                | >=1   |
        /// | currentHealth        | base.x  | unchanged                           | <=max |
        /// | manualAttackInterval | base.x  | unchanged                           | >=0.05|
        /// </summary>
        public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? equipped)
        {
            PlayerStatsSnapshot source = baseSnapshot ?? new PlayerStatsSnapshot();
            PlayerStatsSnapshot result = new PlayerStatsSnapshot
            {
                autoAttackDamage = source.autoAttackDamage,
                manualAttackDamage = source.manualAttackDamage,
                autoAttackInterval = Mathf.Max(0.05f, source.autoAttackInterval),
                manualAttackInterval = Mathf.Max(0.05f, source.manualAttackInterval),
                criticalChance = Mathf.Clamp01(source.criticalChance),
                criticalMultiplier = Mathf.Max(1f, source.criticalMultiplier),
                armorPenetration = Mathf.Max(0f, source.armorPenetration),
                maxHealth = Mathf.Max(1f, source.maxHealth),
                currentHealth = source.currentHealth
            };

            if (equipped.HasValue)
            {
                WeaponStats bonus = equipped.Value;
                result.autoAttackDamage += bonus.autoAttackDamage;
                result.manualAttackDamage += bonus.manualAttackDamage;
                result.autoAttackInterval = Mathf.Max(0.05f, result.autoAttackInterval - bonus.autoFireRateBonus);
                result.criticalChance = Mathf.Clamp01(result.criticalChance + bonus.criticalChance);
                result.criticalMultiplier = Mathf.Max(1f, result.criticalMultiplier + bonus.criticalMultiplier);
                result.armorPenetration = Mathf.Max(0f, result.armorPenetration + bonus.armorPenetration);
                result.maxHealth = Mathf.Max(1f, result.maxHealth + bonus.maxHealth);
            }

            result.currentHealth = Mathf.Clamp(result.currentHealth <= 0f ? result.maxHealth : result.currentHealth, 0f, result.maxHealth);
            return result;
        }
    }
}
