using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Auth;

namespace WizardGrower.UI
{
    public class LoginPanel : MonoBehaviour
    {
        [SerializeField] private Button googleLoginButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private NicknameRegistrationPanel nicknamePanel;

        private AuthService authService;
        private UserProfileService profileService;

        private void Awake()
        {
            if (googleLoginButton != null)
                googleLoginButton.onClick.AddListener(LinkGoogle);
            if (skipButton != null)
                skipButton.onClick.AddListener(() => gameObject.SetActive(false));
            gameObject.SetActive(false);
        }

        public void Bind(AuthService authService, UserProfileService profileService)
        {
            this.authService = authService;
            this.profileService = profileService;
            if (authService != null)
            {
                authService.UserChanged += OnUserChanged;
                authService.ErrorRaised += SetStatus;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private async void LinkGoogle()
        {
            if (authService == null)
                return;

            SetStatus("Google 로그인 중...");
            bool linked = await authService.LinkWithGoogleAsync();
            if (!linked)
            {
                SetStatus(authService.LastError);
                return;
            }

            SetStatus("Google 로그인 완료");
            UserProfile profile = profileService != null
                ? await profileService.GetOrCreateAsync(authService.CurrentUid, AccountType.Google)
                : null;

            if (nicknamePanel != null && (profile == null || string.IsNullOrWhiteSpace(profile.DisplayName)))
            {
                nicknamePanel.Show(authService.CurrentUid, string.Empty, profileService, _ => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnUserChanged(string uid, AccountType type)
        {
            SetStatus(string.IsNullOrEmpty(uid) ? "로그인 안 됨" : $"{type} {uid}");
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message ?? string.Empty;
        }
    }
}
