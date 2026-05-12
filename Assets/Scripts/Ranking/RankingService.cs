using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using WizardGrower.Auth;
using WizardGrower.Player;

namespace WizardGrower.Ranking
{
    public class RankingService : MonoBehaviour
    {
        private const string CategoryDocumentId = "combatPower";
        private const string EntriesCollectionId = "entries";
        private const long DuplicatePushThrottleMs = 30000;

        private AuthService auth;
        private UserProfileService profile;
        private CombatPowerService combat;
        private FirebaseFirestore firestore;
        private long lastPushedScore = long.MinValue;
        private long lastPushUtcMs;
        private bool pushInFlight;
        private string cachedDisplayName;

        private FirebaseFirestore Firestore => firestore ?? (firestore = FirebaseFirestore.DefaultInstance);

        public event Action Refreshed;

        public void Initialize(AuthService authService, UserProfileService profileService, CombatPowerService combatPower)
        {
            if (combat != null)
                combat.PowerChanged -= OnPowerChanged;

            auth = authService;
            profile = profileService;
            combat = combatPower;

            if (combat != null)
                combat.PowerChanged += OnPowerChanged;
        }

        private void OnDestroy()
        {
            if (combat != null)
                combat.PowerChanged -= OnPowerChanged;
        }

        public Task PushMyCombatPowerScoreAsync()
        {
            return PushMyCombatPowerScoreAsync(null);
        }

        public async Task PushMyCombatPowerScoreAsync(string displayNameOverride)
        {
            if (pushInFlight || auth == null || combat == null || string.IsNullOrEmpty(auth.CurrentUid))
                return;

            long score = Math.Max(0L, (long)combat.CurrentPower);
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (lastPushedScore == score && nowMs - lastPushUtcMs < DuplicatePushThrottleMs)
                return;

            pushInFlight = true;
            try
            {
                string displayName = !string.IsNullOrWhiteSpace(displayNameOverride)
                    ? displayNameOverride.Trim()
                    : await ResolveDisplayNameAsync(auth.CurrentUid);

                await GetEntryDocument(auth.CurrentUid).SetAsync(new Dictionary<string, object>
                {
                    { "score", score },
                    { "displayName", displayName },
                    { "lastUpdateUtcMs", nowMs }
                }, SetOptions.MergeAll);

                lastPushedScore = score;
                lastPushUtcMs = nowMs;
                Refreshed?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ranking score push failed: {ex.GetBaseException().Message}");
            }
            finally
            {
                pushInFlight = false;
            }
        }

        public async Task<IReadOnlyList<RankingEntry>> GetTopCombatPowerAsync(int limit = 100)
        {
            int safeLimit = Mathf.Clamp(limit, 1, 100);
            QuerySnapshot snapshot = await GetEntriesCollection()
                .OrderByDescending("score")
                .OrderByDescending("lastUpdateUtcMs")
                .Limit(safeLimit)
                .GetSnapshotAsync();

            List<RankingEntry> entries = new List<RankingEntry>(snapshot.Count);
            int rank = 1;
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                entries.Add(ToRankingEntry(document, rank));
                rank++;
            }

            Refreshed?.Invoke();
            return entries;
        }

