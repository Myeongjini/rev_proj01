using System;
using System.Reflection;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

namespace WizardGrower.Auth
{
    public enum AccountType
    {
        Anonymous,
        Google,
        // Apple, // Reserved — not implemented this iteration. See Bundle 5 prework status.
    }

    public class AuthService : MonoBehaviour
    {
        private FirebaseAuth auth;
        private AuthConfig config;

        public string CurrentUid { get; private set; }
        public AccountType CurrentAccountType { get; private set; } = AccountType.Anonymous;
        public string LastError { get; private set; }

        public event Action<string, AccountType> UserChanged;
        public event Action<string> ErrorRaised;

        public async Task InitializeAsync(AuthConfig configOverride = null)
        {
            config = configOverride != null ? configOverride : Resources.Load<AuthConfig>("AuthConfig");
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new InvalidOperationException($"Firebase dependencies unavailable: {status}");

            if (config != null && !string.IsNullOrEmpty(config.bundleId) && config.bundleId != Application.identifier)
                Debug.LogWarning($"AuthConfig bundleId '{config.bundleId}' differs from Application.identifier '{Application.identifier}'.");

            auth = FirebaseAuth.DefaultInstance;
            ApplyCurrentUser(auth.CurrentUser);
        }

        public async Task<string> SignInAnonymouslyAsync()
        {
            EnsureInitialized();

            if (auth.CurrentUser != null)
            {
                ApplyCurrentUser(auth.CurrentUser);
                return CurrentUid;
            }

            AuthResult result = await auth.SignInAnonymouslyAsync();
            ApplyCurrentUser(result.User);
            return CurrentUid;
        }

        public async Task<bool> LinkWithGoogleAsync()
        {
            EnsureInitialized();
            try
            {
                string idToken = await RequestGoogleIdTokenAsync();
                if (string.IsNullOrEmpty(idToken))
                    throw new InvalidOperationException("Google sign-in did not return an ID token.");

                Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
                FirebaseUser resultUser;
                if (auth.CurrentUser == null)
                    resultUser = await auth.SignInWithCredentialAsync(credential);
                else if (auth.CurrentUser.IsAnonymous)
                {
                    AuthResult result = await auth.CurrentUser.LinkWithCredentialAsync(credential);
                    resultUser = result.User;
                }
                else
                    resultUser = await auth.SignInWithCredentialAsync(credential);

                CurrentAccountType = AccountType.Google;
                ApplyCurrentUser(resultUser, AccountType.Google);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.GetBaseException().Message;
                ErrorRaised?.Invoke(LastError);
                Debug.LogError($"Google login failed: {LastError}");
                return false;
            }
        }

        public Task SignOutAsync()
        {
            EnsureInitialized();
            auth.SignOut();
            CurrentUid = string.Empty;
            CurrentAccountType = AccountType.Anonymous;
            UserChanged?.Invoke(CurrentUid, CurrentAccountType);
            return Task.CompletedTask;
        }

        private async Task<string> RequestGoogleIdTokenAsync()
        {
            if (config == null || string.IsNullOrEmpty(config.googleWebClientId))
                throw new InvalidOperationException("AuthConfig.googleWebClientId is missing.");

            if (Application.isEditor)
                throw new InvalidOperationException("Google Sign-In account picker requires an Android or iOS player runtime; Unity Editor has no native currentActivity.");

            Type configType = FindType("Google.GoogleSignInConfiguration");
            Type signInType = FindType("Google.GoogleSignIn");
            if (configType == null || signInType == null)
                throw new InvalidOperationException("Google Sign-In Unity Plugin is not installed. Import it to enable Google login.");

            object googleConfig = Activator.CreateInstance(configType);
            SetMember(configType, googleConfig, "WebClientId", config.googleWebClientId);
            SetMember(configType, googleConfig, "RequestIdToken", true);
            SetMember(configType, googleConfig, "RequestEmail", true);

            PropertyInfo configuration = signInType.GetProperty("Configuration", BindingFlags.Public | BindingFlags.Static);
            PropertyInfo defaultInstance = signInType.GetProperty("DefaultInstance", BindingFlags.Public | BindingFlags.Static);
            MethodInfo signIn = signInType.GetMethod("SignIn", BindingFlags.Public | BindingFlags.Instance);
            if (configuration == null || defaultInstance == null || signIn == null)
                throw new InvalidOperationException("Google Sign-In Unity Plugin API shape is not recognized.");

            configuration.SetValue(null, googleConfig);
            object instance = defaultInstance.GetValue(null);
            Task signInTask = signIn.Invoke(instance, null) as Task;
            if (signInTask == null)
                throw new InvalidOperationException("Google Sign-In did not return a Task.");

            await signInTask;
            object googleUser = signInTask.GetType().GetProperty("Result")?.GetValue(signInTask);
            string idToken = googleUser?.GetType().GetProperty("IdToken")?.GetValue(googleUser) as string;
            return idToken;
        }

        private void ApplyCurrentUser(FirebaseUser user, AccountType? explicitType = null)
        {
            if (user == null)
                return;

            CurrentUid = user.UserId;
            CurrentAccountType = explicitType ?? (user.IsAnonymous ? AccountType.Anonymous : AccountType.Google);
            UserChanged?.Invoke(CurrentUid, CurrentAccountType);
        }

        private void EnsureInitialized()
        {
            if (auth == null)
                throw new InvalidOperationException("AuthService is not initialized.");
        }

        private static void SetMember(Type type, object target, string memberName, object value)
        {
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                property.SetValue(target, value);
                return;
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            throw new InvalidOperationException($"Google Sign-In config member not found: {memberName}");
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
