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
        [SerializeField] private Button enhancementStoneTabButton;
        [SerializeField] private Transform difficultySlotContainer;
        [SerializeField] private string dungeonSceneName = "GoldDungeonScene";
        [SerializeField] private string expDungeonSceneName = "EXPDungeonScene";
        [SerializeField] private string enhancementStoneDungeonSceneName = "EnhancementStoneDungeonScene";

        private GoldDungeonService service;
        private EXPDungeonService expService;
        private EnhancementStoneDungeonService enhancementStoneService;
        private readonly System.Collections.Generic.List<GoldDungeonDifficultySlotView> difficultySlots = new System.Collections.Generic.List<GoldDungeonDifficultySlotView>();
        private int selectedDifficultyIndex;
        private DungeonTab activeTab;

        private enum DungeonTab
        {
            Gold,
            Exp,
            EnhancementStone
        }

        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();
            Close();
        }

        public void Bind(GoldDungeonService service = null, EXPDungeonService expService = null, EnhancementStoneDungeonService enhancementStoneService = null)
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
            if (enhancementStoneService != null)
            {
                if (this.enhancementStoneService != null)
                    this.enhancementStoneService.EntryCountChanged -= OnEntryCountChanged;
                this.enhancementStoneService = enhancementStoneService;
                this.enhancementStoneService.EntryCountChanged += OnEntryCountChanged;
            }

            ResolveReferences();
            RefreshFromService();
        }

        public void Open()
        {
            ResolveReferences();
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
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
            ResolveReferences();
            if (remainingLabel != null)
                remainingLabel.text = $"잔여 입장 {Mathf.Max(0, remainingEntries)}/{Mathf.Max(1, dailyLimit)}";
            if (titleLabel != null)
                titleLabel.text = GetActiveTitle();
            if (feedbackLabel != null)
                feedbackLabel.text = GetActiveDescription();
            if (enterButton != null)
                enterButton.interactable = remainingEntries > 0;
            if (sweepButton != null)
                sweepButton.interactable = remainingEntries > 0 && GetActiveBestScore() > 0;
            RefreshDifficultySlots();
            RefreshTabs();
        }

        private async void Enter()
        {
            if (activeTab == DungeonTab.EnhancementStone)
            {
                if (enhancementStoneService != null)
                {
                    bool stoneEntered = await enhancementStoneService.BeginEntryAsync(selectedDifficultyIndex);
                    if (!stoneEntered)
                    {
                        if (feedbackLabel != null)
                            feedbackLabel.text = "입장 조건을 확인해주세요";
                        RefreshFromService();
                        return;
                    }
                }

                _ = SceneManager.LoadSceneAsync(enhancementStoneDungeonSceneName, LoadSceneMode.Single);
                return;
            }

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
            if (activeTab == DungeonTab.EnhancementStone)
            {
                if (enhancementStoneService == null)
                    return;

                long bestStone = enhancementStoneService.GetBestScore();
                if (bestStone <= 0)
                    return;

                bool stoneSweepEntered = await enhancementStoneService.BeginEntryAsync(selectedDifficultyIndex);
                if (!stoneSweepEntered)
                {
                    if (feedbackLabel != null)
                        feedbackLabel.text = "입장 조건을 확인해주세요";
                    RefreshFromService();
                    return;
                }

                EnhancementStoneDungeonSceneTransfer.SetPending(new EnhancementStoneDungeonResult
                {
                    killCount = 0,
                    earnedStone = bestStone,
                    difficulty = selectedDifficultyIndex + 1
                });
                Close();
                return;
            }

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
            if (activeTab == DungeonTab.EnhancementStone)
            {
                if (enhancementStoneService == null)
                {
                    Refresh(3, 3);
                    return;
                }

                int stoneUsed = await enhancementStoneService.GetTodayEntryCountAsync();
                Refresh(enhancementStoneService.DailyEntryLimit - stoneUsed, enhancementStoneService.DailyEntryLimit);
                return;
            }

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

        private void ResolveReferences()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (titleLabel == null)
                titleLabel = FindText("Title");
            if (goldTabButton == null)
                goldTabButton = FindButton("GoldTabButton");
            if (expTabButton == null)
                expTabButton = FindButton("EXPTabButton");
            if (enhancementStoneTabButton == null)
                enhancementStoneTabButton = FindButton("EnhancementStoneTabButton");
            if (remainingLabel == null)
                remainingLabel = FindText("Remaining");
            if (feedbackLabel == null)
                feedbackLabel = FindText("Feedback");
            if (enterButton == null)
                enterButton = FindButton("EnterButton");
            if (sweepButton == null)
                sweepButton = FindButton("SweepButton");
            if (cancelButton == null)
                cancelButton = FindButton("CancelButton");
            if (difficultySlotContainer == null)
                difficultySlotContainer = FindChildTransform("DifficultySlots");
            ResolveDifficultySlots();
        }

        private void WireButtons()
        {
            if (enterButton != null)
            {
                enterButton.onClick.RemoveAllListeners();
                enterButton.onClick.AddListener(Enter);
            }
            if (sweepButton != null)
            {
                sweepButton.onClick.RemoveAllListeners();
                sweepButton.onClick.AddListener(Sweep);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(Close);
            }
            if (goldTabButton != null)
            {
                goldTabButton.onClick.RemoveAllListeners();
                goldTabButton.onClick.AddListener(() => SelectTab(DungeonTab.Gold));
            }
            if (expTabButton != null)
            {
                expTabButton.onClick.RemoveAllListeners();
                expTabButton.onClick.AddListener(() => SelectTab(DungeonTab.Exp));
            }
            if (enhancementStoneTabButton != null)
            {
                enhancementStoneTabButton.onClick.RemoveAllListeners();
                enhancementStoneTabButton.onClick.AddListener(() => SelectTab(DungeonTab.EnhancementStone));
            }

            for (int i = 0; i < difficultySlots.Count; i++)
            {
                int captured = i;
                difficultySlots[i].AddClickListener(() =>
                {
                    selectedDifficultyIndex = captured;
                    RefreshDifficultySlots();
                });
            }
        }

        private void ResolveDifficultySlots()
        {
            if (difficultySlots.Count > 0)
                return;

            GoldDungeonDifficultySlotView[] slots = difficultySlotContainer != null
                ? difficultySlotContainer.GetComponentsInChildren<GoldDungeonDifficultySlotView>(true)
                : GetComponentsInChildren<GoldDungeonDifficultySlotView>(true);
            Array.Sort(slots, (a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < slots.Length; i++)
                difficultySlots.Add(slots[i]);
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
            return activeTab switch
            {
                DungeonTab.Exp => expService != null ? expService.GetBestScore() : 0,
                DungeonTab.EnhancementStone => enhancementStoneService != null ? enhancementStoneService.GetBestScore() : 0,
                _ => service != null ? service.GetBestScore() : 0
            };
        }

        private GoldDungeonDifficulty GetActiveDifficulty(int index)
        {
            if (activeTab == DungeonTab.Exp)
            {
                if (expService != null && expService.Difficulties != null && index < expService.Difficulties.Count)
                    return expService.Difficulties[index];
            }
            else if (activeTab == DungeonTab.EnhancementStone)
            {
                if (enhancementStoneService != null && enhancementStoneService.Difficulties != null && index < enhancementStoneService.Difficulties.Count)
                    return enhancementStoneService.Difficulties[index];
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
            if (activeTab == DungeonTab.EnhancementStone && enhancementStoneService != null)
                return enhancementStoneService.IsDifficultyUnlocked(index);
            if (activeTab == DungeonTab.Gold && service != null)
                return service.IsDifficultyUnlocked(index);
            return difficulty != null && difficulty.unlockPlayerLevel <= 0;
        }

        private void RefreshTabs()
        {
            if (goldTabButton != null)
                goldTabButton.GetComponent<Image>().color = activeTab == DungeonTab.Gold ? new Color(0.78f, 0.48f, 0.12f, 1f) : new Color(0.18f, 0.18f, 0.20f, 1f);
            if (expTabButton != null)
                expTabButton.GetComponent<Image>().color = activeTab == DungeonTab.Exp ? new Color(0.12f, 0.36f, 0.92f, 1f) : new Color(0.18f, 0.18f, 0.20f, 1f);
            if (enhancementStoneTabButton != null)
                enhancementStoneTabButton.GetComponent<Image>().color = activeTab == DungeonTab.EnhancementStone ? new Color(0.15f, 0.62f, 0.58f, 1f) : new Color(0.18f, 0.18f, 0.20f, 1f);
        }

        private string GetActiveTitle()
        {
            return activeTab switch
            {
                DungeonTab.Exp => "EXP 던전",
                DungeonTab.EnhancementStone => "강화석 던전",
                _ => "골드 던전"
            };
        }

        private string GetActiveDescription()
        {
            return activeTab switch
            {
                DungeonTab.Exp => "EXP 보상 던전",
                DungeonTab.EnhancementStone => "강화석 보상 던전",
                _ => "골드 보상 던전"
            };
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

        private Transform FindChildTransform(string objectName)
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i] != null && rects[i].name == objectName)
                    return rects[i];
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
