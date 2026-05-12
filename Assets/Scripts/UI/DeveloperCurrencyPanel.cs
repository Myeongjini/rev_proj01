using TMPro;
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
            ResolveWallet();
            if (wallet == null)
                return;

            wallet.SetGold(wallet.Gold + Mathf.Max(0, goldAmount));
            if (feedbackLabel != null)
                feedbackLabel.text = $"골드 {wallet.Gold:N0}";
#endif
        }

        private void AddDebugGems()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ResolveWallet();
            if (wallet == null)
                return;

            wallet.SetGems(wallet.Gems + Mathf.Max(0, gemsAmount));
            if (feedbackLabel != null)
                feedbackLabel.text = $"젬 {wallet.Gems:N0}";
#endif
        }
    }
}