        public async Task<MyRankInfo> GetMyCombatPowerRankAsync()
        {
            if (auth == null || string.IsNullOrEmpty(auth.CurrentUid))
                return new MyRankInfo { myRank = 0, surrounding = Array.Empty<RankingEntry>() };

            DocumentSnapshot mine = await GetEntryDocument(auth.CurrentUid).GetSnapshotAsync();
            if (mine == null || !mine.Exists)
                return new MyRankInfo { myRank = 0, surrounding = Array.Empty<RankingEntry>() };

            long myScore = ReadLong(mine, "score");
            Query entries = GetEntriesCollection();

            QuerySnapshot aboveRankSnapshot = await entries
                .WhereGreaterThan("score", myScore)
                .GetSnapshotAsync();
            int myRank = aboveRankSnapshot.Count + 1;

            QuerySnapshot aboveSnapshot = await entries
                .WhereGreaterThan("score", myScore)
                .OrderBy("score")
                .OrderBy("lastUpdateUtcMs")
                .Limit(2)
                .GetSnapshotAsync();

            QuerySnapshot belowSnapshot = await entries
                .WhereLessThan("score", myScore)
                .OrderByDescending("score")
                .OrderByDescending("lastUpdateUtcMs")
                .Limit(2)
                .GetSnapshotAsync();

            List<DocumentSnapshot> above = new List<DocumentSnapshot>(aboveSnapshot.Documents);
            above.Reverse();

            List<RankingEntry> surrounding = new List<RankingEntry>(above.Count + belowSnapshot.Count + 1);
            for (int i = 0; i < above.Count; i++)
                surrounding.Add(ToRankingEntry(above[i], myRank - above.Count + i));

            surrounding.Add(ToRankingEntry(mine, myRank));

            int belowOffset = 1;
            foreach (DocumentSnapshot document in belowSnapshot.Documents)
            {
                surrounding.Add(ToRankingEntry(document, myRank + belowOffset));
                belowOffset++;
            }

            Refreshed?.Invoke();
            return new MyRankInfo { myRank = myRank, surrounding = surrounding.ToArray() };
        }

        private async void OnPowerChanged(float _)
        {
            await PushMyCombatPowerScoreAsync();
        }

        private async Task<string> ResolveDisplayNameAsync(string uid)
        {
            if (!string.IsNullOrWhiteSpace(cachedDisplayName))
                return cachedDisplayName;

            if (profile != null)
            {
                UserProfile userProfile = await profile.GetOrCreateAsync(uid, auth.CurrentAccountType);
                cachedDisplayName = SanitizeDisplayName(userProfile != null ? userProfile.DisplayName : string.Empty, uid);
                return cachedDisplayName;
            }

            cachedDisplayName = SanitizeDisplayName(string.Empty, uid);
            return cachedDisplayName;
        }

        private CollectionReference GetEntriesCollection()
        {
            return Firestore.Collection("rankings").Document(CategoryDocumentId).Collection(EntriesCollectionId);
        }

        private DocumentReference GetEntryDocument(string uid)
        {
            return GetEntriesCollection().Document(uid);
        }

        private static RankingEntry ToRankingEntry(DocumentSnapshot document, int rank)
        {
            return new RankingEntry
            {
                rank = rank,
                uid = document.Id,
                displayName = ReadString(document, "displayName"),
                score = ReadLong(document, "score"),
                lastUpdateUtcMs = ReadLong(document, "lastUpdateUtcMs")
            };
        }

        private static string SanitizeDisplayName(string displayName, string uid)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName.Trim().Length <= 30 ? displayName.Trim() : displayName.Trim().Substring(0, 30);

            string suffix = string.IsNullOrEmpty(uid) ? "000000" : uid.Substring(0, Mathf.Min(6, uid.Length));
            return $"Guest-{suffix}";
        }

        private static string ReadString(DocumentSnapshot document, string field)
        {
            return document.TryGetValue(field, out string value) ? value : string.Empty;
        }

        private static long ReadLong(DocumentSnapshot document, string field)
        {
            if (document.TryGetValue(field, out long longValue))
                return longValue;
            if (document.TryGetValue(field, out int intValue))
                return intValue;
            if (document.TryGetValue(field, out double doubleValue))
                return (long)doubleValue;
            return 0L;
        }
    }

    [Serializable]
    public struct RankingEntry
    {
        public int rank;
        public string uid;
        public string displayName;
        public long score;
        public long lastUpdateUtcMs;
    }

    [Serializable]
    public struct MyRankInfo
    {
        public int myRank;
        public RankingEntry[] surrounding;
    }
}
