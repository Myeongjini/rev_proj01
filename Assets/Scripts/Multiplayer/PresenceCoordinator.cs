using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WizardGrower.Core;
using WizardGrower.Stages;

namespace WizardGrower.Multiplayer
{
    public class PresenceCoordinator : MonoBehaviour
    {
        [SerializeField] private PresenceService presenceService;
        [SerializeField] private float writeIntervalSeconds = 0.2f;
        [SerializeField] private bool logWrites = true;
        [SerializeField] private bool subscribeToRemoteEvents = true;
        [SerializeField] private RemotePlayerView remoteWizardPrefab;
        [SerializeField] private Transform remoteRoot;
        [SerializeField] private long staleThresholdMs = 30000;

        private GameContext context;
        private string uid;
        private string currentStageKey;
        private IDisposable subscription;
        private readonly Dictionary<string, RemotePlayerView> remotes = new Dictionary<string, RemotePlayerView>();
        private Vector2 latestPosition;
        private float nextWriteTime;
        private bool beginComplete;
        private bool writeInFlight;
        private bool hasActiveStage;
        private string writeLogMessage;

        public event Action<RemotePresenceEvent> RemotePresenceChanged;

        public async void Begin(GameContext context, string uid, string displayName)
        {
            if (beginComplete || context == null || string.IsNullOrEmpty(uid))
                return;

            this.context = context;
            this.uid = uid;
            if (presenceService == null)
                presenceService = context.PresenceService != null ? context.PresenceService : GetComponent<PresenceService>();
            if (remoteWizardPrefab == null)
                remoteWizardPrefab = context.RemoteWizardPrefab;

            if (presenceService == null)
            {
                Debug.LogWarning("PresenceCoordinator requires PresenceService.");
                return;
            }

            try
            {
                await presenceService.InitializeAsync(uid, displayName);
                context.Movement.PositionChanged += OnPositionChanged;
                context.StageManager.StateChanged += OnStageChanged;
                latestPosition = context.Wizard != null ? (Vector2)context.Wizard.transform.position : Vector2.zero;
                SwitchStage(context.StageManager.CurrentChapter, context.StageManager.CurrentStage, context.StageManager.Mode);
                beginComplete = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("Presence initialization failed: {0}", ex.GetBaseException().Message));
            }
        }

        private void OnDestroy()
        {
            if (context != null)
            {
                if (context.Movement != null)
                    context.Movement.PositionChanged -= OnPositionChanged;
                if (context.StageManager != null)
                    context.StageManager.StateChanged -= OnStageChanged;
            }

            subscription?.Dispose();
            if (!string.IsNullOrEmpty(currentStageKey) && presenceService != null)
                _ = presenceService.RemoveOwnAsync(currentStageKey);
        }

        private void Update()
        {
            if (!beginComplete || !hasActiveStage || Time.unscaledTime < nextWriteTime)
            {
                CleanupStaleRemotes();
                return;
            }

            if (context != null && context.Wizard != null)
                latestPosition = context.Wizard.transform.position;

            _ = WriteLatestAsync();
            nextWriteTime = Time.unscaledTime + writeIntervalSeconds;
            CleanupStaleRemotes();
        }

        private void OnPositionChanged(Vector2 position)
        {
            latestPosition = position;
        }

        private void OnStageChanged(ChapterDefinition chapter, StageDefinition stage, StageMode mode)
        {
            SwitchStage(chapter, stage, mode);
        }

        private async void SwitchStage(ChapterDefinition chapter, StageDefinition stage, StageMode mode)
        {
            string previousStageKey = currentStageKey;
            string nextStageKey = mode == StageMode.Field && chapter != null && stage != null
                ? BuildStageKey(chapter.chapterNumber, stage.stageNumber)
                : string.Empty;

            if (previousStageKey == nextStageKey)
                return;

            subscription?.Dispose();
            subscription = null;
            ClearRemotes();
            currentStageKey = nextStageKey;
            hasActiveStage = !string.IsNullOrEmpty(currentStageKey);
            writeLogMessage = string.Empty;

            if (!string.IsNullOrEmpty(previousStageKey))
                await presenceService.RemoveOwnAsync(previousStageKey);

            if (!hasActiveStage)
            {
                Debug.Log("Presence cleared for solo boss room.");
                return;
            }

            if (subscribeToRemoteEvents)
                subscription = presenceService.SubscribeStage(currentStageKey, HandleRemotePresenceEvent);
            writeLogMessage = string.Format("Presence write presence/{0}/{1}", currentStageKey, uid);
            nextWriteTime = 0f;
            await WriteLatestAsync();
        }

        private async Task WriteLatestAsync()
        {
            if (writeInFlight || !hasActiveStage)
                return;

            writeInFlight = true;
            try
            {
                await presenceService.WriteOwnAsync(currentStageKey, latestPosition.x, latestPosition.y);
                if (logWrites)
                    Debug.Log(writeLogMessage);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.GetBaseException().Message);
            }
            finally
            {
                writeInFlight = false;
            }
        }

        private static string BuildStageKey(int chapterNumber, int stageNumber)
        {
            return string.Format("{0}_{1}", chapterNumber, stageNumber);
        }

        public void HandleRemotePresenceEvent(RemotePresenceEvent evt)
        {
            if (string.IsNullOrEmpty(evt.Uid))
                return;

            if (evt.Type == RemotePresenceEventType.Removed)
            {
                RemoveRemote(evt.Uid);
                RemotePresenceChanged?.Invoke(evt);
                return;
            }

            RemotePlayerView view;
            if (!remotes.TryGetValue(evt.Uid, out view) || view == null)
            {
                if (remoteWizardPrefab == null)
                {
                    Debug.LogWarning("RemoteWizardPrefab is not assigned.");
                    return;
                }

                Transform parent = remoteRoot != null ? remoteRoot : transform;
                view = Instantiate(remoteWizardPrefab, evt.Position, Quaternion.identity, parent);
                view.Initialize(evt.Uid, evt.DisplayName, evt.Position);
                remotes[evt.Uid] = view;
            }
            else
            {
                view.SetTarget(evt.Position);
            }

            view.Touch(evt.LastUpdateUnixMs);
            RemotePresenceChanged?.Invoke(evt);
        }

        private void CleanupStaleRemotes()
        {
            if (remotes.Count == 0)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            s_staleIds.Clear();
            foreach (KeyValuePair<string, RemotePlayerView> pair in remotes)
            {
                if (pair.Value == null || pair.Value.IsStale(now, staleThresholdMs))
                    s_staleIds.Add(pair.Key);
            }

            for (int i = 0; i < s_staleIds.Count; i++)
                RemoveRemote(s_staleIds[i]);
        }

        private void ClearRemotes()
        {
            s_staleIds.Clear();
            foreach (string remoteUid in remotes.Keys)
                s_staleIds.Add(remoteUid);

            for (int i = 0; i < s_staleIds.Count; i++)
                RemoveRemote(s_staleIds[i]);
        }

        private void RemoveRemote(string remoteUid)
        {
            RemotePlayerView view;
            if (!remotes.TryGetValue(remoteUid, out view))
                return;

            remotes.Remove(remoteUid);
            if (view != null)
                Destroy(view.gameObject);
        }

        private static readonly List<string> s_staleIds = new List<string>();
    }
}
