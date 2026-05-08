using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizardGrower.Weapons
{
    public class WeaponInventory : MonoBehaviour
    {
        private const string StarterWeaponId = "wand_starter";

        [SerializeField] private WeaponDatabase database;
        [SerializeField] private string equippedWeaponId = StarterWeaponId;

        private readonly List<string> ownedWeaponIds = new List<string>();

        public IReadOnlyCollection<string> OwnedWeaponIds => ownedWeaponIds;
        public string EquippedWeaponId => equippedWeaponId;
        public WeaponDefinition Equipped => database != null ? database.GetById(equippedWeaponId) : null;
        public WeaponDatabase Database => database;

        public event Action<WeaponDefinition> EquippedChanged;
        public event Action<WeaponDefinition> WeaponObtained;

        public void Initialize(WeaponDatabase db)
        {
            database = db != null ? db : database;
            if (!ownedWeaponIds.Contains(StarterWeaponId))
                ownedWeaponIds.Add(StarterWeaponId);
            if (string.IsNullOrEmpty(equippedWeaponId) || !ownedWeaponIds.Contains(equippedWeaponId))
                equippedWeaponId = StarterWeaponId;
        }

        public void Add(string weaponId)
        {
            WeaponDefinition weapon = database != null ? database.GetById(weaponId) : null;
            if (weapon == null || ownedWeaponIds.Contains(weaponId))
                return;

            ownedWeaponIds.Add(weaponId);
            WeaponObtained?.Invoke(weapon);
        }

        public bool TryEquip(string weaponId)
        {
            WeaponDefinition weapon = database != null ? database.GetById(weaponId) : null;
            if (weapon == null || !ownedWeaponIds.Contains(weaponId))
                return false;

            equippedWeaponId = weaponId;
            EquippedChanged?.Invoke(weapon);
            return true;
        }

        public void LoadFromSave(List<string> ownedIds, string equippedId)
        {
            ownedWeaponIds.Clear();
            if (ownedIds != null)
            {
                for (int i = 0; i < ownedIds.Count; i++)
                {
                    string id = ownedIds[i];
                    if (!string.IsNullOrEmpty(id) && database != null && database.GetById(id) != null && !ownedWeaponIds.Contains(id))
                        ownedWeaponIds.Add(id);
                }
            }

            if (!ownedWeaponIds.Contains(StarterWeaponId))
                ownedWeaponIds.Insert(0, StarterWeaponId);

            equippedWeaponId = !string.IsNullOrEmpty(equippedId) && ownedWeaponIds.Contains(equippedId) ? equippedId : StarterWeaponId;
            EquippedChanged?.Invoke(Equipped);
        }

        public (List<string> owned, string equipped) CaptureForSave()
        {
            return (new List<string>(ownedWeaponIds), equippedWeaponId);
        }

        public bool IsOwned(string weaponId)
        {
            return ownedWeaponIds.Contains(weaponId);
        }
    }
}
