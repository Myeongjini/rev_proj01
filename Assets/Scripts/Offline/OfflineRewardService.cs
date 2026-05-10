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
    }

    public class OfflineRewardService : MonoBehaviour
    {
        private IOfflineTimeProvider time;
        private CurrencyWallet wallet;
        private StageManager stageManager;
        private PlayerWizard wizard;
        private SaveService save;
        private OfflineRewardSnapshot lastSnapshot;
        private bool hasSnapshot;

        public event Action<OfflineRewardSnapshot> PendingResolved;
        public event Action<long, bool> Claimed;

        public void Initialize(IOfflineTimeProvider time, CurrencyWallet wallet, StageManager stageMgr, PlayerWizard wizard, SaveService save)
        {
            this.time = time;
            this.wallet = wallet;
            stageManager = stageMgr;
            this.wizard = wizard;
            this.save = save;
            hasSnapshot = false;
        }

        public async Task<OfflineRewardSnapshot> ResolvePendingAsync()
        {
            if (time == null || save == null)
                return default;

            OfflineWindow window = await time.ResolveOfflineWindowAsync();
            long pendingGold = Math.Max(0, save.CurrentData.offlineRewardPending);
            if (pendingGold <= 0 && time.ShouldTriggerOfflineFlow(window))
            {
                pendingGold = OfflineRewardCalculator.CalculateGold(window, stageManager != null ? stageManager.CurrentChapter : null, stageManager != null ? stageManager.CurrentStage : null, wizard != null ? wizard.Stats : null);
                save.CurrentData.offlineRewardPending = pendingGold;
                save.Save();
            }

            lastSnapshot = new OfflineRewardSnapshot
            {
                elapsedSeconds = window.elapsedSeconds,
                isCapped = window.isCapped,
                baseGold = pendingGold,
                maxAdMultipliedGold = SafeMultiply(pendingGold, 2)
            };
            hasSnapshot = true;
            PendingResolved?.Invoke(lastSnapshot);
            return lastSnapshot;
        }

        public async Task ClaimAsync(bool watchedAd)
        {
            OfflineRewardSnapshot snapshot = hasSnapshot ? lastSnapshot : await ResolvePendingAsync();
            long baseGold = Math.Max(0, save != null ? save.CurrentData.offlineRewardPending : snapshot.baseGold);
            if (baseGold <= 0 || wallet == null || save == null)
                return;

            long totalGold = watchedAd ? SafeMultiply(baseGold, 2) : baseGold;
            wallet.AddGold(ToWalletAmount(totalGold));
            save.CurrentData.gold = wallet.Gold;
            save.CurrentData.offlineRewardPending = 0;

            OfflineWindow window = time != null ? await time.ResolveOfflineWindowAsync() : default;
            if (window.currentServerNowMs > 0)
                save.CurrentData.lastSeenAtUtcMs = window.currentServerNowMs;

            save.Save();
            hasSnapshot = false;
            Claimed?.Invoke(totalGold, watchedAd);
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
