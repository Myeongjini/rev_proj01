using System.Collections;
using TMPro;
using UnityEngine;
using WizardGrower.Player;

namespace WizardGrower.UI
{
    public class LevelUpPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float visibleSeconds = 1.5f;

        private PlayerLevelService service;
        private Coroutine routine;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void Bind(PlayerLevelService service)
        {
            if (this.service != null)
                this.service.LeveledUp -= OnLeveledUp;

            this.service = service;

            if (this.service != null)
                this.service.LeveledUp += OnLeveledUp;
        }

        private void OnLeveledUp(int newLevel, int attackGained, int hpGained)
        {
            EnsureUi();
            if (label != null)
                label.text = $"Level Up! Lv.{newLevel}\nATK +{attackGained}  HP +{hpGained}";

            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            yield return new WaitForSecondsRealtime(visibleSeconds);
            Hide();
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
                label.fontSize = 28f;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(1f, 0.92f, 0.35f, 1f);
            }
        }

        private void OnDestroy()
        {
            if (service != null)
                service.LeveledUp -= OnLeveledUp;
        }
    }
}
