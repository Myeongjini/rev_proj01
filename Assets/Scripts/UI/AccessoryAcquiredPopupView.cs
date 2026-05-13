using System.Collections;
using TMPro;
using UnityEngine;
using WizardGrower.Accessory;

namespace WizardGrower.UI
{
    public class AccessoryAcquiredPopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float visibleSeconds = 1.5f;

        private Coroutine routine;
        private bool suppressInitialHide;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (!suppressInitialHide)
                HideImmediate();
        }

        public void Show(AccessoryDefinition accessory)
        {
            if (!gameObject.activeSelf)
            {
                suppressInitialHide = true;
                gameObject.SetActive(true);
                suppressInitialHide = false;
            }

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine(accessory));
        }

        private IEnumerator ShowRoutine(AccessoryDefinition accessory)
        {
            if (label != null)
                label.text = accessory != null ? $"장신구 획득!\n{accessory.displayName}" : "장신구 획득!";
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }
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
