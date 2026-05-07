using System;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardGrower.Auth;
using WizardGrower.UI;

namespace WizardGrower.Login
{
    public class LoginBootstrap : MonoBehaviour
    {
        [SerializeField] private SplashView splash;
        [SerializeField] private LoginPanel loginPanel;
        [SerializeField] private NicknameRegistrationPanel nicknamePanel;
        [SerializeField] private AuthConfig authConfig;
        [SerializeField] private string mainSceneName = "MainScene";
        [SerializeField] private float minSplashSeconds = 0.8f;

        private AuthBootstrapHolder holder;
        private bool loadRequested;

        private async void Start()
        {
            if (loginPanel != null)
                loginPanel.gameObject.SetActive(false);
            if (nicknamePanel != null)
                nicknamePanel.gameObject.SetActive(false);

            await RunAsync();
        }

        private async Task RunAsync()
        {
            while (!loadRequested)
            {
                bool retry = await TryPrepareAuthenticationAsync();
                if (!retry)
                    break;
            }
        }

        private async Task<bool> TryPrepareAuthenticationAsync()
        {
            float startTime = Time.realtimeSinceStartup;
            if (splash != null)
            {
                splash.ShowImmediate();
                splash.HideRetry();
                splash.SetMessage("로그인 준비 중...");
            }

            try
            {
                holder = AuthBootstrapHolder.GetOrCreate();
                holder.Configure(authConfig);

                await holder.Auth.InitializeAsync(authConfig);
                bool hadExistingUser = FirebaseAuth.DefaultInstance.CurrentUser != null;
                string uid = await holder.Auth.SignInAnonymouslyAsync();
                holder.CloudSync.Initialize();
                await holder.Profile.GetOrCreateAsync(uid, holder.Auth.CurrentAccountType);
                holder.Bind(holder.Auth, holder.Profile, authConfig);

                await WaitForMinSplashAsync(startTime);
                if (splash != null)
                    await splash.FadeOutAsync(0.25f);

                if (hadExistingUser)
                {
                    Debug.Log($"LoginBootstrap reused Firebase UID: {uid}");
                    await LoadMainSceneAsync();
                    return false;
                }

                await ShowLoginPanelAndWaitAsync();
                await LoadMainSceneAsync();
                return false;
            }
            catch (Exception ex)
            {
                string message = ex.GetBaseException().Message;
                Debug.LogWarning($"Login bootstrap failed: {message}");
                if (splash != null)
                {
                    splash.SetMessage($"네트워크 오류: {message}");
                    return await splash.ShowRetryAsync();
                }

                return false;
            }
        }

        private async Task ShowLoginPanelAndWaitAsync()
        {
            if (loginPanel == null)
                return;

            loginPanel.Bind(holder.Auth, holder.Profile);
            loginPanel.Show();

            while (loginPanel != null && loginPanel.gameObject.activeSelf)
                await Task.Yield();
        }

        private async Task WaitForMinSplashAsync(float startTime)
        {
            while (Time.realtimeSinceStartup - startTime < minSplashSeconds)
                await Task.Yield();
        }

        private async Task LoadMainSceneAsync()
        {
            if (loadRequested)
                return;

            loadRequested = true;
            if (splash != null)
                splash.SetMessage("게임으로 이동 중...");

            AsyncOperation operation = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Single);
            while (operation != null && !operation.isDone)
                await Task.Yield();
        }
    }
}
