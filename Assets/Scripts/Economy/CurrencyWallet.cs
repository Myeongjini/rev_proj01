using System;
using UnityEngine;

namespace WizardGrower.Economy
{
    public class CurrencyWallet : MonoBehaviour
    {
        [SerializeField] private int gold;

        public event Action<int> GoldChanged;
        public int Gold => gold;

        public void AddGold(int amount)
        {
            gold += Mathf.Max(0, amount);
            GoldChanged?.Invoke(gold);
        }

        public bool TrySpendGold(int amount)
        {
            if (gold < amount)
                return false;

            gold -= amount;
            GoldChanged?.Invoke(gold);
            return true;
        }
    }
}
