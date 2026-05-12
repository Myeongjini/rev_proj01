using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WizardGrower.Weapons;

namespace WizardGrower.Accessory
{
    [CreateAssetMenu(menuName = "Wizard Grower/Accessory Database")]
    public class AccessoryDatabase : ScriptableObject
    {
        public AccessoryDefinition[] accessories;

        private List<AccessoryDefinition> orderedAccessories;

        public IReadOnlyList<AccessoryDefinition> OrderedAccessories
        {
            get
            {
                EnsureOrdered();
                return orderedAccessories;
            }
        }

        public AccessoryDefinition GetById(string accessoryId)
        {
            if (accessories == null || string.IsNullOrEmpty(accessoryId))
                return null;

            for (int i = 0; i < accessories.Length; i++)
            {
                AccessoryDefinition accessory = accessories[i];
                if (accessory != null && accessory.accessoryId == accessoryId)
                    return accessory;
            }

            return null;
        }

        public AccessoryDefinition GetBySlotAndGrade(AccessorySlot slot, WeaponUpperGrade upper, WeaponLowerGrade lower)
        {
            EnsureOrdered();
            for (int i = 0; i < orderedAccessories.Count; i++)
            {
                AccessoryDefinition accessory = orderedAccessories[i];
                if (accessory != null && accessory.slot == slot && accessory.upperGrade == upper && accessory.lowerGrade == lower)
                    return accessory;
            }

            return null;
        }

        public IReadOnlyList<AccessoryDefinition> GetRow(AccessorySlot slot, WeaponUpperGrade upper)
        {
            EnsureOrdered();
            List<AccessoryDefinition> row = new List<AccessoryDefinition>();
            for (int i = 0; i < orderedAccessories.Count; i++)
            {
                AccessoryDefinition accessory = orderedAccessories[i];
                if (accessory != null && accessory.slot == slot && accessory.upperGrade == upper)
                    row.Add(accessory);
            }

            return row;
        }

        public AccessoryDefinition GetNext(AccessoryDefinition accessory)
        {
            if (accessory == null)
                return null;

            int nextLower = (int)accessory.lowerGrade + 1;
            if (nextLower > (int)WeaponLowerGrade.Supreme)
                return null;

            return GetBySlotAndGrade(accessory.slot, accessory.upperGrade, (WeaponLowerGrade)nextLower);
        }

        private void EnsureOrdered()
        {
            if (orderedAccessories != null)
                return;

            orderedAccessories = accessories == null
                ? new List<AccessoryDefinition>()
                : accessories.Where(accessory => accessory != null)
                    .OrderBy(accessory => accessory.slot)
                    .ThenBy(accessory => accessory.upperGrade)
                    .ThenBy(accessory => accessory.lowerGrade)
                    .ThenBy(accessory => accessory.ladderIndex)
                    .ToList();
        }

        private void OnValidate()
        {
            orderedAccessories = null;
        }
    }
}
