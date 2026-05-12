using TMPro;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Economy;

namespace WizardGrower.UI
{
    public class DeveloperCurrencyPanel : MonoBehaviour
    {
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private Button addGoldButton;
        [SerializeField] private Button addGemsButton;
        [SerializeField] private TMP_Text goldButtonLabel;
        [SerializeField] private TMP_Text gemsButtonLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private int goldAmount = 10000;
        [SerializeField] private int gemsAmount = 3000;
        private bool busy;

        private void Awake()
        {
            ResolveWallet();
            WireButtons();
            RefreshLabels();
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            gameObject.SetActive(false);
#endif
        }

        private void OnDestroy()
        {
            if (addGoldButton != null)
                addGoldButton.onClick.RemoveListener(AddDebugGold);
            if (addGemsButton != null)
                addGemsButton.onClick.RemoveListener(AddDebugGems);
        }

        private void ResolveWallet()
        {
            if (wallet == null)
                wallet = FindAnyObjectByType<CurrencyWallet>(FindObjectsInactive.Include);
        }

        private void WireButtons()
        {
            if (addGoldButton != null)
            {
                addGoldButton.onClick.RemoveListener(AddDebugGold);
                addGoldButton.onClick.AddListener(AddDebugGold);
            }

            if (addGemsButton != null)
            {
                addGemsButton.onClick.RemoveListener(AddDebugGems);
                addGemsButton.onClick.AddListener(AddDebugGems);
            }
        }

        private void RefreshLabels()
        {
            if (goldButtonLabel != null)
                goldButtonLabel.text = $"+{goldAmount:N0} 골드";
            if (gemsButtonLabel != null)
                gemsButtonLabel.text = $"+{gemsAmount:N0} 젬";
            if (feedbackLabel != null)
                feedbackLabel.text = "DEV";
        }

        private void AddDebugGold()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ = AddDebugGoldAsync();
#endif
        }

        private void AddDebugGems()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ = AddDebugGemsAsync();
#endif
        }

        private async Task AddDebugGoldAsync()
        {
            ResolveWallet();
            if (!CanRequestServerGrant())
                return;

            await GrantAsync("gold", Mathf.Max(0, goldAmount));
        }

        private async Task AddDebugGemsAsync()
        {
            ResolveWallet();
            if (!CanRequestServerGrant())
                return;

            await GrantAsync("gem", Mathf.Max(0, gemsAmount));
        }

        private bool CanRequestServerGrant()
        {
            if (busy)
                return false;
            if (wallet == null)
            {
                SetFeedback("지갑 없음");
                return false;
            }
            if (!wallet.IsServerAuthoritative)
            {
                SetFeedback("서버 연결 필요");
                return false;
            }

            return true;
        }

        private async Task GrantAsync(string kind, int amount)
        {
            busy = true;
            SetButtonsInteractable(false);
            SetFeedback("서버 승인 중...");
            bool granted = kind == "gem"
                ? await wallet.AddGemsAsync(amount, "developer_grant", "developer")
                : await wallet.AddGoldAsync(amount, "developer_grant", "developer");
            if (granted)
                SetFeedback(kind == "gem" ? $"젬 {wallet.Gems:N0}" : $"골드 {wallet.Gold:N0}");
            else
                SetFeedback(string.IsNullOrEmpty(wallet.LastFailureMessage) ? "서버 승인 실패" : wallet.LastFailureMessage);

            busy = false;
            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (addGoldButton != null)
                addGoldButton.interactable = interactable;
            if (addGemsButton != null)
                addGemsButton.interactable = interactable;
        }

        private void SetFeedback(string message)
        {
            if (feedbackLabel != null)
                feedbackLabel.text = message;
        }
    }
}
