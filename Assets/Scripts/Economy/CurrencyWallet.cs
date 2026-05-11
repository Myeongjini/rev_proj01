using System;
using Firebase.Firestore;
using UnityEngine;
using WizardGrower.Cloud;

namespace WizardGrower.Economy
{
    public class CurrencyWallet : MonoBehaviour
    {
        [SerializeField] private int gold;
        [SerializeField] private int gems = 300;

        private ICurrencyAuthority authority = new LocalCurrencyAuthority();
        private ListenerRegistration walletListener;

        public event Action<int> GoldChanged;
        public event Action<int> GemsChanged;
        public event Action<int> GoldGained;
        public int Gold => gold;
        public int Gems => gems;

        public void InitializeAuthority(CloudFunctionsClient client)
        {
            authority = client != null && client.IsReady
                ? new ServerCurrencyAuthority(client)
                : new LocalCurrencyAuthority();
        }

        public void AddGold(int amount, string reason = "reward", string source = "gameplay")
        {
            int gained = Mathf.Max(0, amount);
            if (gained <= 0)
                return;

            if (!TryGrant("gold", gained, reason, source, gold, SetGold))
                return;

            GoldGained?.Invoke(gained);
        }

        public bool TrySpendGold(int amount, string reason = "spend_gold")
        {
            int cost = Mathf.Max(0, amount);
            if (cost <= 0)
                return true;

            if (!TrySpend("gold", cost, reason, gold, SetGold))
                return false;

            return true;
        }

        public void SetGold(int amount)
        {
            gold = Mathf.Max(0, amount);
            GoldChanged?.Invoke(gold);
        }

        public void AddGems(int amount, string reason = "reward", string source = "gameplay")
        {
            int gained = Mathf.Max(0, amount);
            if (gained <= 0)
                return;

            TryGrant("gem", gained, reason, source, gems, SetGems);
        }

        public bool TrySpendGems(int amount, string reason = "spend_gem")
        {
            int cost = Mathf.Max(0, amount);
            if (cost <= 0)
                return true;

            if (!TrySpend("gem", cost, reason, gems, SetGems))
                return false;

            return true;
        }

        public void SetGems(int amount)
        {
            gems = Mathf.Max(0, amount);
            GemsChanged?.Invoke(gems);
        }

        public void StartServerWalletListener(string uid)
        {
            StopServerWalletListener();
            if (string.IsNullOrEmpty(uid))
                return;

            try
            {
                DocumentReference walletRef = FirebaseFirestore.DefaultInstance
                    .Collection("users")
                    .Document(uid)
                    .Collection("wallet")
                    .Document("main");
                walletListener = walletRef.Listen(snapshot =>
                {
                    if (snapshot == null || !snapshot.Exists)
                        return;

                    if (snapshot.TryGetValue("gold", out int serverGold))
                        SetGold(serverGold);
                    if (snapshot.TryGetValue("gem", out int serverGems))
                        SetGems(serverGems);
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Wallet listener failed to start: {ex.GetBaseException().Message}");
            }
        }

        public void StopServerWalletListener()
        {
            walletListener?.Stop();
            walletListener = null;
        }

        private bool TryGrant(string kind, int amount, string reason, string source, int currentBalance, Action<int> applyBalance)
        {
            if (authority == null || !authority.IsServerAuthoritative)
            {
                applyBalance(currentBalance + amount);
                return true;
            }

            try
            {
                CurrencyAuthorityResult result = authority.GrantAsync(kind, amount, reason, source).GetAwaiter().GetResult();
                if (!result.Success)
                    return false;

                applyBalance(result.BalanceAfter >= 0 ? result.BalanceAfter : currentBalance + amount);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Server grantCurrency path failed ({source}/{reason}): {ex.GetBaseException().Message}");
                return false;
            }
        }

        private bool TrySpend(string kind, int amount, string reason, int currentBalance, Action<int> applyBalance)
        {
            if (currentBalance < amount)
                return false;

            if (authority == null || !authority.IsServerAuthoritative)
            {
                applyBalance(currentBalance - amount);
                return true;
            }

            try
            {
                CurrencyAuthorityResult result = authority.SpendAsync(kind, amount, reason).GetAwaiter().GetResult();
                if (!result.Success)
                    return false;

                applyBalance(result.BalanceAfter >= 0 ? result.BalanceAfter : currentBalance - amount);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Server spendCurrency failed ({reason}): {ex.GetBaseException().Message}");
                return false;
            }
        }

        private void OnDestroy()
        {
            StopServerWalletListener();
        }
    }
}
