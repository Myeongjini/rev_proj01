using UnityEngine;

namespace WizardGrower.Data
{
    [CreateAssetMenu(menuName = "Wizard Grower/Player Stat Profile")]
    public class PlayerStatProfile : ScriptableObject
    {
        public float autoAttackDamage = 10f;
        public float manualAttackDamage = 20f;
        public float autoAttackInterval = 1f;
        public float manualAttackInterval = 0.3f;
        public float criticalChance = 0.1f;
        public float criticalMultiplier = 2f;
        public float armorPenetration = 0f;
        public float maxHealth = 100f;
        public float maxMana = 100f;
        public float manaRegen = 5f;
    }
}
