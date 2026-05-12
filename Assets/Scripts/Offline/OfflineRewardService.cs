using System;
using System.Threading.Tasks;
using UnityEngine;
using WizardGrower.Economy;
using WizardGrower.Player;
using WizardGrower.Save;
using WizardGrower.Stages;

namespace WizardGrower.Offline
{
    [Serializable]
    public struct OfflineRewardSnapshot
    {
        public long elapsedSeconds;
        public bool isCapped;
        public long baseGold;
        public long maxAdMultipliedGold;
        public long baseExp;
        public long maxAdMultipliedExp;
    }

    public class OfflineRewardService : MonoBehaviour
    {
        private IOfflineTimeProvider time;
        private CurrencyWallet wallet;
        private StageManager stageManager;
        private PlayerWizard wizard;
        private PlayerLevelService playerLevel;
        private SaveService save;
        private OfflineRewardSnapshot lastSnapshot;
        private bool hasSnapshot;

        public event Action<OfflineRewardSnapshot> PendingResolved;
        public event Action<long, long, bool> Claimed;

        public void Initialize(IOfflineTimeProvider time, CurrencyWallet wallet, StageManager stageMgr, PlayerWizard wizard, SaveService save, PlayerLevelService playerLevel = null)
        {
            this.time = time;
            this.wallet = wallet;
            stageManager = stageMgr;
            this.wizard = wizard;
            this.save = save;
            this.playerLevel = playerLevel;
            hasSnapshot = false;
        }

        public async Task<OfflineRewardSnapshot> ResolvePendingAsync()
        {
            if (time == null || save == null)
                return default;

            OfflineWindow window = await time.ResolveOfflineWindowAsync();
            long pendingGold = Math.Max(0, save.CurrentData.offlineRewardPending);
            long pendingExp = Math.Max(0, save.CurrentData.offlineRewardPendingExp);
            if ((pendingGold <= 0 || pendingExp <= 0) && time.ShouldTriggerOfflineFlow(window))
            {
                if (pendingGold <= 0)
                    pendingGold = OfflineRewardCalculator.CalculateGold(window, stageManager != null ? stageManager.CurrentChapter : null, stageManager != null ? stageManager.CurrentStage : null, wizard != null ? wizard.Stats : null);
                if (pendingExp <= 0)
                    pendingExp = OfflineRewardCalculator.CalculateExp(window, stageManager != null ? stageManager.CurrentChapter : null, stageManager != null ? stageManager.CurrentStage : null, wizard != null ? wizard.Stats : null);
                save.CurrentData.offlineRewardPending = pendingGold;
                save.CurrentData.offlineRewardPendingExp = pendingExp;
                save.Save();
            }

            lastSnapshot = new OfflineRewardSnapshot
            {
                elapsedSeconds = window.elapsedSeconds,
                isCapped = window.isCapped,
                baseGold = pendingGold,
                maxAdMultipliedGold = SafeMultiply(pendingGold, 2),
                baseExp = pendingExp,
                maxAdMultipliedExp = SafeMultiply(pendingExp, 2)
            };
            hasSnapshot = true;
            PendingResolved?.Invoke(lastSnapshot);
            return lastSnapshot;
        }

        public async Task ClaimAsync(bool watchedAd)
        {
            OfflineRewardSnapshot snapshot = hasSnapshot ? lastSnapshot : await ResolvePendingAsync();
            long baseGold = Math.Max(0, save != null ? save.CurrentData.offlineRewardPending : snapshot.baseGold);
            long baseExp = Math.Max(0, save != null ? save.CurrentData.offlineRewardPendingExp : snapshot.baseExp);
            if ((baseGold <= 0 && baseExp <= 0) || wallet == null || save == null)
                return;

            long totalGold = watchedAd ? SafeMultiply(baseGold, 2) : baseGold;
            long totalExp = watchedAd ? SafeMultiply(baseExp, 2) : baseExp;
            bool granted = await wallet.AddGoldAsync(ToWalletAmount(totalGold), "offline_reward", "offline");
            if (!granted)
                return;
            playerLevel?.GrantExp(ToWalletAmount(totalExp));
            save.CurrentData.gold = wallet.Gold;
            save.CurrentData.offlineRewardPending = 0;
            save.CurrentData.offlineRewardPendingExp = 0;

            OfflineWindow window = time != null ? await time.ResolveOfflineWindowAsync() : default;
            if (window.currentServerNowMs > 0)
                save.CurrentData.lastSeenAtUtcMs = window.currentServerNowMs;

            save.Save();
            hasSnapshot = false;
            Claimed?.Invoke(totalGold, totalExp, watchedAd);
        }

        private static int ToWalletAmount(long amount)
        {
            if (amount <= 0)
                return 0;
            return amount > int.MaxValue ? int.MaxValue : (int)amount;
        }

        private static long SafeMultiply(long value, long multiplier)
        {
            if (value <= 0 || multiplier <= 0)
                return 0;
            if (value > long.MaxValue / multiplier)
                return long.MaxValue;
            return value * multiplier;
        }
    }
}
