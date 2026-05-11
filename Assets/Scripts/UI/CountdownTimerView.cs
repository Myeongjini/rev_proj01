using TMPro;
using UnityEngine;

namespace WizardGrower.UI
{
    public class CountdownTimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            EnsureUi();
        }

        public void Refresh(float current, float duration)
        {
            EnsureUi();
            if (label != null)
                label.text = $"골드던전 {Mathf.CeilToInt(Mathf.Max(0f, current))}초";
        }

        private void EnsureUi()
        {
            if (label != null)
                return;

            label = GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                return;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(transform, false);
            RectTransform rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            label = labelGo.GetComponent<TMP_Text>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
        }
    }
}
