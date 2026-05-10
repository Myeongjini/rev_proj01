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
            EnsureUi();
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
            if (resolved.baseGold <= 0)
                return;

            closeCompletion = new TaskCompletionSource<bool>();
            Show(resolved);
            await closeCompletion.Task;
        }

        public void Show(OfflineRewardSnapshot snapshot)
        {
            EnsureUi();
            this.snapshot = snapshot;
            busy = false;
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            elapsedLabel.text = $"경과 시간: {FormatElapsed(snapshot.elapsedSeconds)}";
            goldLabel.text = $"누적 골드: {snapshot.baseGold:N0}";
            claimButtonLabel.text = "받기";
            claimAdButtonLabel.text = $"광고 보고 2배 ({snapshot.maxAdMultipliedGold:N0})";
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
            await service.ClaimAsync(false);
            Hide();
        }

        private async void ClaimWithAd()
        {
            if (busy || service == null)
                return;
            busy = true;
            bool watched = ad != null && await ad.WatchRewardedAdAsync();
            if (watched)
                await service.ClaimAsync(true);
            busy = false;
            if (watched)
                Hide();
        }

        private void EnsureUi()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Image overlay = GetComponent<Image>();
            if (overlay == null)
                overlay = gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(transform, false);
                RectTransform panelRect = panelGo.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(620f, 420f);
                panelRect.anchoredPosition = Vector2.zero;
                panelGo.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
                panel = panelGo.transform;
            }

            if (titleLabel == null)
                titleLabel = CreateText(panel, "Title", "오프라인 보상", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(-120f, 58f), 30f, FontStyles.Bold);
            if (elapsedLabel == null)
                elapsedLabel = CreateText(panel, "Elapsed", "경과 시간: 0분", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 70f), new Vector2(-80f, 50f), 22f, FontStyles.Normal);
            if (goldLabel == null)
                goldLabel = CreateText(panel, "Gold", "누적 골드: 0", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 18f), new Vector2(-80f, 50f), 24f, FontStyles.Bold);
            if (closeButton == null)
                closeButton = CreateButton(panel, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -32f), new Vector2(48f, 44f), new Color(0.16f, 0.18f, 0.22f, 1f), out _);
            if (claimButton == null)
                claimButton = CreateButton(panel, "ClaimButton", "받기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 70f), new Vector2(220f, 62f), new Color(0.12f, 0.32f, 0.82f, 1f), out claimButtonLabel);
            if (claimAdButton == null)
                claimAdButton = CreateButton(panel, "ClaimAdButton", "광고 보고 2배", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150f, 70f), new Vector2(250f, 62f), new Color(0.92f, 0.48f, 0.08f, 1f), out claimAdButtonLabel);

            closeButton.onClick.RemoveAllListeners();
            claimButton.onClick.RemoveAllListeners();
            claimAdButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
            claimButton.onClick.AddListener(Claim);
            claimAdButton.onClick.AddListener(ClaimWithAd);
        }

        private TMP_Text CreateText(Transform parent, string name, string value, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, float fontSize, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            return text;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Color color, out TMP_Text label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            label = CreateText(go.transform, "Label", text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 17f, FontStyles.Bold);
            RectTransform labelRect = label.transform as RectTransform;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
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
