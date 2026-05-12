using System;
using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Enhancement;

namespace WizardGrower.Accessory
{
    public class AccessoryInventory : MonoBehaviour
    {
        [SerializeField] private AccessoryDatabase database;

        private readonly List<OwnedAccessoryEntry> ownedAccessories = new List<OwnedAccessoryEntry>();
        private readonly Dictionary<AccessorySlot, string> equippedBySlot = new Dictionary<AccessorySlot, string>();

        public AccessoryDatabase Database => database;
        public IReadOnlyList<OwnedAccessoryEntry> OwnedAccessories => ownedAccessories;

        public event Action InventoryChanged;
        public event Action<AccessoryDefinition> EquippedChanged;
        public event Action<AccessoryDefinition, int> AccessoryCountChanged;

        public void Initialize(AccessoryDatabase db)
        {
            database = db != null ? db : database;
            RemoveUnknownEntries();
            InventoryChanged?.Invoke();
        }

        public int GetCount(string accessoryId)
        {
            OwnedAccessoryEntry entry = FindEntry(accessoryId);
            return entry != null ? Mathf.Max(0, entry.count) : 0;
        }

        public bool Has(string accessoryId)
        {
            return GetCount(accessoryId) > 0;
        }

        public int GetEnhancementLevel(string accessoryId)
        {
            OwnedAccessoryEntry entry = FindEntry(accessoryId);
            return entry != null ? EnhancementCostCalculator.ClampLevel(entry.enhancementLevel) : 0;
        }

        public bool TrySetEnhancementLevel(string accessoryId, int level)
        {
            OwnedAccessoryEntry entry = FindEntry(accessoryId);
            if (entry == null || entry.count <= 0)
                return false;

            entry.enhancementLevel = EnhancementCostCalculator.ClampLevel(level);
            AccessoryDefinition accessory = database != null ? database.GetById(accessoryId) : null;
            AccessoryCountChanged?.Invoke(accessory, entry.count);
            if (accessory != null && GetEquippedId(accessory.slot) == accessoryId)
                EquippedChanged?.Invoke(accessory);
            InventoryChanged?.Invoke();
            return true;
        }

        public string GetEquippedId(AccessorySlot slot)
        {
            return equippedBySlot.TryGetValue(slot, out string accessoryId) ? accessoryId : string.Empty;
        }

        public AccessoryDefinition GetEquipped(AccessorySlot slot)
        {
            string accessoryId = GetEquippedId(slot);
            return !string.IsNullOrEmpty(accessoryId) && database != null ? database.GetById(accessoryId) : null;
        }

        public AccessoryStats CaptureEquippedStats()
        {
            AccessoryStats total = default;
            foreach (KeyValuePair<AccessorySlot, string> pair in equippedBySlot)
            {
                AccessoryDefinition accessory = database != null ? database.GetById(pair.Value) : null;
                if (accessory != null)
                    total.Add(AccessoryStatComposer.ApplyEnhancement(accessory.statBonuses, GetEnhancementLevel(pair.Value)));
            }
            return total;
        }

        public void Add(string accessoryId, int count = 1)
        {
            AccessoryDefinition accessory = database != null ? database.GetById(accessoryId) : null;
            int amount = Mathf.Max(0, count);
            if (accessory == null || amount <= 0)
                return;

            OwnedAccessoryEntry entry = FindEntry(accessoryId);
            if (entry == null)
            {
                entry = new OwnedAccessoryEntry(accessoryId, 0);
                ownedAccessories.Add(entry);
            }

            entry.count += amount;
            AccessoryCountChanged?.Invoke(accessory, entry.count);
            InventoryChanged?.Invoke();
        }

        public bool TryConsume(string accessoryId, int count)
        {
            OwnedAccessoryEntry entry = FindEntry(accessoryId);
            int amount = Mathf.Max(1, count);
            if (entry == null || entry.count < amount)
                return false;

            entry.count -= amount;
            AccessoryDefinition accessory = database != null ? database.GetById(accessoryId) : null;
            AccessoryCountChanged?.Invoke(accessory, entry.count);
            if (entry.count <= 0)
                ownedAccessories.Remove(entry);

            if (accessory != null && GetEquippedId(accessory.slot) == accessoryId && GetCount(accessoryId) <= 0)
                equippedBySlot.Remove(accessory.slot);

            InventoryChanged?.Invoke();
            EquippedChanged?.Invoke(accessory);
            return true;
        }

