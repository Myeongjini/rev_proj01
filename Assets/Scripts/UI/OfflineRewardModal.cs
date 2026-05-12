using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Ads;
using WizardGrower.Core;
using WizardGrower.Offline;

namespace WizardGrower.UI
{
    public class OfflineRewardModal : MonoBehaviour, IGameStartupPopup
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text elapsedLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text expLabel;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimButtonLabel;
        [SerializeField] private Button claimAdButton;
        [SerializeField] private TMP_Text claimAdButtonLabel;
        [SerializeField] private Button closeButton;

        private OfflineRewardService service;
        private IRewardedAdProvider ad;
        private OfflineRewardSnapshot snapshot;
        private TaskCompletionSource<bool> closeCompletion;
        private bool busy;

        public event Action Closed;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();
            Hide();
        }

        public void Bind(OfflineRewardService service, IRewardedAdProvider ad)
        {
            this.service = service;
            this.ad = ad;
        }

        public bool ShouldShow()
        {
            return service != null;
        }

        public async Task ShowAsync()
        {
            if (service == null)
                return;

            OfflineRewardSnapshot resolved = await service.ResolvePendingAsync();
            if (resolved.baseGold <= 0 && resolved.baseExp <= 0)
                return;

            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
            closeCompletion = completion;
            Show(resolved);
            await completion.Task;
        }

        public void Show(OfflineRewardSnapshot snapshot)
        {
            ResolveReferences();
            this.snapshot = snapshot;
            busy = false;
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (elapsedLabel != null)
                elapsedLabel.text = $"경과 시간: {FormatElapsed(snapshot.elapsedSeconds)}";
            if (goldLabel != null)
                goldLabel.text = $"누적 골드: {snapshot.baseGold:N0}";
            if (expLabel != null)
                expLabel.text = $"누적 EXP: {snapshot.baseExp:N0}";
            if (claimButtonLabel != null)
                claimButtonLabel.text = "받기";
            if (claimAdButtonLabel != null)
                claimAdButtonLabel.text = $"광고 보고 2배 (골드 {snapshot.maxAdMultipliedGold:N0} / EXP {snapshot.maxAdMultipliedExp:N0})";
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
            Closed?.Invoke();
            closeCompletion?.TrySetResult(true);
            closeCompletion = null;
        }

        private async void Claim()
        {
            if (busy || service == null)
                return;
            busy = true;
            SetButtonsInteractable(false);
            await service.ClaimAsync(false);
            Hide();
        }

        private async void ClaimWithAd()
        {
            if (busy || service == null)
                return;
            busy = true;
            SetButtonsInteractable(false);
            bool watched = ad != null && await ad.WatchRewardedAdAsync();
            if (watched)
                await service.ClaimAsync(true);
            busy = false;
            SetButtonsInteractable(true);
            if (watched)
                Hide();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (claimButton != null)
                claimButton.interactable = interactable;
            if (claimAdButton != null)
                claimAdButton.interactable = interactable;
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            titleLabel = titleLabel != null ? titleLabel : FindText("Title");
            elapsedLabel = elapsedLabel != null ? elapsedLabel : FindText("Elapsed");
            goldLabel = goldLabel != null ? goldLabel : FindText("Gold");
            expLabel = expLabel != null ? expLabel : FindText("EXP");
            closeButton = closeButton != null ? closeButton : FindButton("CloseButton");
            claimButton = claimButton != null ? claimButton : FindButton("ClaimButton");
            claimAdButton = claimAdButton != null ? claimAdButton : FindButton("ClaimAdButton");
            claimButtonLabel = claimButtonLabel != null ? claimButtonLabel : FindButtonLabel(claimButton);
            claimAdButtonLabel = claimAdButtonLabel != null ? claimAdButtonLabel : FindButtonLabel(claimAdButton);
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(Claim);
            }
            if (claimAdButton != null)
            {
                claimAdButton.onClick.RemoveAllListeners();
                claimAdButton.onClick.AddListener(ClaimWithAd);
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

        private TMP_Text FindButtonLabel(Button button)
        {
            return button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private string FormatElapsed(long seconds)
        {
            long clamped = Math.Max(0, seconds);
            long hours = clamped / 3600;
            long minutes = (clamped % 3600) / 60;
            long secs = clamped % 60;
            if (hours > 0)
                return $"{hours}시간 {minutes}분";
            if (minutes > 0)
                return $"{minutes}분 {secs}초";
            return $"{secs}초";
        }
    }
}
