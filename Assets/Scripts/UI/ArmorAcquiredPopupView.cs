using System.Collections;
using TMPro;
using UnityEngine;
using WizardGrower.Armor;

namespace WizardGrower.UI
{
    public class ArmorAcquiredPopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float visibleSeconds = 1.5f;

        private Coroutine routine;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            HideImmediate();
        }

        public void Show(ArmorDefinition armor)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine(armor));
        }

        private IEnumerator ShowRoutine(ArmorDefinition armor)
        {
            if (label != null)
                label.text = armor != null ? $"방어구 획득!\n{armor.displayName}" : "방어구 획득!";
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(visibleSeconds);
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }
    }
}
