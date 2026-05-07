using System;
using System.Threading.Tasks;
using UnityEngine;
using WizardGrower.Auth;
using WizardGrower.Core;
using WizardGrower.UI;

namespace WizardGrower.Save
{
    public class SyncCoordinator : MonoBehaviour
    {
        [SerializeField] private float pushDebounceSeconds = 5f;

        private GameContext context;
        private string currentUid;
        private bool initialized;
        private bool pushQueued;
        private float nextPushTime;
        private bool pushInFlight;
        private bool resolveInFlight;
        private bool wasReachable;

        public async Task StartSyncAsync(string uid, GameContext context)
        {
            Initialize(context);
            await ResolveAndApply(uid);
        }

        public void Initialize(GameContext context)
        {
            if (initialized)
                return;

            this.context = context;
            if (context == null)
                return;

            context.CloudSyncService.Initialize();
            context.Wallet.GoldChanged += _ => QueuePush();
            context.UpgradeSystem.UpgradePurchased += (_, _, _) => QueuePush();
            context.StageManager.StateChanged += (_, _, _) => QueuePush();
            wasReachable = Application.internetReachability != NetworkReachability.NotReachable;
            initialized = true;
        }

        public async Task OnUidChanged(string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return;

            await ResolveAndApply(uid);
        }

        public void QueuePush()
        {
            if (string.IsNullOrEmpty(currentUid))
                return;

            pushQueued = true;
            nextPushTime = Time.unscaledTime + pushDebounceSeconds;
        }

        public async void FlushNow()
        {
            if (context == null || string.IsNullOrEmpty(currentUid))
                return;

            await PushCurrentAsync();
            await context.CloudSyncService.FlushPendingAsync();
        }

        private async Task ResolveAndApply(string uid)
        {
            if (resolveInFlight || context == null || string.IsNullOrEmpty(uid))
                return;

            resolveInFlight = true;
            try
            {
                string previousUid = currentUid;
                currentUid = uid;

                SaveData local = context.SaveService.CurrentData;
                SaveDataDocument remoteDocument = await context.CloudSyncService.PullDocumentAsync(uid);
                bool firstLoginConflict = IsFirstLoginConflict(previousUid, uid, local, remoteDocument);

                if (firstLoginConflict)
                {
                    await ResolveFirstLoginConflict(uid, local, remoteDocument);
                    return;
                }

                await context.CloudSyncService.ResolveAndApply(context.SaveService, uid);
                context.SaveBinder.SetUserId(uid);
                context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Cloud sync resolve failed: {ex.GetBaseException().Message}");
            }
            finally
            {
                resolveInFlight = false;
            }
        }

        private async Task ResolveFirstLoginConflict(string uid, SaveData local, SaveDataDocument remoteDocument)
        {
            SaveConflictPanel conflictPanel = FindAnyObjectByType<SaveConflictPanel>(FindObjectsInactive.Include);
            ConflictChoice choice = conflictPanel != null
                ? await conflictPanel.ShowAsync(local, remoteDocument)
                : ConflictChoice.UseRemote;

            if (choice == ConflictChoice.UseLocal)
            {
                local.userId = uid;
                context.SaveService.SetCurrentData(local);
                context.SaveService.Save();
                context.SaveBinder.SetUserId(uid);
                context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
                await context.CloudSyncService.PushAsync(context.SaveService.CurrentData);
            }
            else if (choice == ConflictChoice.UseRemote)
            {
                SaveData remote = SaveDataMapper.FromDocument(remoteDocument);
                context.SaveService.OverwriteFromServer(remote);
                context.SaveBinder.SetUserId(uid);
                context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
            }
            else
            {
                await context.AuthService.SignOutAsync();
                string anonymousUid = await context.AuthService.SignInAnonymouslyAsync();
                currentUid = anonymousUid;
                context.SaveBinder.SetUserId(anonymousUid);
            }
        }

        private bool IsFirstLoginConflict(string previousUid, string uid, SaveData local, SaveDataDocument remoteDocument)
        {
            if (remoteDocument == null || local == null)
                return false;

            if (previousUid == uid)
                return false;

            bool localBelongsElsewhere = string.IsNullOrEmpty(local.userId)
                || local.userId == "local"
                || local.userId != uid;
            return localBelongsElsewhere && HasMeaningfulProgress(local);
        }

        private static bool HasMeaningfulProgress(SaveData data)
        {
            return data.gold > 0
                || data.currentChapter > 1
                || data.currentStage > 1
                || (data.upgrades != null && data.upgrades.Count > 0);
        }

        private async Task PushCurrentAsync()
        {
            if (pushInFlight || context == null || string.IsNullOrEmpty(currentUid))
                return;

            pushInFlight = true;
            try
            {
                context.SaveBinder.SetUserId(currentUid);
                context.SaveBinder.SaveNow(context, context.SaveService);
                await context.CloudSyncService.PushAsync(context.SaveService.CurrentData);
                pushQueued = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Cloud sync push failed: {ex.GetBaseException().Message}");
                QueuePush();
            }
            finally
            {
                pushInFlight = false;
            }
        }

        private async void Update()
        {
            bool reachable = Application.internetReachability != NetworkReachability.NotReachable;
            if (reachable && !wasReachable)
                QueuePush();
            wasReachable = reachable;

            if (!pushQueued || Time.unscaledTime < nextPushTime)
                return;

            await PushCurrentAsync();
        }
    }
}
