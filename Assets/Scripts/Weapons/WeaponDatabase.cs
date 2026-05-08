using System.Collections.Generic;
using UnityEngine;

namespace WizardGrower.Weapons
{
    [CreateAssetMenu(menuName = "Wizard Grower/Weapon Database")]
    public class WeaponDatabase : ScriptableObject
    {
        public WeaponDefinition[] weapons;

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
    }
}
