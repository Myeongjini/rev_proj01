using System;
using System.IO;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

namespace WizardGrower.Save
{
    public class CloudSyncService : MonoBehaviour
    {
        private FirebaseFirestore db;
        private bool initialized;

        public void Initialize()
        {
            if (initialized)
                return;

            db = FirebaseFirestore.DefaultInstance;
            try
            {
                db.Settings.PersistenceEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Firestore persistence setting was not applied: {ex.Message}");
            }
            initialized = true;
        }

        public async Task PushAsync(SaveData data)
        {
            Initialize();
            if (data == null || string.IsNullOrEmpty(data.userId) || data.userId == "local")
                throw new ArgumentException("Cloud save requires a resolved Firebase UID.", nameof(data));

            SaveDataDocument document = SaveDataMapper.ToDocument(data);
            if (document.UpdatedAtUnixMs <= 0)
                document.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await GetUserDocument(document.UserId).SetAsync(document, SetOptions.MergeAll);
        }

        public async Task<SaveData> PullAsync(string uid)
        {
            SaveDataDocument document = await PullDocumentAsync(uid);
            return SaveDataMapper.FromDocument(document);
        }

        public async Task<SaveDataDocument> PullDocumentAsync(string uid)
        {
            Initialize();
            if (string.IsNullOrEmpty(uid))
                throw new ArgumentException("UID is required.", nameof(uid));

            DocumentSnapshot snapshot = await GetUserDocument(uid).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<SaveDataDocument>() : null;
        }

        public async Task ResolveAndApply(SaveService localService, string uid)
        {
            if (localService == null || string.IsNullOrEmpty(uid))
                return;

            SaveData local = localService.CurrentData ?? new SaveData();
            SaveDataDocument remoteDocument = await PullDocumentAsync(uid);
            SaveData remote = SaveDataMapper.FromDocument(remoteDocument);
            bool hasLocalCache = File.Exists(localService.FilePath);

            if (remote != null && (!hasLocalCache || remote.updatedAtUnixMs > local.updatedAtUnixMs))
            {
                localService.OverwriteFromServer(remote);
                Debug.Log("RestoredFromServer");
                return;
            }

            local.userId = uid;
            localService.SetCurrentData(local);
            localService.Save();
            await PushAsync(localService.CurrentData);
        }

        public async Task ReconcileWalletAsync(string uid, SaveData data)
        {
            Initialize();
            if (string.IsNullOrEmpty(uid) || data == null)
                return;

            DocumentReference walletRef = GetUserDocument(uid).Collection("wallet").Document("main");
            DocumentSnapshot snapshot = await walletRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                if (snapshot.TryGetValue("gold", out int serverGold))
                    data.gold = Mathf.Max(0, serverGold);
                if (snapshot.TryGetValue("gem", out int serverGem))
                    data.gems = Mathf.Max(0, serverGem);
                return;
            }

            await walletRef.SetAsync(new
            {
                gold = Mathf.Max(0, data.gold),
                gem = Mathf.Max(0, data.gems),
                lastUpdatedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, SetOptions.MergeAll);
        }

        public Task FlushPendingAsync()
        {
            Initialize();
            return db.WaitForPendingWritesAsync();
        }

        private DocumentReference GetUserDocument(string uid)
        {
            return db.Collection("users").Document(uid);
        }
    }
}
