using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class UpgradeDrawerView : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private TMP_Text toggleLabel;
        [SerializeField] private RectTransform panel;
        [SerializeField] private float openY = 0f;
        [SerializeField] private float closeY = -800f;
        [SerializeField] private float animDuration = 0.25f;

        private bool isOpen;

        private void Awake()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
                toggleButton.onClick.AddListener(Toggle);
            }
            ApplyImmediate(false);
        }

        public void Toggle()
        {
            isOpen = !isOpen;
            if (toggleLabel != null)
                toggleLabel.text = isOpen ? "▼ 강화 닫기" : "▲ 강화 열기";
            StopAllCoroutines();
            StartCoroutine(Animate(isOpen ? openY : closeY));
        }

        private IEnumerator Animate(float targetY)
        {
            if (panel == null)
                yield break;

            float elapsed = 0f;
            Vector2 start = panel.anchoredPosition;
            Vector2 end = new Vector2(start.x, targetY);
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                panel.anchoredPosition = Vector2.Lerp(start, end, elapsed / animDuration);
                yield return null;
            }

            panel.anchoredPosition = end;
        }

        private void ApplyImmediate(bool open)
        {
            isOpen = open;
            if (panel != null)
                panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, open ? openY : closeY);
            if (toggleLabel != null)
                toggleLabel.text = open ? "▼ 강화 닫기" : "▲ 강화 열기";
        }
    }
}
