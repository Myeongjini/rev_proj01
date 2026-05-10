using System;
using System.Threading.Tasks;
using UnityEngine;
using WizardGrower.Missions;
using WizardGrower.Save;

namespace WizardGrower.Offline
{
    public interface IOfflineTimeProvider
    {
        Task<OfflineWindow> ResolveOfflineWindowAsync();
        bool ShouldTriggerOfflineFlow(OfflineWindow window);
    }

    [Serializable]
    public struct OfflineWindow
    {
        public long elapsedSeconds;
        public long lastSeenAtUtcMs;
        public long currentServerNowMs;
        public bool isCapped;
    }

    public class OfflineTimeTracker : MonoBehaviour, IOfflineTimeProvider
    {
        [SerializeField] private long capSeconds = 43200;
        [SerializeField] private long minTriggerSeconds = 30;

        private SaveService saveService;
        private SaveBinder saveBinder;
        private MissionResetService resetService;
        private bool initialized;

        public long CapSeconds => Math.Max(0, capSeconds);
        public long MinTriggerSeconds => Math.Max(0, minTriggerSeconds);

        public void Initialize(SaveService saveService, MissionResetService resetService)
        {
            Initialize(saveService, null, resetService);
        }

        public void Initialize(SaveService saveService, SaveBinder saveBinder, MissionResetService resetService)
        {
            this.saveService = saveService;
            this.saveBinder = saveBinder;
            this.resetService = resetService;
            initialized = this.saveService != null;

            if (initialized && this.saveService.CurrentData.lastSeenAtUtcMs <= 0)
                this.saveService.CurrentData.lastSeenAtUtcMs = GetServerNowMs();
        }

        public Task<OfflineWindow> ResolveOfflineWindowAsync()
        {
            OfflineWindow window = ResolveOfflineWindow();
            return Task.FromResult(window);
        }

        public bool ShouldTriggerOfflineFlow(OfflineWindow window)
        {
            return window.elapsedSeconds >= MinTriggerSeconds;
        }

        public void RecordLastSeenAndSave(WizardGrower.Core.GameContext context = null)
        {
            if (!initialized || saveService == null)
                return;

            saveService.CurrentData.lastSeenAtUtcMs = GetServerNowMs();

            if (context != null && saveBinder != null)
                saveBinder.SaveNow(context, saveService);
            else
                saveService.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                RecordLastSeenAndSave();
        }

        private void OnApplicationQuit()
        {
            RecordLastSeenAndSave();
        }

        private OfflineWindow ResolveOfflineWindow()
        {
            long nowMs = GetServerNowMs();
            long lastSeenMs = saveService != null ? saveService.CurrentData.lastSeenAtUtcMs : 0;
            if (lastSeenMs <= 0)
            {
                lastSeenMs = nowMs;
                if (saveService != null)
                    saveService.CurrentData.lastSeenAtUtcMs = nowMs;
            }

            long rawElapsedSeconds = (nowMs - lastSeenMs) / 1000L;
            if (rawElapsedSeconds < 0)
            {
                Debug.LogWarning($"Offline elapsed was negative. lastSeen={lastSeenMs}, now={nowMs}. Clamped to 0.");
                rawElapsedSeconds = 0;
            }

            long cappedElapsed = Math.Min(rawElapsedSeconds, CapSeconds);
            return new OfflineWindow
            {
                elapsedSeconds = cappedElapsed,
                lastSeenAtUtcMs = lastSeenMs,
                currentServerNowMs = nowMs,
                isCapped = rawElapsedSeconds > CapSeconds
            };
        }

        private long GetServerNowMs()
        {
            return resetService != null
                ? resetService.CurrentServerUtcMs
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
