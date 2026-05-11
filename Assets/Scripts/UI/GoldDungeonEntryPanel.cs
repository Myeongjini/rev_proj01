using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WizardGrower.Dungeons;

namespace WizardGrower.UI
{
    public class GoldDungeonEntryPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text remainingLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private Button enterButton;
        [SerializeField] private Button sweepButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button goldTabButton;
        [SerializeField] private Button expTabButton;
        [SerializeField] private string dungeonSceneName = "GoldDungeonScene";
        [SerializeField] private string expDungeonSceneName = "EXPDungeonScene";

        private GoldDungeonService service;
        private EXPDungeonService expService;
        private readonly System.Collections.Generic.List<GoldDungeonDifficultySlotView> difficultySlots = new System.Collections.Generic.List<GoldDungeonDifficultySlotView>();
        private int selectedDifficultyIndex;
        private DungeonTab activeTab;

        private enum DungeonTab
        {
            Gold,
            Exp
        }

        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            EnsureUi();
            Close();
        }

        public void Bind(GoldDungeonService service = null, EXPDungeonService expService = null)
        {
            if (service != null)
            {
                if (this.service != null)
                    this.service.EntryCountChanged -= OnEntryCountChanged;
                this.service = service;
                this.service.EntryCountChanged += OnEntryCountChanged;
            }
            if (expService != null)
            {
                if (this.expService != null)
                    this.expService.EntryCountChanged -= OnEntryCountChanged;
                this.expService = expService;
                this.expService.EntryCountChanged += OnEntryCountChanged;
            }

            EnsureUi();
            RefreshFromService();
        }

        public void Open()
        {
            EnsureUi();
            gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            RefreshFromService();
            OpenStateChanged?.Invoke(true);
        }

        public void Close()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
            OpenStateChanged?.Invoke(false);
        }

        public void Refresh(int remainingEntries, int dailyLimit)
        {
            EnsureUi();
            if (remainingLabel != null)
                remainingLabel.text = $"잔여 입장 {Mathf.Max(0, remainingEntries)}/{Mathf.Max(1, dailyLimit)}";
            if (titleLabel != null)
                titleLabel.text = activeTab == DungeonTab.Gold ? "골드 던전" : "EXP 던전";
            if (feedbackLabel != null)
                feedbackLabel.text = activeTab == DungeonTab.Gold ? "골드 보상 던전" : "EXP 보상 던전";
            if (enterButton != null)
                enterButton.interactable = remainingEntries > 0;
            if (sweepButton != null)
                sweepButton.interactable = remainingEntries > 0 && GetActiveBestScore() > 0;
            RefreshDifficultySlots();
            RefreshTabs();
        }

        private async void Enter()
        {
            if (activeTab == DungeonTab.Exp)
            {
                if (expService != null)
                {
                    bool expEntered = await expService.BeginEntryAsync(selectedDifficultyIndex);
                    if (!expEntered)
                    {
                        if (feedbackLabel != null)
                            feedbackLabel.text = "입장 조건을 확인해주세요";
                        RefreshFromService();
                        return;
                    }
                }

                _ = SceneManager.LoadSceneAsync(expDungeonSceneName, LoadSceneMode.Single);
                return;
            }

            if (service != null)
            {
                bool goldEntered = await service.BeginEntryAsync(selectedDifficultyIndex);
                if (!goldEntered)
                {
                    if (feedbackLabel != null)
                        feedbackLabel.text = "오늘 입장 횟수를 모두 사용했습니다";
                    RefreshFromService();
                    return;
                }
            }

            _ = SceneManager.LoadSceneAsync(dungeonSceneName, LoadSceneMode.Single);
        }

        private async void Sweep()
        {
            if (activeTab == DungeonTab.Exp)
            {
                if (expService == null)
                    return;

                long bestExp = expService.GetBestScore();
                if (bestExp <= 0)
                    return;

                bool expSweepEntered = await expService.BeginEntryAsync(selectedDifficultyIndex);
                if (!expSweepEntered)
                {
                    if (feedbackLabel != null)
                        feedbackLabel.text = "입장 조건을 확인해주세요";
                    RefreshFromService();
                    return;
                }

                EXPDungeonSceneTransfer.SetPending(new EXPDungeonResult
                {
                    killCount = 0,
                    earnedExp = bestExp,
                    difficulty = selectedDifficultyIndex + 1
                });
                Close();
                return;
            }

            if (service == null)
                return;
            long bestScore = service.GetBestScore();
            if (bestScore <= 0)
                return;

            bool goldSweepEntered = await service.BeginEntryAsync(selectedDifficultyIndex);
            if (!goldSweepEntered)
            {
                if (feedbackLabel != null)
                    feedbackLabel.text = "오늘 입장 횟수를 모두 사용했습니다";
                RefreshFromService();
                return;
            }

            GoldDungeonSceneTransfer.SetPending(new GoldDungeonResult
            {
                killCount = 0,
                earnedGold = bestScore,
                difficulty = selectedDifficultyIndex + 1
            });
            Close();
        }

        private async void RefreshFromService()
        {
            if (activeTab == DungeonTab.Exp)
            {
                if (expService == null)
                {
                    Refresh(3, 3);
                    return;
                }

                int expUsed = await expService.GetTodayEntryCountAsync();
                Refresh(expService.DailyEntryLimit - expUsed, expService.DailyEntryLimit);
                return;
            }

            if (service == null)
            {
                Refresh(3, 3);
                return;
            }

            int goldUsed = await service.GetTodayEntryCountAsync();
            Refresh(service.DailyEntryLimit - goldUsed, service.DailyEntryLimit);
        }

        private void OnEntryCountChanged(int _)
        {
            RefreshFromService();
        }

        private void EnsureUi()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            Image overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.55f);

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
                panelRect.sizeDelta = new Vector2(620f, 430f);
                panelRect.anchoredPosition = Vector2.zero;
                panelGo.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
                panel = panelGo.transform;
            }

            if (titleLabel == null)
                titleLabel = CreateText(panel, "Title", "골드던전", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -52f), new Vector2(-120f, 58f), 30f, FontStyles.Bold);
            if (goldTabButton == null)
                goldTabButton = CreateButton(panel, "GoldTabButton", "골드", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -50f), new Vector2(96f, 42f), new Color(0.78f, 0.48f, 0.12f, 1f));
            if (expTabButton == null)
                expTabButton = CreateButton(panel, "EXPTabButton", "EXP", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(196f, -50f), new Vector2(96f, 42f), new Color(0.12f, 0.36f, 0.92f, 1f));
            if (remainingLabel == null)
                remainingLabel = CreateText(panel, "Remaining", "잔여 입장 3/3", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 74f), new Vector2(-90f, 48f), 22f, FontStyles.Bold);
            if (feedbackLabel == null)
                feedbackLabel = CreateText(panel, "Feedback", "난이도 Lv1", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 24f), new Vector2(-90f, 44f), 18f, FontStyles.Normal);
            if (enterButton == null)
                enterButton = CreateButton(panel, "EnterButton", "입장", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 70f), new Vector2(200f, 60f), new Color(0.12f, 0.36f, 0.92f, 1f));
            if (sweepButton == null)
                sweepButton = CreateButton(panel, "SweepButton", "소탕", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 138f), new Vector2(200f, 48f), new Color(0.78f, 0.48f, 0.12f, 1f));
            if (cancelButton == null)
                cancelButton = CreateButton(panel, "CancelButton", "취소", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120f, 70f), new Vector2(200f, 60f), new Color(0.20f, 0.22f, 0.26f, 1f));
            EnsureDifficultySlots(panel);

            enterButton.onClick.RemoveAllListeners();
            sweepButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            goldTabButton.onClick.RemoveAllListeners();
            expTabButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(Enter);
            sweepButton.onClick.AddListener(Sweep);
            cancelButton.onClick.AddListener(Close);
            goldTabButton.onClick.AddListener(() => SelectTab(DungeonTab.Gold));
            expTabButton.onClick.AddListener(() => SelectTab(DungeonTab.Exp));
        }

        private void EnsureDifficultySlots(Transform panel)
        {
            if (difficultySlots.Count > 0)
                return;

            for (int i = 0; i < 5; i++)
            {
                GameObject slotGo = new GameObject($"DifficultySlot_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(GoldDungeonDifficultySlotView));
                slotGo.transform.SetParent(panel, false);
                RectTransform rect = slotGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-208f + i * 104f, -50f);
                rect.sizeDelta = new Vector2(94f, 70f);
                GoldDungeonDifficultySlotView slot = slotGo.GetComponent<GoldDungeonDifficultySlotView>();
                int captured = i;
                slot.AddClickListener(() =>
                {
                    selectedDifficultyIndex = captured;
                    RefreshDifficultySlots();
                });
                difficultySlots.Add(slot);
            }
        }

        private void RefreshDifficultySlots()
        {
            if (difficultySlots.Count == 0)
                return;

            for (int i = 0; i < difficultySlots.Count; i++)
            {
                GoldDungeonDifficulty difficulty = GetActiveDifficulty(i);
                bool unlocked = IsActiveDifficultyUnlocked(i, difficulty);
                difficultySlots[i].Bind(i, difficulty, i == selectedDifficultyIndex, unlocked);
            }
        }

        private void SelectTab(DungeonTab tab)
        {
            activeTab = tab;
            selectedDifficultyIndex = 0;
            RefreshFromService();
        }

        private long GetActiveBestScore()
        {
            return activeTab == DungeonTab.Exp
                ? expService != null ? expService.GetBestScore() : 0
                : service != null ? service.GetBestScore() : 0;
        }

        private GoldDungeonDifficulty GetActiveDifficulty(int index)
        {
            if (activeTab == DungeonTab.Exp)
            {
                if (expService != null && expService.Difficulties != null && index < expService.Difficulties.Count)
                    return expService.Difficulties[index];
            }
            else if (service != null && service.Difficulties != null && index < service.Difficulties.Count)
            {
                return service.Difficulties[index];
            }

            return new GoldDungeonDifficulty { level = index + 1, unlockPlayerLevel = index == 0 ? 0 : (index + 1) * 5 };
        }

        private bool IsActiveDifficultyUnlocked(int index, GoldDungeonDifficulty difficulty)
        {
            if (activeTab == DungeonTab.Exp && expService != null)
                return expService.IsDifficultyUnlocked(index);
            return difficulty != null && difficulty.unlockPlayerLevel <= 0;
        }

        private void RefreshTabs()
        {
            if (goldTabButton != null)
                goldTabButton.GetComponent<Image>().color = activeTab == DungeonTab.Gold ? new Color(0.78f, 0.48f, 0.12f, 1f) : new Color(0.18f, 0.18f, 0.20f, 1f);
            if (expTabButton != null)
                expTabButton.GetComponent<Image>().color = activeTab == DungeonTab.Exp ? new Color(0.12f, 0.36f, 0.92f, 1f) : new Color(0.18f, 0.18f, 0.20f, 1f);
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
            CreateText(go.transform, "Label", text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18f, FontStyles.Bold);
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
