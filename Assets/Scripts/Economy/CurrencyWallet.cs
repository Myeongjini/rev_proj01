using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Functions;
using Firebase.Firestore;
using UnityEngine;
using WizardGrower.Cloud;

namespace WizardGrower.Economy
{
    public class CurrencyWallet : MonoBehaviour
    {
        [SerializeField] private int gold;
        [SerializeField] private int gems = 300;
        [SerializeField] private int enhancementStone;

        private ICurrencyAuthority authority = new LocalCurrencyAuthority();
        private readonly SemaphoreSlim authorityLock = new SemaphoreSlim(1, 1);
        private ListenerRegistration walletListener;
        private bool authorityMutationInFlight;
        private string lastFailureMessage = string.Empty;
        private static string recentFailureMessage = string.Empty;
        private static float recentFailureTime;
        private const float RecentFailureSeconds = 4f;

        public event Action<int> GoldChanged;
        public event Action<int> GemsChanged;
        public event Action<int> EnhancementStoneChanged;
        public event Action<int> GoldGained;
        public int Gold => gold;
        public int Gems => gems;
        public int EnhancementStone => enhancementStone;
        public bool IsAuthorityBusy => authorityMutationInFlight || (authority != null && authority.IsBusy);
        public bool IsServerAuthoritative => authority != null && authority.IsServerAuthoritative;
        public string LastFailureMessage => lastFailureMessage;
        public static string RecentFailureMessage => !string.IsNullOrEmpty(recentFailureMessage) && Time.realtimeSinceStartup - recentFailureTime <= RecentFailureSeconds
            ? recentFailureMessage
            : string.Empty;

        public void InitializeAuthority(CloudFunctionsClient client)
        {
            authority = client != null && client.IsReady
                ? new ServerCurrencyAuthority(client)
                : new LocalCurrencyAuthority();
        }

        [Obsolete("Use AddGoldAsync — sync API is no-op in server-authority mode", false)]
        public void AddGold(int amount, string reason = "reward", string source = "gameplay")
        {
            int gained = Mathf.Max(0, amount);
            if (gained <= 0)
                return;

            if (!TryGrantLocalOnly("gold", gained, reason, source, gold, SetGold))
                return;

            GoldGained?.Invoke(gained);
        }

        public Task<bool> AddGoldAsync(int amount, string reason = "reward", string source = "gameplay", CancellationToken ct = default)
        {
            return TryGrantAsync("gold", amount, reason, source, Gold, SetGold, GoldGained, ct);
        }

        [Obsolete("Use TrySpendGoldAsync — sync API is no-op in server-authority mode", false)]
        public bool TrySpendGold(int amount, string reason = "spend_gold")
        {
            int cost = Mathf.Max(0, amount);
            if (cost <= 0)
                return true;

            if (!TrySpendLocalOnly("gold", cost, reason, gold, SetGold))
                return false;

            return true;
        }

        public Task<bool> TrySpendGoldAsync(int amount, string reason = "spend_gold", CancellationToken ct = default)
        {
            return TrySpendAsync("gold", amount, reason, Gold, SetGold, ct);
        }

        public void SetGold(int amount)
        {
            gold = Mathf.Max(0, amount);
            GoldChanged?.Invoke(gold);
        }

        [Obsolete("Use AddGemsAsync — sync API is no-op in server-authority mode", false)]
        public void AddGems(int amount, string reason = "reward", string source = "gameplay")
        {
            int gained = Mathf.Max(0, amount);
            if (gained <= 0)
                return;

            TryGrantLocalOnly("gem", gained, reason, source, gems, SetGems);
        }

        public Task<bool> AddGemsAsync(int amount, string reason = "reward", string source = "gameplay", CancellationToken ct = default)
        {
            return TryGrantAsync("gem", amount, reason, source, Gems, SetGems, null, ct);
        }

