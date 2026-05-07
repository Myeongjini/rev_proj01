using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Auth;

namespace WizardGrower.UI
{
    public class NicknameRegistrationPanel : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button submitButton;

        private string uid;
        private UserProfileService profileService;
        private Action<string> completed;

        private void Awake()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(Submit);
            gameObject.SetActive(false);
        }

        public void Show(string uid, string suggestedName, UserProfileService profileService, Action<string> completed = null)
        {
            this.uid = uid;
            this.profileService = profileService;
            this.completed = completed;
            if (inputField != null)
                inputField.text = suggestedName ?? string.Empty;
            SetMessage(string.Empty);
            gameObject.SetActive(true);
        }

        private async void Submit()
        {
            string nickname = inputField != null ? inputField.text.Trim() : string.Empty;
            if (nickname.Length < 1 || nickname.Length > 20)
            {
                SetMessage("닉네임은 1~20자로 입력하세요.");
                return;
            }

            try
            {
                if (profileService != null)
                    await profileService.UpdateDisplayNameAsync(uid, nickname);
                gameObject.SetActive(false);
                completed?.Invoke(nickname);
            }
            catch (Exception ex)
            {
                SetMessage(ex.Message);
            }
        }

        private void SetMessage(string message)
        {
            if (messageLabel != null)
                messageLabel.text = message;
        }
    }
}
