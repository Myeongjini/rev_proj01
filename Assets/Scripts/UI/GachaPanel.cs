using System.Collections.Generic;
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

        private GachaService service;
        private GachaDefinition definition;
        private float feedbackTimer;

        private void Awake()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
        }

        public void Initialize(GachaService service, GachaDefinition definition)
        {
            if (this.service != null)
            {
                this.service.PityChanged -= OnPityChanged;
                this.service.StateChanged -= Refresh;
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

            if (this.service != null)
            {
                this.service.PityChanged += OnPityChanged;
                this.service.StateChanged += Refresh;
                this.service.PullFailed += ShowFeedback;
            }

            Refresh();
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
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            gameObject.SetActive(false);
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
            RefreshPity();
        }

        private void OnPityChanged(int _)
        {
            RefreshPity();
        }

        private void RefreshPity()
        {
            if (pityLabel == null)
                return;

            int pity = service != null ? service.CurrentPity : 0;
            int threshold = definition != null ? definition.pityThreshold : 30;
            SummonLevelDefinition level = service != null ? service.CurrentLevelDefinition : null;
            int summonLevel = service != null ? service.CurrentSummonLevel : 1;
            int pulls = service != null ? service.SummonPullsInLevel : 0;
            string progress = level != null && level.pullsToNextLevel > 0 ? $"{pulls}/{level.pullsToNextLevel}" : "MAX";
            string maxGrade = level != null ? WeaponGradeLabels.UpperKo(level.maxUpperGrade) : "-";
            pityLabel.text = $"소환 Lv. {summonLevel}\n성장 {progress}\n최대 {maxGrade}\n천장 {pity}/{threshold}";
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

            service.PityChanged -= OnPityChanged;
            service.StateChanged -= Refresh;
            service.PullFailed -= ShowFeedback;
        }
    }
}
