using UnityEngine;
using WizardGrower.Armor;
using WizardGrower.Enhancement;
using WizardGrower.Save;
using WizardGrower.Weapons;

namespace WizardGrower.Accessory
{
    public static class AccessoryStatComposer
    {
        public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? weaponStats, ArmorStats armorStats, AccessoryStats accessoryStats)
        {
            return Recompute(baseSnapshot, weaponStats, 0, armorStats, accessoryStats);
        }

        public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? weaponStats, int weaponEnhancementLevel, ArmorStats armorStats, AccessoryStats accessoryStats)
        {
            PlayerStatsSnapshot result = ArmorStatComposer.Recompute(baseSnapshot, weaponStats, weaponEnhancementLevel, armorStats);
            result.attackDamage += accessoryStats.attackDamage;
            result.autoAttackDamage = result.attackDamage;
            result.manualAttackDamage = result.attackDamage * 2f;
            result.criticalChance = Mathf.Clamp01(result.criticalChance + accessoryStats.criticalChance);
            result.criticalMultiplier = Mathf.Max(1f, result.criticalMultiplier + accessoryStats.criticalMultiplier);
            result.maxHealth = Mathf.Max(1f, result.maxHealth + accessoryStats.maxHealth);
            result.maxMana = Mathf.Max(0f, result.maxMana + accessoryStats.maxMana);
            result.armorPenetration = Mathf.Max(0f, result.armorPenetration + accessoryStats.armorPenetration);
            result.defense = Mathf.Max(0f, result.defense + accessoryStats.defense);
            result.currentHealth = Mathf.Clamp(result.currentHealth <= 0f ? result.maxHealth : result.currentHealth, 0f, result.maxHealth);
            return result;
        }

        public static AccessoryStats ApplyEnhancement(AccessoryStats stats, int enhancementLevel)
        {
            float multiplier = EnhancementCostCalculator.GetStatMultiplier(enhancementLevel);
            stats.attackDamage *= multiplier;
            stats.criticalChance *= multiplier;
            stats.criticalMultiplier *= multiplier;
            stats.maxHealth *= multiplier;
            stats.maxMana *= multiplier;
            stats.armorPenetration *= multiplier;
            stats.defense *= multiplier;
            return stats;
        }
    }
}
