using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WizardGrower.Player;
using WizardGrower.Skills;

namespace WizardGrower.UI
{
    public class SkillUnlockPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float visibleSeconds = 1.5f;

        private readonly HashSet<string> announcedSkillIds = new HashSet<string>();
        private readonly Queue<string> pendingMessages = new Queue<string>();
        private PlayerLevelService levelService;
        private SkillDatabase skillDatabase;
        private Coroutine routine;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void Bind(PlayerLevelService levelService, SkillDatabase skillDatabase)
        {
            if (this.levelService != null)
                this.levelService.LevelChanged -= OnLevelChanged;

            this.levelService = levelService;
            this.skillDatabase = skillDatabase;
            announcedSkillIds.Clear();
            pendingMessages.Clear();

            MarkAlreadyUnlocked();

            if (this.levelService != null)
                this.levelService.LevelChanged += OnLevelChanged;
        }

        private void OnLevelChanged(int newLevel)
        {
            if (skillDatabase == null)
                return;

            foreach (SkillDefinition skill in skillDatabase.OrderedSkills)
            {
                if (skill == null || string.IsNullOrEmpty(skill.skillId))
                    continue;
                if (announcedSkillIds.Contains(skill.skillId))
                    continue;
                if (Mathf.Max(1, skill.unlockLevel) > newLevel)
                    continue;

                announcedSkillIds.Add(skill.skillId);
                pendingMessages.Enqueue($"새 스킬: {skill.displayName} 해금됨!");
            }

            if (routine == null && pendingMessages.Count > 0)
                routine = StartCoroutine(ShowQueuedRoutine());
        }

        private void MarkAlreadyUnlocked()
        {
            if (levelService == null || skillDatabase == null)
                return;

            int currentLevel = levelService.CurrentLevel;
            foreach (SkillDefinition skill in skillDatabase.OrderedSkills)
            {
                if (skill == null || string.IsNullOrEmpty(skill.skillId))
                    continue;
                if (Mathf.Max(1, skill.unlockLevel) <= currentLevel)
                    announcedSkillIds.Add(skill.skillId);
            }
        }

        private IEnumerator ShowQueuedRoutine()
        {
            while (pendingMessages.Count > 0)
            {
                gameObject.SetActive(true);
                EnsureUi();
                if (label != null)
                    label.text = pendingMessages.Dequeue();
                if (group != null)
                {
                    group.alpha = 1f;
                    group.blocksRaycasts = false;
                    group.interactable = false;
                }

                yield return new WaitForSecondsRealtime(visibleSeconds);
                Hide();
                yield return new WaitForSecondsRealtime(0.1f);
            }

            routine = null;
        }

        private void Hide()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            if (label != null)
                label.text = string.Empty;
        }

        private void EnsureUi()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(transform, false);
                RectTransform rect = labelGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                label = labelGo.GetComponent<TMP_Text>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 24f;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(0.52f, 0.9f, 1f, 1f);
            }
        }

        private void OnDestroy()
        {
            if (levelService != null)
                levelService.LevelChanged -= OnLevelChanged;
        }
    }
}