        public bool TryEquip(AccessorySlot slot, string accessoryId)
        {
            AccessoryDefinition accessory = database != null ? database.GetById(accessoryId) : null;
            if (accessory == null || accessory.slot != slot || !Has(accessoryId))
                return false;

            equippedBySlot[slot] = accessoryId;
            EquippedChanged?.Invoke(accessory);
            InventoryChanged?.Invoke();
            return true;
        }

        public void LoadFromSave(List<OwnedAccessoryEntry> entries, List<EquippedAccessoryEntry> equipped)
        {
            ownedAccessories.Clear();
            equippedBySlot.Clear();

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    OwnedAccessoryEntry entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.accessoryId) || entry.count <= 0)
                        continue;
                    if (database == null || database.GetById(entry.accessoryId) == null)
                        continue;

                    OwnedAccessoryEntry existing = FindEntry(entry.accessoryId);
                    if (existing == null)
                        ownedAccessories.Add(new OwnedAccessoryEntry(entry.accessoryId, Mathf.Max(1, entry.count), EnhancementCostCalculator.ClampLevel(entry.enhancementLevel)));
                    else
                    {
                        existing.count += Mathf.Max(1, entry.count);
                        existing.enhancementLevel = Mathf.Max(existing.enhancementLevel, EnhancementCostCalculator.ClampLevel(entry.enhancementLevel));
                    }
                }
            }

            if (equipped != null)
            {
                for (int i = 0; i < equipped.Count; i++)
                {
                    EquippedAccessoryEntry entry = equipped[i];
                    if (entry == null || string.IsNullOrEmpty(entry.accessoryId))
                        continue;
                    AccessoryDefinition accessory = database != null ? database.GetById(entry.accessoryId) : null;
                    if (accessory != null && accessory.slot == entry.slot && Has(entry.accessoryId))
                        equippedBySlot[entry.slot] = entry.accessoryId;
                }
            }

            InventoryChanged?.Invoke();
            EquippedChanged?.Invoke(null);
        }

        public IReadOnlyList<OwnedAccessoryEntry> CaptureForSave()
        {
            List<OwnedAccessoryEntry> copy = new List<OwnedAccessoryEntry>();
            for (int i = 0; i < ownedAccessories.Count; i++)
            {
                OwnedAccessoryEntry entry = ownedAccessories[i];
                if (entry != null && entry.count > 0)
                    copy.Add(new OwnedAccessoryEntry(entry.accessoryId, entry.count, EnhancementCostCalculator.ClampLevel(entry.enhancementLevel)));
            }
            return copy;
        }

        public IReadOnlyList<EquippedAccessoryEntry> CaptureEquippedForSave()
        {
            List<EquippedAccessoryEntry> copy = new List<EquippedAccessoryEntry>();
            foreach (KeyValuePair<AccessorySlot, string> pair in equippedBySlot)
            {
                if (!string.IsNullOrEmpty(pair.Value))
                    copy.Add(new EquippedAccessoryEntry(pair.Key, pair.Value));
            }
            return copy;
        }

        private OwnedAccessoryEntry FindEntry(string accessoryId)
        {
            if (string.IsNullOrEmpty(accessoryId))
                return null;

            for (int i = 0; i < ownedAccessories.Count; i++)
            {
                OwnedAccessoryEntry entry = ownedAccessories[i];
                if (entry != null && entry.accessoryId == accessoryId)
                    return entry;
            }
            return null;
        }

        private void RemoveUnknownEntries()
        {
            for (int i = ownedAccessories.Count - 1; i >= 0; i--)
            {
                OwnedAccessoryEntry entry = ownedAccessories[i];
                if (entry == null || entry.count <= 0 || database == null || database.GetById(entry.accessoryId) == null)
                    ownedAccessories.RemoveAt(i);
            }
        }
    }
}
