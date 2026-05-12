using UnityEngine;
using WizardGrower.Enhancement;
using WizardGrower.Save;
using WizardGrower.Weapons;

namespace WizardGrower.Armor
{
    public static class ArmorStatComposer
    {
        public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? weaponStats, ArmorStats armorStats)
        {
            return Recompute(baseSnapshot, weaponStats, 0, armorStats);
        }

        public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? weaponStats, int weaponEnhancementLevel, ArmorStats armorStats)
        {
            PlayerStatsSnapshot result = WeaponStatComposer.Recompute(baseSnapshot, weaponStats, weaponEnhancementLevel);
            result.attackDamage += armorStats.attackDamageBonus;
            result.autoAttackDamage = result.attackDamage;
            result.manualAttackDamage = result.attackDamage * 2f;
            result.criticalChance = Mathf.Clamp01(result.criticalChance + armorStats.criticalChance);
            result.criticalMultiplier = Mathf.Max(1f, result.criticalMultiplier + armorStats.criticalMultiplier);
            result.maxHealth = Mathf.Max(1f, result.maxHealth + armorStats.maxHealth);
            result.maxMana = Mathf.Max(0f, result.maxMana + armorStats.maxMana);
            result.defense = Mathf.Max(0f, result.defense + armorStats.defense);
            result.currentHealth = Mathf.Clamp(result.currentHealth <= 0f ? result.maxHealth : result.currentHealth, 0f, result.maxHealth);
            return result;
        }

        public static ArmorStats ApplyEnhancement(ArmorStats stats, int enhancementLevel)
        {
            float multiplier = EnhancementCostCalculator.GetStatMultiplier(enhancementLevel);
            stats.defense *= multiplier;
            stats.criticalChance *= multiplier;
            stats.criticalMultiplier *= multiplier;
            stats.maxHealth *= multiplier;
            stats.maxMana *= multiplier;
            stats.attackDamageBonus *= multiplier;
            return stats;
        }
    }
}
