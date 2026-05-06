using UnityEngine;

namespace WizardGrower.Combat
{
    public enum DamageType
    {
        Auto,
        Manual,
        Skill,
        BossAttack
    }

    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly bool IsCritical;
        public readonly DamageType Type;
        public readonly GameObject Source;
        public readonly float ArmorPenetration;

        public DamageInfo(float amount, bool isCritical, DamageType type, GameObject source, float armorPenetration = 0f)
        {
            Amount = amount;
            IsCritical = isCritical;
            Type = type;
            Source = source;
            ArmorPenetration = armorPenetration;
        }
    }
}
