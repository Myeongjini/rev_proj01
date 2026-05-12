using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Economy;
using WizardGrower.UI.Common;

namespace WizardGrower.Upgrades
{
    public class UpgradeButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;

        private UpgradeSystem system;
        private UpgradeDefinition definition;
        private CurrencyWallet wallet;
        private bool busy;
        private string failureMessage;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        public void Bind(UpgradeSystem system, UpgradeDefinition definition, Sprite iconSprite, CurrencyWallet wallet = null)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();

            this.system = system;
            this.definition = definition;
            this.wallet = wallet;
            if (icon != null)
                icon.sprite = iconSprite;
            if (button != null)
                button.onClick.AddListener(OnClickBuy);
            Refresh();
        }

        public void Refresh()
        {
            if (label == null || system == null || definition == null)
                return;

            if (busy)
                label.text = $"{definition.displayName}\n처리 중...";
            else if (!string.IsNullOrEmpty(failureMessage))
                label.text = $"{definition.displayName}\n{failureMessage}";
            else if (!CanAfford())
                label.text = $"{definition.displayName}\n골드 부족";
            else
                label.text = $"{definition.displayName}\nLv {system.GetLevel(definition)}  {system.GetCost(definition)}G";

            if (button != null)
                button.interactable = !busy && CanAfford();
        }

        private bool CanAfford()
        {
            return system != null && definition != null && wallet != null && wallet.Gold >= system.GetCost(definition);
        }

        private void OnClickBuy()
        {
            if (busy || system == null || definition == null)
                return;

            StartCoroutine(PurchaseRoutine());
        }

        private IEnumerator PurchaseRoutine()
        {
            busy = true;
            failureMessage = string.Empty;
            if (button != null)
                button.interactable = false;
            Refresh();

            Task<bool> purchaseTask = system.TryPurchaseAsync(definition);
            while (!purchaseTask.IsCompleted)
                yield return null;

            try
            {
                if (purchaseTask.IsFaulted)
                {
                    Exception error = purchaseTask.Exception != null ? purchaseTask.Exception.GetBaseException() : null;
                    if (error != null)
                        Debug.LogException(error);
                    failureMessage = ServerStatusToast.ResolveRewardFailureMessage();
                    ServerStatusToast.Show(failureMessage);
                }
                else if (purchaseTask.Result)
                {
                    Refresh();
                }
                else
                {
                    failureMessage = ServerStatusToast.ResolveRewardFailureMessage();
                    ServerStatusToast.Show(failureMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                failureMessage = ServerStatusToast.ResolveRewardFailureMessage();
                ServerStatusToast.Show(failureMessage);
            }
            finally
            {
                busy = false;
                if (button != null && this != null)
                    button.interactable = true;
                Refresh();
            }
        }
    }
}
