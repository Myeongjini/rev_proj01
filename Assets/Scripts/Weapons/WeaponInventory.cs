using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizardGrower.Weapons
{
    public class WeaponInventory : MonoBehaviour
    {
        public const string StarterWeaponId = "common_beginner_staff";

        [SerializeField] private WeaponDatabase database;
        [SerializeField] private string equippedWeaponId = StarterWeaponId;

        private readonly List<OwnedWeaponEntry> ownedWeapons = new List<OwnedWeaponEntry>();

        public IReadOnlyList<OwnedWeaponEntry> OwnedWeapons => ownedWeapons;
        public string EquippedWeaponId => equippedWeaponId;
        public WeaponDefinition Equipped => database != null ? database.GetById(equippedWeaponId) : null;
        public WeaponDatabase Database => database;

        public event Action InventoryChanged;
        public event Action<WeaponDefinition> EquippedChanged;
        public event Action<WeaponDefinition, int> WeaponCountChanged;

        public void Initialize(WeaponDatabase db)
        {
            database = db != null ? db : database;
            EnsureStarterFallback();
        }

        public int GetCount(string weaponId)
        {
            OwnedWeaponEntry entry = FindEntry(weaponId);
            return entry != null ? Mathf.Max(0, entry.count) : 0;
        }

        public bool Has(string weaponId)
        {
            return GetCount(weaponId) > 0;
        }

        public void Add(string weaponId, int count = 1)
        {
            WeaponDefinition weapon = database != null ? database.GetById(weaponId) : null;
            int amount = Mathf.Max(0, count);
            if (weapon == null || amount <= 0)
                return;

            OwnedWeaponEntry entry = FindEntry(weaponId);
            if (entry == null)
            {
                entry = new OwnedWeaponEntry(weaponId, 0);
                ownedWeapons.Add(entry);
            }
            entry.count += amount;
            WeaponCountChanged?.Invoke(weapon, entry.count);
            InventoryChanged?.Invoke();
        }

        public bool TryConsume(string weaponId, int count)
        {
            return TryConsume(weaponId, count, false);
        }

        public bool TryConsume(string weaponId, int count, bool deferEquipValidation)
        {
            OwnedWeaponEntry entry = FindEntry(weaponId);
            int amount = Mathf.Max(1, count);
            if (entry == null || entry.count < amount)
                return false;

            entry.count -= amount;
            WeaponDefinition weapon = database != null ? database.GetById(weaponId) : null;
            WeaponCountChanged?.Invoke(weapon, entry.count);
            if (entry.count <= 0)
                ownedWeapons.Remove(entry);

            if (!deferEquipValidation && equippedWeaponId == weaponId && GetCount(weaponId) <= 0)
                EquipBestOwnedOrStarter();

            InventoryChanged?.Invoke();
            return true;
        }

        public void EnsureEquippedValid()
        {
            if (!Has(equippedWeaponId))
                EquipBestOwnedOrStarter();
        }

        public bool TryEquip(string weaponId)
        {
            WeaponDefinition weapon = database != null ? database.GetById(weaponId) : null;
            if (weapon == null || !Has(weaponId))
                return false;

            equippedWeaponId = weaponId;
            EquippedChanged?.Invoke(weapon);
            InventoryChanged?.Invoke();
            return true;
        }

        public void LoadFromSave(List<OwnedWeaponEntry> entries, string equippedId)
        {
            ownedWeapons.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    OwnedWeaponEntry entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.weaponId) || entry.count <= 0)
                        continue;
                    if (database == null || database.GetById(entry.weaponId) == null)
                        continue;

                    OwnedWeaponEntry existing = FindEntry(entry.weaponId);
                    if (existing == null)
                        ownedWeapons.Add(new OwnedWeaponEntry(entry.weaponId, Mathf.Max(1, entry.count)));
                    else
                        existing.count += Mathf.Max(1, entry.count);
                }
            }

            EnsureStarterFallback();
            equippedWeaponId = !string.IsNullOrEmpty(equippedId) && Has(equippedId) ? equippedId : FindHighestOwnedId();
            if (string.IsNullOrEmpty(equippedWeaponId))
                equippedWeaponId = StarterWeaponId;

            EquippedChanged?.Invoke(Equipped);
            InventoryChanged?.Invoke();
        }

        public IReadOnlyList<OwnedWeaponEntry> CaptureForSave()
        {
            List<OwnedWeaponEntry> copy = new List<OwnedWeaponEntry>();
            for (int i = 0; i < ownedWeapons.Count; i++)
            {
                OwnedWeaponEntry entry = ownedWeapons[i];
                if (entry != null && entry.count > 0)
                    copy.Add(new OwnedWeaponEntry(entry.weaponId, entry.count));
            }
            return copy;
        }

        public bool IsOwned(string weaponId)
        {
            return Has(weaponId);
        }

        private OwnedWeaponEntry FindEntry(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return null;

            for (int i = 0; i < ownedWeapons.Count; i++)
            {
                OwnedWeaponEntry entry = ownedWeapons[i];
                if (entry != null && entry.weaponId == weaponId)
                    return entry;
            }
            return null;
        }

        private void EnsureStarterFallback()
        {
            bool anyValid = false;
            for (int i = ownedWeapons.Count - 1; i >= 0; i--)
            {
                OwnedWeaponEntry entry = ownedWeapons[i];
                if (entry == null || entry.count <= 0 || database == null || database.GetById(entry.weaponId) == null)
                {
                    ownedWeapons.RemoveAt(i);
                    continue;
                }
                anyValid = true;
            }

            if (!anyValid)
                ownedWeapons.Add(new OwnedWeaponEntry(StarterWeaponId, 1));

            if (!Has(equippedWeaponId))
                equippedWeaponId = FindHighestOwnedId();
            if (string.IsNullOrEmpty(equippedWeaponId))
                equippedWeaponId = StarterWeaponId;
        }

        private void EquipBestOwnedOrStarter()
        {
            string best = FindHighestOwnedId();
            if (string.IsNullOrEmpty(best))
            {
                Add(StarterWeaponId, 1);
                best = StarterWeaponId;
            }
            TryEquip(best);
        }

        private string FindHighestOwnedId()
        {
            string best = null;
            int bestIndex = int.MinValue;
            for (int i = 0; i < ownedWeapons.Count; i++)
            {
                OwnedWeaponEntry entry = ownedWeapons[i];
                if (entry == null || entry.count <= 0)
                    continue;
                WeaponDefinition weapon = database != null ? database.GetById(entry.weaponId) : null;
                if (weapon != null && weapon.ladderIndex > bestIndex)
                {
                    bestIndex = weapon.ladderIndex;
                    best = weapon.weaponId;
                }
            }
            return best;
        }
    }
}
