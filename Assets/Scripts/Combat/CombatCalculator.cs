using UnityEngine;
using WizardGrower.Player;

namespace WizardGrower.Combat
{
    public class CombatCalculator
    {
        private readonly PlayerStats stats;

        public CombatCalculator(PlayerStats stats)
        {
            this.stats = stats;
        }

        public DamageInfo Auto(GameObject source)
        {
            return Build(stats.AutoAttackDamage, DamageType.Auto, source);
        }

        public DamageInfo Manual(GameObject source)
        {
            return Build(stats.ManualAttackDamage, DamageType.Manual, source);
        }

        public DamageInfo Skill(GameObject source, float multiplier)
        {
            return Build(stats.AutoAttackDamage * multiplier, DamageType.Skill, source);
        }

        private DamageInfo Build(float baseAmount, DamageType type, GameObject source)
        {
            bool critical = Random.value < stats.CriticalChance;
            float amount = critical ? baseAmount * stats.CriticalMultiplier : baseAmount;
            return new DamageInfo(amount, critical, type, source, stats.ArmorPenetration);
        }
    }
}
