using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WizardGrower.Weapons
{
    [CreateAssetMenu(menuName = "Wizard Grower/Weapon Database")]
    public class WeaponDatabase : ScriptableObject
    {
        public WeaponDefinition[] weapons;

        private List<WeaponDefinition> orderedWeapons;

        public IReadOnlyList<WeaponDefinition> OrderedWeapons
        {
            get
            {
                EnsureOrdered();
                return orderedWeapons;
            }
        }

        public WeaponDefinition GetById(string weaponId)
        {
            if (weapons == null || string.IsNullOrEmpty(weaponId))
                return null;

            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponDefinition weapon = weapons[i];
                if (weapon != null && weapon.weaponId == weaponId)
                    return weapon;
            }

            return null;
        }

        public IEnumerable<WeaponDefinition> ByRarity(Rarity rarity)
        {
            if (weapons == null)
                yield break;

            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponDefinition weapon = weapons[i];
                if (weapon != null && weapon.rarity == rarity)
                    yield return weapon;
            }
        }

        public WeaponDefinition GetByGrade(WeaponUpperGrade upper, WeaponLowerGrade lower)
        {
            EnsureOrdered();
            for (int i = 0; i < orderedWeapons.Count; i++)
            {
                WeaponDefinition weapon = orderedWeapons[i];
                if (weapon != null && weapon.upperGrade == upper && weapon.lowerGrade == lower)
                    return weapon;
            }
            return null;
        }

        public IReadOnlyList<WeaponDefinition> GetRow(WeaponUpperGrade upper)
        {
            EnsureOrdered();
            List<WeaponDefinition> row = new List<WeaponDefinition>();
            for (int i = 0; i < orderedWeapons.Count; i++)
            {
                WeaponDefinition weapon = orderedWeapons[i];
                if (weapon != null && weapon.upperGrade == upper)
                    row.Add(weapon);
            }
            return row;
        }

        public WeaponDefinition GetByLadderIndex(int ladderIndex)
        {
            EnsureOrdered();
            for (int i = 0; i < orderedWeapons.Count; i++)
            {
                WeaponDefinition weapon = orderedWeapons[i];
                if (weapon != null && weapon.ladderIndex == ladderIndex)
                    return weapon;
            }
            return null;
        }

        public WeaponDefinition GetNext(WeaponDefinition weapon)
        {
            if (weapon == null)
                return null;

            return GetByLadderIndex(weapon.ladderIndex + 1);
        }

        private void EnsureOrdered()
        {
            if (orderedWeapons != null)
                return;

            orderedWeapons = weapons == null
                ? new List<WeaponDefinition>()
                : weapons.Where(weapon => weapon != null)
                    .OrderBy(weapon => weapon.ladderIndex)
                    .ThenBy(weapon => weapon.upperGrade)
                    .ThenBy(weapon => weapon.lowerGrade)
                    .ToList();
        }

        private void OnValidate()
        {
            orderedWeapons = null;
        }
    }
}
