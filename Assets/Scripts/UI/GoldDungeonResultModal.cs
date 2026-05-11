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
            EnsureUi();
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
            EnsureUi();
            currentResult = result;
            busy = false;
            gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            long best = service != null ? service.GetBestScore() : 0;
            bool newRecord = result.earnedGold > best;
            resultLabel.text = $"처치 {result.killCount} / 획득 골드 {result.earnedGold:N0}";
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
            await service.CompleteEntryAsync(currentResult, false);
            Hide(true);
        }

        private async void ClaimAd()
        {
            if (busy || service == null)
                return;
            busy = true;
            bool watched = adProvider != null && await adProvider.WatchRewardedAdAsync();
            if (watched)
                await service.CompleteEntryAsync(currentResult, true);
            busy = false;
            if (watched)
                Hide(true);
        }

        private void OnPendingResultChanged()
        {
            if (GoldDungeonSceneTransfer.PendingResult.HasValue)
                Show(GoldDungeonSceneTransfer.PendingResult.Value);
        }

        private void EnsureUi()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            Image overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.62f);
            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }

            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(transform, false);
                RectTransform rect = panelGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(620f, 430f);
                panelGo.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
                panel = panelGo.transform;
            }

            if (titleLabel == null)
                titleLabel = CreateText(panel, "Title", "골드던전 결과", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(-120f, 58f), 30f, FontStyles.Bold);
            if (resultLabel == null)
                resultLabel = CreateText(panel, "Result", "처치 0 / 획득 골드 0", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 66f), new Vector2(-90f, 52f), 23f, FontStyles.Bold);
            if (bestLabel == null)
                bestLabel = CreateText(panel, "Best", "Best Record: 0", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 14f), new Vector2(-90f, 48f), 20f, FontStyles.Normal);
            if (closeButton == null)
                closeButton = CreateButton(panel, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -32f), new Vector2(48f, 44f), new Color(0.16f, 0.18f, 0.22f, 1f));
            if (claimButton == null)
                claimButton = CreateButton(panel, "ClaimButton", "받기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 70f), new Vector2(210f, 60f), new Color(0.12f, 0.36f, 0.92f, 1f));
            if (claimAdButton == null)
                claimAdButton = CreateButton(panel, "ClaimAdButton", "광고 보고 2배", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(140f, 70f), new Vector2(230f, 60f), new Color(0.92f, 0.48f, 0.08f, 1f));

            closeButton.onClick.RemoveAllListeners();
            claimButton.onClick.RemoveAllListeners();
            claimAdButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Hide(false));
            claimButton.onClick.AddListener(Claim);
            claimAdButton.onClick.AddListener(ClaimAd);
        }

        private TMP_Text CreateText(Transform parent, string name, string text, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, float fontSize, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            TMP_Text label = go.GetComponent<TMP_Text>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = Color.white;
            ApplyProjectFont(label);
            label.text = text;
            return label;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            CreateText(go.transform, "Label", text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 17f, FontStyles.Bold);
            return go.GetComponent<Button>();
        }

        private void ApplyProjectFont(TMP_Text text)
        {
            if (text == null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
                return;

            TMP_Text[] labels = canvas.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].font != null && labels[i].font.name.Contains("Nanum"))
                {
                    text.font = labels[i].font;
                    return;
                }
            }
        }
    }
}
