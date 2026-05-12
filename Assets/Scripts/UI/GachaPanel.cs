using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class GachaPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text pityLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private Button singlePullButton;
        [SerializeField] private TMP_Text singlePullLabel;
        [SerializeField] private Button tenPullButton;
        [SerializeField] private TMP_Text tenPullLabel;
        [SerializeField] private Button thirtyPullButton;
        [SerializeField] private TMP_Text thirtyPullLabel;
        [SerializeField] private GachaResultPanel resultPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button probabilityButton;
        [SerializeField] private GachaProbabilityPopup probabilityPopup;

        private GachaService service;
        private GachaDefinition definition;
        private float feedbackTimer;
        private bool isOpen;
        private bool pullInFlight;
        public bool IsOpen => isOpen;
        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            EnsureThirtyPullButton();
            ArrangePullButtons();
        }

        public void Initialize(GachaService service, GachaDefinition definition)
        {
            EnsureThirtyPullButton();
            ArrangePullButtons();

            if (this.service != null)
            {
                this.service.StateChanged -= Refresh;
                this.service.SummonLevelChanged -= OnSummonLevelChanged;
                this.service.PullFailed -= ShowFeedback;
            }

            this.service = service;
            this.definition = definition;

            if (singlePullButton != null)
            {
                singlePullButton.onClick.RemoveListener(PullSingle);
                singlePullButton.onClick.AddListener(PullSingle);
            }
            if (tenPullButton != null)
            {
                tenPullButton.onClick.RemoveListener(PullTen);
                tenPullButton.onClick.AddListener(PullTen);
            }
            if (thirtyPullButton != null)
            {
                thirtyPullButton.onClick.RemoveListener(PullThirty);
                thirtyPullButton.onClick.AddListener(PullThirty);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
            if (probabilityButton != null)
            {
                probabilityButton.onClick.RemoveListener(ShowProbabilityPopup);
                probabilityButton.onClick.AddListener(ShowProbabilityPopup);
            }

            if (this.service != null)
            {
                this.service.StateChanged += Refresh;
                this.service.SummonLevelChanged += OnSummonLevelChanged;
                this.service.PullFailed += ShowFeedback;
            }
            if (probabilityPopup != null)
                probabilityPopup.Bind(this.service);

            Refresh();
            if (probabilityPopup != null)
                probabilityPopup.Hide();
        }

        public void Toggle()
        {
            if (group != null && group.alpha > 0.5f)
                Close();
            else
                Open();
        }

        public void Open()
        {
            isOpen = true;
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            gameObject.SetActive(true);
            Refresh();
            OpenStateChanged?.Invoke(true);
        }

        public void Close()
        {
            isOpen = false;
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            if (probabilityPopup != null)
                probabilityPopup.Hide();
            gameObject.SetActive(false);
            OpenStateChanged?.Invoke(false);
        }

        private void Update()
        {
            if (feedbackTimer <= 0f)
                return;

            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackTimer <= 0f && feedbackLabel != null)
                feedbackLabel.text = string.Empty;
        }

        private async void PullSingle()
        {
            if (service == null || pullInFlight)
                return;

            await PullAsync(1);
        }

        private async void PullTen()
        {
            if (service == null || pullInFlight)
                return;

            await PullAsync(10);
        }

        private async void PullThirty()
        {
            if (service == null || pullInFlight)
                return;

            await PullAsync(30);
        }

        private async System.Threading.Tasks.Task PullAsync(int count)
        {
            pullInFlight = true;
            Refresh();
            try
            {
                GachaPullResult result = count switch
                {
                    1 => await service.TrySinglePullAsync(),
                    10 => await service.TryTenPullAsync(),
                    30 => await service.TryThirtyPullAsync(),
                    _ => GachaPullResult.Fail("지원하지 않는 소환 횟수입니다.")
                };

                if (result.Success && result.PulledList.Count > 0 && resultPanel != null)
                    resultPanel.Show(result.PulledList);
                else if (!result.Success && !string.IsNullOrEmpty(result.FailureMessage))
                    ShowFeedback(result.FailureMessage);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Gacha pull failed: {ex.GetBaseException().Message}");
                ShowFeedback("서버 뽑기에 실패했습니다.");
            }
            finally
            {
                pullInFlight = false;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (definition == null && service != null)
                definition = service.Definition;

            if (titleLabel != null)
                titleLabel.text = definition != null ? definition.displayName : "가챠";
            if (singlePullLabel != null)
                singlePullLabel.text = definition != null ? FormatPullLabel(1, definition.costSingle) : "1회";
            if (tenPullLabel != null)
                tenPullLabel.text = definition != null ? FormatPullLabel(10, definition.costTen) : "10회";
            if (thirtyPullLabel != null)
                thirtyPullLabel.text = definition != null ? FormatPullLabel(30, definition.costThirty) : "30회";
            if (singlePullButton != null)
                singlePullButton.interactable = !pullInFlight && service != null && service.CanSinglePull();
            if (tenPullButton != null)
                tenPullButton.interactable = !pullInFlight && service != null && service.CanTenPull();
            if (thirtyPullButton != null)
                thirtyPullButton.interactable = !pullInFlight && service != null && service.CanThirtyPull();
            if (probabilityButton != null)
                probabilityButton.interactable = !pullInFlight && service != null && service.CurrentLevelDefinition != null;
            if (pullInFlight && feedbackLabel != null)
                feedbackLabel.text = "서버 처리 중...";
            RefreshInsufficientFeedback();
            RefreshPity();
        }

        private void OnSummonLevelChanged(int _)
        {
            RefreshPity();
        }

        private void RefreshPity()
        {
            if (pityLabel == null)
                return;

            SummonLevelDefinition level = service != null ? service.CurrentLevelDefinition : null;
            int summonLevel = service != null ? service.CurrentSummonLevel : 1;
            int pulls = service != null ? service.SummonPullsInLevel : 0;
            string progress = level != null && level.pullsToNextLevel > 0 ? $"{pulls}/{level.pullsToNextLevel}" : "MAX";
            pityLabel.text = $"소환 Lv. {summonLevel}\n성장 {progress}";
        }

        private void ShowProbabilityPopup()
        {
            if (probabilityPopup != null && service != null)
                probabilityPopup.Show(service.CurrentSummonLevel);
        }

        private void ShowFeedback(string message)
        {
            if (feedbackLabel == null)
                return;

            feedbackTimer = 1.5f;
            feedbackLabel.text = message;
        }

        private void EnsureThirtyPullButton()
        {
            if (thirtyPullButton != null || tenPullButton == null)
                return;

            GameObject clone = Instantiate(tenPullButton.gameObject, tenPullButton.transform.parent);
            clone.name = "ThirtyPullButton";
            thirtyPullButton = clone.GetComponent<Button>();
            thirtyPullLabel = clone.GetComponentInChildren<TMP_Text>(true);
            if (thirtyPullButton != null)
                thirtyPullButton.onClick.RemoveAllListeners();
        }

        private void ArrangePullButtons()
        {
            SetPullButtonRect(singlePullButton, -224f);
            SetPullButtonRect(tenPullButton, 0f);
            SetPullButtonRect(thirtyPullButton, 224f);
        }

        private static void SetPullButtonRect(Button button, float x)
        {
            if (button == null)
                return;

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, -36f);
            rect.sizeDelta = new Vector2(188f, 96f);
        }

        private static string FormatPullLabel(int count, int cost)
        {
            return $"{count}회 {cost:N0}";
        }

        private void RefreshInsufficientFeedback()
        {
            if (!isOpen || pullInFlight || feedbackLabel == null || feedbackTimer > 0f || service == null || definition == null)
                return;

            bool anyDisabled = !service.CanSinglePull() || !service.CanTenPull() || !service.CanThirtyPull();
            feedbackLabel.text = anyDisabled ? "젬이 부족합니다" : string.Empty;
        }

        private void OnDestroy()
        {
            if (service == null)
                return;

            service.StateChanged -= Refresh;
            service.SummonLevelChanged -= OnSummonLevelChanged;
            service.PullFailed -= ShowFeedback;
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (probabilityButton != null)
                probabilityButton.onClick.RemoveListener(ShowProbabilityPopup);
            if (thirtyPullButton != null)
                thirtyPullButton.onClick.RemoveListener(PullThirty);
        }
    }
}
