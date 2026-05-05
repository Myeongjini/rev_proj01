using UnityEngine;

namespace WizardGrower.Data
{
    [CreateAssetMenu(menuName = "Wizard Grower/Player Stat Profile")]
    public class PlayerStatProfile : ScriptableObject
    {
        public float baseAttack = 10f;
        public float autoAttackInterval = 1f;
        public float criticalChance = 0.1f;
        public float criticalMultiplier = 2f;
        public float maxMana = 100f;
        public float manaRegen = 5f;
    }
}
