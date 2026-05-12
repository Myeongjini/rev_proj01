using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.UI.Common;

namespace WizardGrower.Upgrades
{
    public class UpgradeButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        [SerializeField] private float pendingTimeoutSeconds = 10f;

        private UpgradeSystem system;
        private UpgradeDefinition definition;
        private bool busy;
        private string failureMessage;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        public void Bind(UpgradeSystem system, UpgradeDefinition definition, Sprite iconSprite)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();

            this.system = system;
            this.definition = definition;
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
            else
                label.text = $"{definition.displayName}\nLv {system.GetLevel(definition)}  {system.GetCost(definition)}G";
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
            float elapsed = 0f;
            while (!purchaseTask.IsCompleted && elapsed < Mathf.Max(1f, pendingTimeoutSeconds))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            try
            {
                if (!purchaseTask.IsCompleted)
                {
                    failureMessage = "서버 지연";
                    ServerStatusToast.Show(ServerStatusToast.ServerDelayed);
                }
                else if (purchaseTask.IsFaulted)
                {
                    Exception error = purchaseTask.Exception != null ? purchaseTask.Exception.GetBaseException() : null;
                    if (error != null)
                        Debug.LogException(error);
                }
                else if (purchaseTask.Result)
                {
                    Refresh();
                }
                else
                {
                    failureMessage = "구매 실패";
                    ServerStatusToast.Show(ServerStatusToast.RewardFailed);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                failureMessage = "구매 실패";
                ServerStatusToast.Show(ServerStatusToast.RewardFailed);
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