        [Obsolete("Use TrySpendGemsAsync — sync API is no-op in server-authority mode", false)]
        public bool TrySpendGems(int amount, string reason = "spend_gem")
        {
            int cost = Mathf.Max(0, amount);
            if (cost <= 0)
                return true;

            if (!TrySpendLocalOnly("gem", cost, reason, gems, SetGems))
                return false;

            return true;
        }

        public Task<bool> TrySpendGemsAsync(int amount, string reason = "spend_gem", CancellationToken ct = default)
        {
            return TrySpendAsync("gem", amount, reason, Gems, SetGems, ct);
        }

        public Task<bool> AddEnhancementStoneAsync(int amount, string reason = "reward", string source = "gameplay", CancellationToken ct = default)
        {
            return TryGrantAsync("enhancement_stone", amount, reason, source, EnhancementStone, SetEnhancementStone, null, ct);
        }

        public Task<bool> TrySpendEnhancementStoneAsync(int amount, string reason = "spend_enhancement_stone", CancellationToken ct = default)
        {
            return TrySpendAsync("enhancement_stone", amount, reason, EnhancementStone, SetEnhancementStone, ct);
        }

        public void SetEnhancementStone(int amount)
        {
            enhancementStone = Mathf.Max(0, amount);
            EnhancementStoneChanged?.Invoke(enhancementStone);
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

                    if (authorityMutationInFlight)
                        return;

                    if (snapshot.TryGetValue("gold", out int serverGold))
                        SetGold(serverGold);
                    if (snapshot.TryGetValue("gem", out int serverGems))
                        SetGems(serverGems);
                    if (snapshot.TryGetValue("enhancement_stone", out int serverEnhancementStone))
                        SetEnhancementStone(serverEnhancementStone);
                    else if (snapshot.TryGetValue("enhancementStone", out int serverEnhancementStoneCamel))
                        SetEnhancementStone(serverEnhancementStoneCamel);
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

        private bool TryGrantLocalOnly(string kind, int amount, string reason, string source, int currentBalance, Action<int> applyBalance)
        {
            if (authority == null || !authority.IsServerAuthoritative)
            {
                applyBalance(currentBalance + amount);
                return true;
            }

            Debug.LogWarning($"Use async API in server-authority mode: {kind} grant ({source}/{reason}).");
            return false;
        }

        private bool TrySpendLocalOnly(string kind, int amount, string reason, int currentBalance, Action<int> applyBalance)
        {
            if (currentBalance < amount)
                return false;

            if (authority == null || !authority.IsServerAuthoritative)
            {
                applyBalance(currentBalance - amount);
                return true;
            }

            Debug.LogWarning($"Use async API in server-authority mode: {kind} spend ({reason}).");
            return false;
        }

        private async Task<bool> TryGrantAsync(
            string kind,
            int amount,
            string reason,
            string source,
            int currentBalance,
            Action<int> applyBalance,
            Action<int> gainedEvent,
            CancellationToken ct)
        {
            int gained = Mathf.Max(0, amount);
            if (gained <= 0)
                return true;

            ClearFailure();
            if (authority == null || !authority.IsServerAuthoritative)
            {
                applyBalance(GetBalance(kind) + gained);
                gainedEvent?.Invoke(gained);
                return true;
            }

            await authorityLock.WaitAsync(ct);
            authorityMutationInFlight = true;
            try
            {
                int latestBalance = GetBalance(kind);
                CurrencyAuthorityResult result = await authority.GrantAsync(kind, gained, reason, source);
                if (!result.Success)
                {
                    SetFailure(string.IsNullOrEmpty(result.Message) ? "서버 보상 처리에 실패했습니다. 다시 시도해주세요." : result.Message);
                    return false;
                }

                applyBalance(result.BalanceAfter >= 0 ? result.BalanceAfter : latestBalance + gained);
                gainedEvent?.Invoke(gained);
                ClearFailure();
                return true;
            }
            catch (OperationCanceledException)
            {
                SetFailure("서버 연결이 지연되고 있습니다. 잠시 후 다시 시도해주세요.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Server grantCurrency path failed ({source}/{reason}): {ex.GetBaseException().Message}");
                SetFailure(ToServerFailureMessage(ex, "서버 보상 처리에 실패했습니다. 다시 시도해주세요."));
                return false;
            }
            finally
            {
                authorityMutationInFlight = false;
                authorityLock.Release();
            }
        }

        private async Task<bool> TrySpendAsync(
            string kind,
            int amount,
            string reason,
            int currentBalance,
            Action<int> applyBalance,
            CancellationToken ct)
        {
            int cost = Mathf.Max(0, amount);
            if (cost <= 0)
                return true;

            ClearFailure();
            if (authority == null || !authority.IsServerAuthoritative)
            {
                int localBalance = GetBalance(kind);
                if (localBalance < cost)
                {
                    SetFailure(ToInsufficientMessage(kind));
                    return false;
                }

                applyBalance(localBalance - cost);
                return true;
            }

            await authorityLock.WaitAsync(ct);
            authorityMutationInFlight = true;
            try
            {
                int latestBalance = GetBalance(kind);
                if (latestBalance < cost)
                {
                    SetFailure(ToInsufficientMessage(kind));
                    return false;
                }

                CurrencyAuthorityResult result = await authority.SpendAsync(kind, cost, reason);
                if (!result.Success)
                {
                    SetFailure(string.IsNullOrEmpty(result.Message) ? "서버 구매 처리에 실패했습니다. 다시 시도해주세요." : result.Message);
                    return false;
                }

                applyBalance(result.BalanceAfter >= 0 ? result.BalanceAfter : latestBalance - cost);
                ClearFailure();
                return true;
            }
            catch (OperationCanceledException)
            {
                SetFailure("서버 연결이 지연되고 있습니다. 잠시 후 다시 시도해주세요.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Server spendCurrency failed ({reason}): {ex.GetBaseException().Message}");
                SetFailure(ToServerFailureMessage(ex, "서버 구매 처리에 실패했습니다. 다시 시도해주세요.", kind));
                return false;
            }
            finally
            {
                authorityMutationInFlight = false;
                authorityLock.Release();
            }
        }

        private void OnDestroy()
        {
            StopServerWalletListener();
            authorityLock.Dispose();
        }

        private int GetBalance(string kind)
        {
            if (kind == "gold")
                return gold;
            if (kind == "gem")
                return gems;
            if (kind == "enhancement_stone")
                return enhancementStone;
            return 0;
        }

        private void SetFailure(string message)
        {
            lastFailureMessage = string.IsNullOrEmpty(message) ? "서버 처리에 실패했습니다. 다시 시도해주세요." : message;
            recentFailureMessage = lastFailureMessage;
            recentFailureTime = Time.realtimeSinceStartup;
        }

        private void ClearFailure()
        {
            lastFailureMessage = string.Empty;
            recentFailureMessage = string.Empty;
            recentFailureTime = 0f;
        }

        private static string ToServerFailureMessage(Exception ex, string fallback, string kind = "")
        {
            FunctionsException functionsException = ex.GetBaseException() as FunctionsException;
            if (functionsException == null)
                return fallback;

            if (functionsException.ErrorCode == FunctionsErrorCode.FailedPrecondition)
                return ToInsufficientMessage(kind);
            if (functionsException.ErrorCode == FunctionsErrorCode.DeadlineExceeded
                || functionsException.ErrorCode == FunctionsErrorCode.Unavailable
                || functionsException.ErrorCode == FunctionsErrorCode.Cancelled)
                return "서버 연결이 지연되고 있습니다. 잠시 후 다시 시도해주세요.";
            if (functionsException.ErrorCode == FunctionsErrorCode.Unauthenticated)
                return "로그인 세션이 필요합니다. LoginScene부터 다시 시작해주세요.";

            return fallback;
        }

        private static string ToInsufficientMessage(string kind)
        {
            if (kind == "gem")
                return "젬이 부족합니다.";
            if (kind == "enhancement_stone")
                return "강화석이 부족합니다.";
            return "골드가 부족합니다.";
        }
    }
}
