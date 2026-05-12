using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WizardGrower.Accessory;
using WizardGrower.Armor;
using WizardGrower.Cloud;
using WizardGrower.Economy;
using WizardGrower.Save;
using WizardGrower.Weapons;

namespace WizardGrower.Enhancement
{
    public enum EnhancementSlotKind
    {
        Weapon,
        Armor,
        Accessory
    }

    public class EnhancementService : MonoBehaviour
    {
        private CurrencyWallet wallet;
        private WeaponInventory weaponInventory;
        private ArmorInventory armorInventory;
        private AccessoryInventory accessoryInventory;
        private SaveService save;
        private CloudFunctionsClient cloudFunctions;

        public event Action<EnhancementSlotKind, string, int> EnhancementChanged;

        public void Initialize(
            CurrencyWallet wallet,
            WeaponInventory weaponInventory,
            ArmorInventory armorInventory,
            AccessoryInventory accessoryInventory,
            SaveService save,
            CloudFunctionsClient cloudFunctions)
        {
            this.wallet = wallet;
            this.weaponInventory = weaponInventory;
            this.armorInventory = armorInventory;
            this.accessoryInventory = accessoryInventory;
            this.save = save;
            this.cloudFunctions = cloudFunctions;
        }

        public int GetEnhancementLevel(EnhancementSlotKind slotKind, string itemId)
        {
            return slotKind switch
            {
                EnhancementSlotKind.Weapon => weaponInventory != null ? weaponInventory.GetEnhancementLevel(itemId) : 0,
                EnhancementSlotKind.Armor => armorInventory != null ? armorInventory.GetEnhancementLevel(itemId) : 0,
                EnhancementSlotKind.Accessory => accessoryInventory != null ? accessoryInventory.GetEnhancementLevel(itemId) : 0,
                _ => 0
            };
        }

        public bool CanEnhance(EnhancementSlotKind slotKind, string itemId)
        {
            int level = GetEnhancementLevel(slotKind, itemId);
            int cost = EnhancementCostCalculator.GetCost(level);
            return HasOwnedItem(slotKind, itemId)
                && EnhancementCostCalculator.CanEnhance(level)
                && wallet != null
                && wallet.EnhancementStone >= cost;
        }

        public async Task<bool> TryEnhanceAsync(EnhancementSlotKind slotKind, string itemId, int currentLevel, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(itemId) || wallet == null || !HasOwnedItem(slotKind, itemId))
                return false;

            int localLevel = GetEnhancementLevel(slotKind, itemId);
            int level = EnhancementCostCalculator.ClampLevel(Mathf.Max(localLevel, currentLevel));
            if (!EnhancementCostCalculator.CanEnhance(level))
                return false;

            int cost = EnhancementCostCalculator.GetCost(level);
            if (wallet.EnhancementStone < cost)
                return false;

            int nextLevel = level + 1;
            int balanceAfter = -1;
            if (cloudFunctions != null && cloudFunctions.IsReady)
            {
                IDictionary<string, object> response = await cloudFunctions.CallAsync("enhanceItem", new Dictionary<string, object>
                {
                    { "slotKind", ToPayloadSlotKind(slotKind) },
                    { "itemId", itemId },
                    { "currentLevel", level }
                }, ct);
                nextLevel = ConvertToInt(response, "nextLevel", nextLevel);
                balanceAfter = ConvertToInt(response, "balanceAfter", -1);
            }
            else
            {
                if (!Application.isEditor)
                    return false;
                bool spent = await wallet.TrySpendEnhancementStoneAsync(cost, $"enhance_{ToPayloadSlotKind(slotKind)}_{itemId}_{level}", ct);
                if (!spent)
                    return false;
                balanceAfter = wallet.EnhancementStone;
            }

            nextLevel = EnhancementCostCalculator.ClampLevel(nextLevel);
            if (!ApplyLevel(slotKind, itemId, nextLevel))
                return false;
            if (balanceAfter >= 0)
                wallet.SetEnhancementStone(balanceAfter);

            save?.Save();
            EnhancementChanged?.Invoke(slotKind, itemId, nextLevel);
            return true;
        }

        private bool HasOwnedItem(EnhancementSlotKind slotKind, string itemId)
        {
            return slotKind switch
            {
                EnhancementSlotKind.Weapon => weaponInventory != null && weaponInventory.GetCount(itemId) > 0,
                EnhancementSlotKind.Armor => armorInventory != null && armorInventory.GetCount(itemId) > 0,
                EnhancementSlotKind.Accessory => accessoryInventory != null && accessoryInventory.GetCount(itemId) > 0,
                _ => false
            };
        }

        private bool ApplyLevel(EnhancementSlotKind slotKind, string itemId, int nextLevel)
        {
            return slotKind switch
            {
                EnhancementSlotKind.Weapon => weaponInventory != null && weaponInventory.TrySetEnhancementLevel(itemId, nextLevel),
                EnhancementSlotKind.Armor => armorInventory != null && armorInventory.TrySetEnhancementLevel(itemId, nextLevel),
                EnhancementSlotKind.Accessory => accessoryInventory != null && accessoryInventory.TrySetEnhancementLevel(itemId, nextLevel),
                _ => false
            };
        }

        private static string ToPayloadSlotKind(EnhancementSlotKind slotKind)
        {
            return slotKind switch
            {
                EnhancementSlotKind.Armor => "armor",
                EnhancementSlotKind.Accessory => "accessory",
                _ => "weapon"
            };
        }

        private static int ConvertToInt(IDictionary<string, object> response, string key, int fallback)
        {
            if (response == null || !response.TryGetValue(key, out object value) || value == null)
                return fallback;
            if (value is int typed)
                return typed;
            if (value is long longValue)
                return (int)longValue;
            if (value is double doubleValue)
                return Mathf.RoundToInt((float)doubleValue);
            return int.TryParse(value.ToString(), out int parsed) ? parsed : fallback;
        }
    }
}
