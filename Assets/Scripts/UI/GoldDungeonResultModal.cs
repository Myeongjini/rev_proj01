using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Ads;
using WizardGrower.Core;
using WizardGrower.Dungeons;

namespace WizardGrower.UI
{
    public class GoldDungeonResultModal : MonoBehaviour, IGameStartupPopup
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button claimAdButton;
        [SerializeField] private Button closeButton;

        private GoldDungeonService service;
        private IRewardedAdProvider adProvider;
        private GoldDungeonResult currentResult;
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
            GoldDungeonSceneTransfer.PendingResultChanged -= OnPendingResultChanged;
        }

        public void Bind(GoldDungeonService service, IRewardedAdProvider adProvider)
        {
            this.service = service;
            this.adProvider = adProvider;
            GoldDungeonSceneTransfer.PendingResultChanged -= OnPendingResultChanged;
            GoldDungeonSceneTransfer.PendingResultChanged += OnPendingResultChanged;
        }

        public bool ShouldShow()
        {
            return GoldDungeonSceneTransfer.PendingResult.HasValue;
        }

        public async Task ShowAsync()
        {
            if (!GoldDungeonSceneTransfer.PendingResult.HasValue)
                return;

            closeCompletion = new TaskCompletionSource<bool>();
            Show(GoldDungeonSceneTransfer.PendingResult.Value);
            await closeCompletion.Task;
        }

        public void Show(GoldDungeonResult result)
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
            bool newRecord = result.earnedGold > best;
            if (resultLabel != null)
                resultLabel.text = $"처치 {result.killCount} / 획득 골드 {result.earnedGold:N0}";
            if (bestLabel != null)
                bestLabel.text = newRecord ? $"Best Record: {result.earnedGold:N0}  🏆 신기록!" : $"Best Record: {best:N0}";
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
                GoldDungeonSceneTransfer.Clear();
            closeCompletion?.TrySetResult(true);
            closeCompletion = null;
        }

        private async void Claim()
        {
            if (busy || service == null)
                return;
            busy = true;
            SetButtonsInteractable(false);
            await service.CompleteEntryAsync(currentResult, false);
            Hide(true);
        }

        private async void ClaimAd()
        {
            if (busy || service == null)
                return;
            busy = true;
            SetButtonsInteractable(false);
            bool watched = adProvider != null && await adProvider.WatchRewardedAdAsync();
            if (watched)
                await service.CompleteEntryAsync(currentResult, true);
            busy = false;
            SetButtonsInteractable(true);
            if (watched)
                Hide(true);
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
            if (GoldDungeonSceneTransfer.PendingResult.HasValue)
                Show(GoldDungeonSceneTransfer.PendingResult.Value);
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
