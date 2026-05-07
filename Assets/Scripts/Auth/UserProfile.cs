using Firebase.Firestore;

namespace WizardGrower.Auth
{
    [FirestoreData]
    public class UserProfile
    {
        [FirestoreProperty("displayName")] public string DisplayName { get; set; }
        [FirestoreProperty("accountType")] public string AccountType { get; set; }
        [FirestoreProperty("createdAtUnixMs")] public long CreatedAtUnixMs { get; set; }
        [FirestoreProperty("lastLoginAtUnixMs")] public long LastLoginAtUnixMs { get; set; }
        [FirestoreProperty("locale")] public string Locale { get; set; }
    }
}
