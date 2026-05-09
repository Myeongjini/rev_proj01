using System;
using UnityEngine;

namespace WizardGrower.Economy
{
    public class CurrencyWallet : MonoBehaviour
    {
        [SerializeField] private int gold;
        [SerializeField] private int gems = 300;

        public event Action<int> GoldChanged;
        public event Action<int> GemsChanged;
        public event Action<int> GoldGained;
        public int Gold => gold;
        public int Gems => gems;

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
            return true;
        }

        public void SetGems(int amount)
        {
            gems = Mathf.Max(0, amount);
            GemsChanged?.Invoke(gems);
        }
    }
}
