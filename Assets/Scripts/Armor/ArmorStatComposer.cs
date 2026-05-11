using UnityEngine;
using WizardGrower.Save;
using WizardGrower.Weapons;

namespace WizardGrower.Armor
{
    public static class ArmorStatComposer
    {
        public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? weaponStats, ArmorStats armorStats)
        {
            PlayerStatsSnapshot result = WeaponStatComposer.Recompute(baseSnapshot, weaponStats);
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
    }
}
