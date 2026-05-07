using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class SplashView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image spinner;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private float spinnerDegreesPerSecond = -180f;

        private TaskCompletionSource<bool> retryCompletion;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponentInChildren<CanvasGroup>();
            if (retryButton != null)
                retryButton.onClick.AddListener(ResolveRetry);
            HideRetry();
        }

        private void Update()
        {
            if (spinner != null && spinner.gameObject.activeInHierarchy)
                spinner.rectTransform.Rotate(0f, 0f, spinnerDegreesPerSecond * Time.unscaledDeltaTime);
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void SetMessage(string message)
        {
            if (messageLabel != null)
                messageLabel.text = message ?? string.Empty;
        }

        public void HideRetry()
        {
            retryCompletion = null;
            if (retryButton != null)
                retryButton.gameObject.SetActive(false);
        }

        public async Task<bool> ShowRetryAsync()
        {
            if (retryButton == null)
                return false;

            retryCompletion = new TaskCompletionSource<bool>();
            retryButton.gameObject.SetActive(true);
            return await retryCompletion.Task;
        }

        public async Task FadeOutAsync(float seconds)
        {
            if (canvasGroup == null || seconds <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / seconds);
                await Task.Yield();
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void ResolveRetry()
        {
            TaskCompletionSource<bool> completion = retryCompletion;
            retryCompletion = null;
            if (retryButton != null)
                retryButton.gameObject.SetActive(false);
            completion?.TrySetResult(true);
        }
    }
}
