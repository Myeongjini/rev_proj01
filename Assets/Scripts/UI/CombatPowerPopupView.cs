using System.Collections;
using TMPro;
using UnityEngine;
using WizardGrower.Player;

namespace WizardGrower.UI
{
    public class CombatPowerPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float visibleSeconds = 1f;

        private CombatPowerService service;
        private Coroutine routine;

        private void Awake()
        {
            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);
            if (group == null)
                group = GetComponent<CanvasGroup>();
            Hide();
        }

        public void Bind(CombatPowerService service)
        {
            if (this.service != null)
                this.service.PowerIncreased -= Show;

            this.service = service;
            if (this.service != null)
                this.service.PowerIncreased += Show;
        }

        public void Show(float currentPower, float delta)
        {
            if (label != null)
                label.text = $"전투력: {currentPower:0} (+{delta:0})";

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

        private void OnDestroy()
        {
            if (service != null)
                service.PowerIncreased -= Show;
        }
    }
}
