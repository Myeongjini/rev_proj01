using UnityEngine;
using WizardGrower.Armor;
using WizardGrower.Weapons;

namespace WizardGrower.Drops
{
    [CreateAssetMenu(menuName = "Wizard Grower/Drops/Armor Drop Table")]
    public class ArmorDropTable : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.3f;
        [SerializeField] private float commonWeight = 70f;
        [SerializeField] private float normalWeight = 25f;
        [SerializeField] private float advancedWeight = 5f;

        public float DropChance => dropChance;

        public ArmorDefinition Roll(ArmorDatabase database)
        {
            if (database == null || Random.value > dropChance)
                return null;

            WeaponUpperGrade upper = RollUpperGrade();
            ArmorSlot slot = (ArmorSlot)Random.Range(0, 5);
            WeaponLowerGrade lower = (WeaponLowerGrade)Random.Range(0, 4);
            ArmorDefinition armor = database.GetBySlotAndGrade(slot, upper, lower);

            if (armor == null)
                armor = database.GetBySlotAndGrade(slot, WeaponUpperGrade.Common, lower);
            return armor;
        }

        private WeaponUpperGrade RollUpperGrade()
        {
            float total = Mathf.Max(0f, commonWeight) + Mathf.Max(0f, normalWeight) + Mathf.Max(0f, advancedWeight);
            if (total <= 0f)
                return WeaponUpperGrade.Common;

            float roll = Random.Range(0f, total);
            if (roll < commonWeight)
                return WeaponUpperGrade.Common;
            roll -= commonWeight;
            if (roll < normalWeight)
                return WeaponUpperGrade.Normal;
            return WeaponUpperGrade.Advanced;
        }
    }
}
