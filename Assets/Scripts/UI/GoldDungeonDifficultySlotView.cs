using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Dungeons;

namespace WizardGrower.UI
{
    public class GoldDungeonDifficultySlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;
        [SerializeField] private Image background;

        private int index;

        public int Index => index;

        private void Awake()
        {
            EnsureUi();
        }

        public void Bind(int index, GoldDungeonDifficulty difficulty, bool selected)
        {
            bool unlocked = difficulty != null && difficulty.unlockPlayerLevel <= 0;
            Bind(index, difficulty, selected, unlocked);
        }

        public void Bind(int index, GoldDungeonDifficulty difficulty, bool selected, bool unlocked)
        {
            EnsureUi();
            this.index = index;
            if (label != null)
                label.text = unlocked ? $"Lv{difficulty.level}" : $"Lv{difficulty?.level ?? index + 1}\nLv {difficulty?.unlockPlayerLevel ?? 5} 필요";
            if (button != null)
                button.interactable = unlocked;
            if (background != null)
                background.color = !unlocked
                    ? new Color(0.22f, 0.22f, 0.24f, 0.9f)
                    : selected ? new Color(0.32f, 0.58f, 1f, 0.96f) : new Color(0.10f, 0.14f, 0.22f, 0.92f);
        }

        public void AddClickListener(UnityEngine.Events.UnityAction action)
        {
            EnsureUi();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void EnsureUi()
        {
            if (background == null)
                background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            if (button == null)
                button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            if (label == null)
            {
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
                    label.fontSize = 16f;
                    label.fontStyle = FontStyles.Bold;
                    label.color = Color.white;
                    ApplyProjectFont(label);
                }
            }
        }

        private void ApplyProjectFont(TMP_Text text)
        {
            if (text == null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
                return;

            TMP_Text[] labels = canvas.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].font != null && labels[i].font.name.Contains("Nanum"))
                {
                    text.font = labels[i].font;
                    return;
                }
            }
        }
    }
}
