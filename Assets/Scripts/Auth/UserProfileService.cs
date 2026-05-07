using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

namespace WizardGrower.Auth
{
    public class UserProfileService : MonoBehaviour
    {
        private FirebaseFirestore firestore;

        private FirebaseFirestore Firestore => firestore ?? (firestore = FirebaseFirestore.DefaultInstance);

        public async Task<UserProfile> GetOrCreateAsync(string uid, AccountType type)
        {
            if (string.IsNullOrEmpty(uid))
                throw new ArgumentException("UID is required.", nameof(uid));

            DocumentReference document = GetProfileDocument(uid);
            DocumentSnapshot snapshot = await document.GetSnapshotAsync();
            long now = NowMs();

            if (!snapshot.Exists)
            {
                UserProfile created = new UserProfile
                {
                    DisplayName = string.Empty,
                    AccountType = ToFirestoreValue(type),
                    CreatedAtUnixMs = now,
                    LastLoginAtUnixMs = now,
                    Locale = Application.systemLanguage.ToString()
                };
                await document.SetAsync(created);
                return created;
            }

            UserProfile profile = snapshot.ConvertTo<UserProfile>() ?? new UserProfile();
            profile.LastLoginAtUnixMs = now;
            await document.UpdateAsync(new Dictionary<string, object>
            {
                { "lastLoginAtUnixMs", now }
            });
            return profile;
        }

        public Task UpdateDisplayNameAsync(string uid, string displayName)
        {
            return GetProfileDocument(uid).UpdateAsync(new Dictionary<string, object>
            {
                { "displayName", displayName ?? string.Empty }
            });
        }

        public Task UpdateAccountTypeAsync(string uid, AccountType type)
        {
            return GetProfileDocument(uid).UpdateAsync(new Dictionary<string, object>
            {
                { "accountType", ToFirestoreValue(type) }
            });
        }

        public Task TouchLastLoginAsync(string uid)
        {
            return GetProfileDocument(uid).UpdateAsync(new Dictionary<string, object>
            {
                { "lastLoginAtUnixMs", NowMs() }
            });
        }

        private DocumentReference GetProfileDocument(string uid)
        {
            return Firestore.Collection("users").Document(uid).Collection("profile").Document("main");
        }

        public static string ToFirestoreValue(AccountType type)
        {
            return type == AccountType.Google ? "google" : "anonymous";
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
