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
        [SerializeField] private GachaResultPanel resultPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button probabilityButton;
        [SerializeField] private GachaProbabilityPopup probabilityPopup;

        private GachaService service;
        private GachaDefinition definition;
        private float feedbackTimer;
        private bool isOpen;
        public bool IsOpen => isOpen;
        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
        }

        public void Initialize(GachaService service, GachaDefinition definition)
        {
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

        private void PullSingle()
        {
            if (service == null)
                return;

            WeaponDefinition pulled;
            if (service.TrySinglePull(out pulled) && resultPanel != null)
                resultPanel.Show(new[] { pulled });
            Refresh();
        }

        private void PullTen()
        {
            if (service == null)
                return;

            List<WeaponDefinition> pulled;
            if (service.TryTenPull(out pulled) && resultPanel != null)
                resultPanel.Show(pulled);
            Refresh();
        }

        private void Refresh()
        {
            if (definition == null && service != null)
                definition = service.Definition;

            if (titleLabel != null)
                titleLabel.text = definition != null ? definition.displayName : "가챠";
            if (singlePullLabel != null)
                singlePullLabel.text = definition != null ? $"1회\n{definition.costSingle} Gem" : "1회";
            if (tenPullLabel != null)
                tenPullLabel.text = definition != null ? $"10회\n{definition.costTen} Gem" : "10회";
            if (singlePullButton != null)
                singlePullButton.interactable = service != null && service.CanSinglePull();
            if (tenPullButton != null)
                tenPullButton.interactable = service != null && service.CanTenPull();
            if (probabilityButton != null)
                probabilityButton.interactable = service != null && service.CurrentLevelDefinition != null;
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
        }
    }
}
