using UnityEngine;
using WizardGrower.Player;

namespace WizardGrower.Combat
{
    public class CombatCalculator
    {
        public const float AutoAttackCoefficient = 1f;
        public const float ManualAttackCoefficient = 2f;
        public const float SkillAttackCoefficient = 8f;

        private readonly PlayerStats stats;

        public CombatCalculator(PlayerStats stats)
        {
            this.stats = stats;
        }

        public DamageInfo Auto(GameObject source)
        {
            return Build(stats.AttackDamage * AutoAttackCoefficient, DamageType.Auto, source);
        }

        public DamageInfo Manual(GameObject source)
        {
            return Build(stats.AttackDamage * ManualAttackCoefficient, DamageType.Manual, source);
        }

        public DamageInfo Skill(GameObject source, float multiplier)
        {
            return Build(stats.AttackDamage * multiplier, DamageType.Skill, source);
        }

        private DamageInfo Build(float baseAmount, DamageType type, GameObject source)
        {
            bool critical = Random.value < stats.CriticalChance;
            float amount = critical ? baseAmount * stats.CriticalMultiplier : baseAmount;
            return new DamageInfo(amount, critical, type, source, stats.ArmorPenetration);
        }
    }
}
