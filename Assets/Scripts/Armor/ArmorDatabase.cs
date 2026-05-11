using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WizardGrower.Weapons;

namespace WizardGrower.Armor
{
    [CreateAssetMenu(menuName = "Wizard Grower/Armor Database")]
    public class ArmorDatabase : ScriptableObject
    {
        public ArmorDefinition[] armors;

        private List<ArmorDefinition> orderedArmors;

        public IReadOnlyList<ArmorDefinition> OrderedArmors
        {
            get
            {
                EnsureOrdered();
                return orderedArmors;
            }
        }

        public ArmorDefinition GetById(string armorId)
        {
            if (armors == null || string.IsNullOrEmpty(armorId))
                return null;

            for (int i = 0; i < armors.Length; i++)
            {
                ArmorDefinition armor = armors[i];
                if (armor != null && armor.armorId == armorId)
                    return armor;
            }

            return null;
        }

        public ArmorDefinition GetBySlotAndGrade(ArmorSlot slot, WeaponUpperGrade upper, WeaponLowerGrade lower)
        {
            EnsureOrdered();
            for (int i = 0; i < orderedArmors.Count; i++)
            {
                ArmorDefinition armor = orderedArmors[i];
                if (armor != null && armor.slot == slot && armor.upperGrade == upper && armor.lowerGrade == lower)
                    return armor;
            }

            return null;
        }

        public IReadOnlyList<ArmorDefinition> GetRow(ArmorSlot slot, WeaponUpperGrade upper)
        {
            EnsureOrdered();
            List<ArmorDefinition> row = new List<ArmorDefinition>();
            for (int i = 0; i < orderedArmors.Count; i++)
            {
                ArmorDefinition armor = orderedArmors[i];
                if (armor != null && armor.slot == slot && armor.upperGrade == upper)
                    row.Add(armor);
            }

            return row;
        }

        public ArmorDefinition GetNext(ArmorDefinition armor)
        {
            if (armor == null)
                return null;

            int nextLower = (int)armor.lowerGrade + 1;
            if (nextLower > (int)WeaponLowerGrade.Supreme)
                return null;

            return GetBySlotAndGrade(armor.slot, armor.upperGrade, (WeaponLowerGrade)nextLower);
        }

        private void EnsureOrdered()
        {
            if (orderedArmors != null)
                return;

            orderedArmors = armors == null
                ? new List<ArmorDefinition>()
                : armors.Where(armor => armor != null)
                    .OrderBy(armor => armor.slot)
                    .ThenBy(armor => armor.upperGrade)
                    .ThenBy(armor => armor.lowerGrade)
                    .ThenBy(armor => armor.ladderIndex)
                    .ToList();
        }

        private void OnValidate()
        {
            orderedArmors = null;
        }
    }
}
