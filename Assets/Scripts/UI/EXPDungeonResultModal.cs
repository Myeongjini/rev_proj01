using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Ads;
using WizardGrower.Core;
using WizardGrower.Dungeons;
using WizardGrower.UI.Common;

namespace WizardGrower.UI
{
    public class EXPDungeonResultModal : MonoBehaviour, IGameStartupPopup, ICancelableStartupPopup
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button claimAdButton;
        [SerializeField] private Button closeButton;

        private EXPDungeonService service;
        private IRewardedAdProvider adProvider;
        private EXPDungeonResult currentResult;
        private TaskCompletionSource<bool> closeCompletion;
        private bool busy;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();
            Hide(false);
        }

        private void OnDestroy()
        {
            EXPDungeonSceneTransfer.PendingResultChanged -= OnPendingResultChanged;
        }

        public void Bind(EXPDungeonService service, IRewardedAdProvider adProvider)
        {
            this.service = service;
            this.adProvider = adProvider;
            EXPDungeonSceneTransfer.PendingResultChanged -= OnPendingResultChanged;
            EXPDungeonSceneTransfer.PendingResultChanged += OnPendingResultChanged;
        }

        public bool ShouldShow()
        {
            return EXPDungeonSceneTransfer.PendingResult.HasValue;
        }

        public async Task ShowAsync()
        {
            if (!EXPDungeonSceneTransfer.PendingResult.HasValue)
                return;

            closeCompletion = new TaskCompletionSource<bool>();
            Show(EXPDungeonSceneTransfer.PendingResult.Value);
            await closeCompletion.Task;
        }

        public void Show(EXPDungeonResult result)
        {
            ResolveReferences();
            currentResult = result;
            busy = false;
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            long best = service != null ? service.GetBestScore() : 0;
            bool newRecord = result.earnedExp > best;
            if (resultLabel != null)
                resultLabel.text = $"처치 {result.killCount} / 획득 EXP {result.earnedExp:N0}";
            if (bestLabel != null)
                bestLabel.text = newRecord ? $"Best EXP: {result.earnedExp:N0}  신기록!" : $"Best EXP: {best:N0}";
        }

        public void Hide(bool clearPending)
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
            if (clearPending)
                EXPDungeonSceneTransfer.Clear();
            closeCompletion?.TrySetResult(true);
            closeCompletion = null;
        }

        public void CancelStartupPopup()
        {
            Hide(false);
        }

        private async void Claim()
        {
            if (busy || service == null)
                return;
            busy = true;
            SetButtonsInteractable(false);
            long granted = await service.CompleteEntryAsync(currentResult, false);
            if (granted > 0)
            {
                Hide(true);
                return;
            }

            ServerStatusToast.Show(ServerStatusToast.RewardFailed);
            busy = false;
            SetButtonsInteractable(true);
        }

        private async void ClaimAd()
        {
            if (busy || service == null)
                return;
            busy = true;
            SetButtonsInteractable(false);
            bool watched = adProvider != null && await adProvider.WatchRewardedAdAsync();
            if (watched)
            {
                long granted = await service.CompleteEntryAsync(currentResult, true);
                if (granted > 0)
                {
                    Hide(true);
                    return;
                }

                ServerStatusToast.Show(ServerStatusToast.RewardFailed);
            }

            busy = false;
            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (claimButton != null)
                claimButton.interactable = interactable;
            if (claimAdButton != null)
                claimAdButton.interactable = interactable;
        }

        private void OnPendingResultChanged()
        {
            if (EXPDungeonSceneTransfer.PendingResult.HasValue)
                Show(EXPDungeonSceneTransfer.PendingResult.Value);
        }

        private void ResolveReferences()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            titleLabel = titleLabel != null ? titleLabel : FindText("Title");
            resultLabel = resultLabel != null ? resultLabel : FindText("Result");
            bestLabel = bestLabel != null ? bestLabel : FindText("Best");
            closeButton = closeButton != null ? closeButton : FindButton("CloseButton");
            claimButton = claimButton != null ? claimButton : FindButton("ClaimButton");
            claimAdButton = claimAdButton != null ? claimAdButton : FindButton("ClaimAdButton");
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => Hide(false));
            }
            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(Claim);
            }
            if (claimAdButton != null)
            {
                claimAdButton.onClick.RemoveAllListeners();
                claimAdButton.onClick.AddListener(ClaimAd);
            }
        }

        private TMP_Text FindText(string objectName)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == objectName)
                    return texts[i];
            }
            return null;
        }

        private Button FindButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == objectName)
                    return buttons[i];
            }
            return null;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
