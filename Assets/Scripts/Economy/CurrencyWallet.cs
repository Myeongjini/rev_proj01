using System;
using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Cloud;

namespace WizardGrower.Economy
{
    public class CurrencyWallet : MonoBehaviour
    {
        [SerializeField] private int gold;
        [SerializeField] private int gems = 300;

        private CloudFunctionsClient cloudFunctions;

        public event Action<int> GoldChanged;
        public event Action<int> GemsChanged;
        public event Action<int> GoldGained;
        public int Gold => gold;
        public int Gems => gems;

        public void InitializeAuthority(CloudFunctionsClient client)
        {
            cloudFunctions = client;
        }

        public void AddGold(int amount)
        {
            int gained = Mathf.Max(0, amount);
            gold += gained;
            GoldChanged?.Invoke(gold);
            if (gained > 0)
                GoldGained?.Invoke(gained);
        }

        public bool TrySpendGold(int amount)
        {
            if (gold < amount)
                return false;

            gold -= amount;
            GoldChanged?.Invoke(gold);
            QueueSpend("gold", amount, "local_spend");
            return true;
        }

        public void SetGold(int amount)
        {
            gold = Mathf.Max(0, amount);
            GoldChanged?.Invoke(gold);
        }

        public void AddGems(int amount)
        {
            gems += Mathf.Max(0, amount);
            GemsChanged?.Invoke(gems);
        }

        public bool TrySpendGems(int amount)
        {
            if (gems < amount)
                return false;

            gems -= amount;
            GemsChanged?.Invoke(gems);
            QueueSpend("gem", amount, "local_spend");
            return true;
        }

        public void SetGems(int amount)
        {
            gems = Mathf.Max(0, amount);
            GemsChanged?.Invoke(gems);
        }

        private async void QueueSpend(string kind, int amount, string reason)
        {
            if (cloudFunctions == null || !cloudFunctions.IsReady || amount <= 0)
                return;

            try
            {
                IDictionary<string, object> result = await cloudFunctions.CallAsync("spendCurrency", new Dictionary<string, object>
                {
                    { "kind", kind },
                    { "amount", amount },
                    { "reason", reason }
                });
                ApplyServerBalance(kind, result);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Server spendCurrency failed: {ex.GetBaseException().Message}");
            }
        }

        private void ApplyServerBalance(string kind, IDictionary<string, object> result)
        {
            if (result == null || !result.TryGetValue("balanceAfter", out object balance))
                return;

            int value = ConvertToInt(balance, kind == "gem" ? gems : gold);
            if (kind == "gem")
                SetGems(value);
            else
                SetGold(value);
        }

        private static int ConvertToInt(object value, int fallback)
        {
            if (value is int typed)
                return typed;
            if (value is long longValue)
                return (int)longValue;
            if (value is double doubleValue)
                return Mathf.RoundToInt((float)doubleValue);
            return value != null && int.TryParse(value.ToString(), out int parsed) ? parsed : fallback;
        }
    }
}
