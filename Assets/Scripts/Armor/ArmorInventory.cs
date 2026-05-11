using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizardGrower.Armor
{
    public class ArmorInventory : MonoBehaviour
    {
        [SerializeField] private ArmorDatabase database;

        private readonly List<OwnedArmorEntry> ownedArmors = new List<OwnedArmorEntry>();
        private readonly Dictionary<ArmorSlot, string> equippedBySlot = new Dictionary<ArmorSlot, string>();

        public ArmorDatabase Database => database;
        public IReadOnlyList<OwnedArmorEntry> OwnedArmors => ownedArmors;

        public event Action InventoryChanged;
        public event Action<ArmorDefinition> EquippedChanged;
        public event Action<ArmorDefinition, int> ArmorCountChanged;

        public void Initialize(ArmorDatabase db)
        {
            database = db != null ? db : database;
            RemoveUnknownEntries();
            InventoryChanged?.Invoke();
        }

        public int GetCount(string armorId)
        {
            OwnedArmorEntry entry = FindEntry(armorId);
            return entry != null ? Mathf.Max(0, entry.count) : 0;
        }

        public bool Has(string armorId)
        {
            return GetCount(armorId) > 0;
        }

        public string GetEquippedId(ArmorSlot slot)
        {
            return equippedBySlot.TryGetValue(slot, out string armorId) ? armorId : string.Empty;
        }

        public ArmorDefinition GetEquipped(ArmorSlot slot)
        {
            string armorId = GetEquippedId(slot);
            return !string.IsNullOrEmpty(armorId) && database != null ? database.GetById(armorId) : null;
        }

        public ArmorStats CaptureEquippedStats()
        {
            ArmorStats total = default;
            foreach (KeyValuePair<ArmorSlot, string> pair in equippedBySlot)
            {
                ArmorDefinition armor = database != null ? database.GetById(pair.Value) : null;
                if (armor != null)
                    total.Add(armor.statBonuses);
            }
            return total;
        }

        public void Add(string armorId, int count = 1)
        {
            ArmorDefinition armor = database != null ? database.GetById(armorId) : null;
            int amount = Mathf.Max(0, count);
            if (armor == null || amount <= 0)
                return;

            OwnedArmorEntry entry = FindEntry(armorId);
            if (entry == null)
            {
                entry = new OwnedArmorEntry(armorId, 0);
                ownedArmors.Add(entry);
            }

            entry.count += amount;
            ArmorCountChanged?.Invoke(armor, entry.count);
            InventoryChanged?.Invoke();
        }

        public bool TryConsume(string armorId, int count)
        {
            OwnedArmorEntry entry = FindEntry(armorId);
            int amount = Mathf.Max(1, count);
            if (entry == null || entry.count < amount)
                return false;

            entry.count -= amount;
            ArmorDefinition armor = database != null ? database.GetById(armorId) : null;
            ArmorCountChanged?.Invoke(armor, entry.count);
            if (entry.count <= 0)
                ownedArmors.Remove(entry);

            if (armor != null && GetEquippedId(armor.slot) == armorId && GetCount(armorId) <= 0)
                equippedBySlot.Remove(armor.slot);

            InventoryChanged?.Invoke();
            EquippedChanged?.Invoke(armor);
            return true;
        }

        public bool TryEquip(ArmorSlot slot, string armorId)
        {
            ArmorDefinition armor = database != null ? database.GetById(armorId) : null;
            if (armor == null || armor.slot != slot || !Has(armorId))
                return false;

            equippedBySlot[slot] = armorId;
            EquippedChanged?.Invoke(armor);
            InventoryChanged?.Invoke();
            return true;
        }

        public void LoadFromSave(List<OwnedArmorEntry> entries, List<EquippedArmorEntry> equipped)
        {
            ownedArmors.Clear();
            equippedBySlot.Clear();

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    OwnedArmorEntry entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.armorId) || entry.count <= 0)
                        continue;
                    if (database == null || database.GetById(entry.armorId) == null)
                        continue;

                    OwnedArmorEntry existing = FindEntry(entry.armorId);
                    if (existing == null)
                        ownedArmors.Add(new OwnedArmorEntry(entry.armorId, Mathf.Max(1, entry.count)));
                    else
                        existing.count += Mathf.Max(1, entry.count);
                }
            }

            if (equipped != null)
            {
                for (int i = 0; i < equipped.Count; i++)
                {
                    EquippedArmorEntry entry = equipped[i];
                    if (entry == null || string.IsNullOrEmpty(entry.armorId))
                        continue;
                    ArmorDefinition armor = database != null ? database.GetById(entry.armorId) : null;
                    if (armor != null && armor.slot == entry.slot && Has(entry.armorId))
                        equippedBySlot[entry.slot] = entry.armorId;
                }
            }

            InventoryChanged?.Invoke();
            EquippedChanged?.Invoke(null);
        }

        public IReadOnlyList<OwnedArmorEntry> CaptureForSave()
        {
            List<OwnedArmorEntry> copy = new List<OwnedArmorEntry>();
            for (int i = 0; i < ownedArmors.Count; i++)
            {
                OwnedArmorEntry entry = ownedArmors[i];
                if (entry != null && entry.count > 0)
                    copy.Add(new OwnedArmorEntry(entry.armorId, entry.count));
            }
            return copy;
        }

        public IReadOnlyList<EquippedArmorEntry> CaptureEquippedForSave()
        {
            List<EquippedArmorEntry> copy = new List<EquippedArmorEntry>();
            foreach (KeyValuePair<ArmorSlot, string> pair in equippedBySlot)
            {
                if (!string.IsNullOrEmpty(pair.Value))
                    copy.Add(new EquippedArmorEntry(pair.Key, pair.Value));
            }
            return copy;
        }

        private OwnedArmorEntry FindEntry(string armorId)
        {
            if (string.IsNullOrEmpty(armorId))
                return null;

            for (int i = 0; i < ownedArmors.Count; i++)
            {
                OwnedArmorEntry entry = ownedArmors[i];
                if (entry != null && entry.armorId == armorId)
                    return entry;
            }
            return null;
        }

        private void RemoveUnknownEntries()
        {
            for (int i = ownedArmors.Count - 1; i >= 0; i--)
            {
                OwnedArmorEntry entry = ownedArmors[i];
                if (entry == null || entry.count <= 0 || database == null || database.GetById(entry.armorId) == null)
                    ownedArmors.RemoveAt(i);
            }
        }
    }
}
