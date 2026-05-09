using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Missions;

namespace WizardGrower.UI
{
    public class MissionRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Slider slider;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimLabel;

        private string missionId;
        private System.Action<string> claimClicked;

        private void Awake()
        {
            EnsureUi();
        }

        public void Bind(string missionId, string description, int progress, int target, bool complete, System.Action<string> claimClicked)
        {
            EnsureUi();
            this.missionId = missionId;
            this.claimClicked = claimClicked;
            label.text = $"{description} [{progress}/{target}]";
            slider.minValue = 0;
            slider.maxValue = Mathf.Max(1, target);
            slider.value = Mathf.Clamp(progress, 0, Mathf.Max(1, target));
            claimButton.interactable = complete;
            claimButton.GetComponent<Image>().color = complete ? new Color(0.18f, 0.38f, 0.85f, 1f) : new Color(0.28f, 0.28f, 0.30f, 0.85f);
            claimLabel.text = "보상";
        }

        private void EnsureUi()
        {
            Image bg = GetComponent<Image>();
            if (bg == null)
                bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.13f, 0.95f);

            if (label == null)
                label = CreateText("Label", new Vector2(0f, 0.44f), new Vector2(0.72f, 1f), 14f, TextAlignmentOptions.Left);
            if (slider == null)
            {
                GameObject go = new GameObject("Progress", typeof(RectTransform), typeof(Slider));
                go.transform.SetParent(transform, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.08f);
                rect.anchorMax = new Vector2(0.72f, 0.36f);
                rect.offsetMin = new Vector2(12f, 4f);
                rect.offsetMax = new Vector2(-8f, -4f);
                slider = go.GetComponent<Slider>();
            }
            if (claimButton == null)
            {
                GameObject go = new GameObject("ClaimButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.76f, 0.18f);
                rect.anchorMax = new Vector2(0.98f, 0.82f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                claimButton = go.GetComponent<Button>();
                claimLabel = CreateText("Label", Vector2.zero, Vector2.one, 14f, TextAlignmentOptions.Center);
                claimLabel.transform.SetParent(go.transform, false);
            }
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => claimClicked?.Invoke(missionId));
        }

        private TMP_Text CreateText(string name, Vector2 min, Vector2 max, float size, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(12f, 2f);
            rect.offsetMax = new Vector2(-8f, -2f);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.alignment = alignment;
            text.fontSize = size;
            text.color = Color.white;
            return text;
        }
    }
}
