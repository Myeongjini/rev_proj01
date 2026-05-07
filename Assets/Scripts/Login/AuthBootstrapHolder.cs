using UnityEngine;
using WizardGrower.Auth;
using WizardGrower.Save;

namespace WizardGrower.Login
{
    public class AuthBootstrapHolder : MonoBehaviour
    {
        public static AuthBootstrapHolder Instance { get; private set; }

        public AuthService Auth { get; private set; }
        public UserProfileService Profile { get; private set; }
        public CloudSyncService CloudSync { get; private set; }
        public AuthConfig Config { get; private set; }
        public string Uid => Auth != null ? Auth.CurrentUid : string.Empty;

        public static AuthBootstrapHolder GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            GameObject holderObject = new GameObject("AuthBootstrapHolder");
            AuthBootstrapHolder holder = holderObject.AddComponent<AuthBootstrapHolder>();
            holder.EnsureServices();
            return holder;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureServices();
        }

        public void Configure(AuthConfig config)
        {
            Config = config;
            EnsureServices();
        }

        public void Bind(AuthService auth, UserProfileService profile, AuthConfig config)
        {
            Auth = auth;
            Profile = profile;
            Config = config;
            DontDestroyOnLoad(gameObject);
        }

        private void EnsureServices()
        {
            if (Auth == null)
                Auth = GetComponent<AuthService>() ?? gameObject.AddComponent<AuthService>();
            if (Profile == null)
                Profile = GetComponent<UserProfileService>() ?? gameObject.AddComponent<UserProfileService>();
            if (CloudSync == null)
                CloudSync = GetComponent<CloudSyncService>() ?? gameObject.AddComponent<CloudSyncService>();
        }
    }
}
