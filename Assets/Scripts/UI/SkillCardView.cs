using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Skills;

namespace WizardGrower.UI
{
    public class SkillCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text detailLabel;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Image background;

        private SkillDefinition skill;
        private bool unlocked = true;
        private System.Action<SkillDefinition> equipClicked;
        private System.Action<SkillDefinition> unequipClicked;

        private void Awake()
        {
            EnsureUi();
        }

        public void Bind(SkillDefinition skill, int equippedSlot, bool unlocked, int unlockLevel, System.Action<SkillDefinition> equipClicked, System.Action<SkillDefinition> unequipClicked)
        {
            EnsureUi();
            this.skill = skill;
            this.unlocked = unlocked;
            this.equipClicked = equipClicked;
            this.unequipClicked = unequipClicked;

            string equipped = !unlocked ? $"🔒 Lv {unlockLevel} 필요" : (equippedSlot >= 0 ? $"슬롯 {equippedSlot + 1} 장착됨" : "장착 가능");
            ApplyProjectFont(titleLabel);
            ApplyProjectFont(detailLabel);
            ApplyButtonFont(equipButton);
            ApplyButtonFont(unequipButton);
            titleLabel.text = skill != null ? $"{skill.displayName}  {equipped}" : "-";
            detailLabel.text = skill != null
                ? $"{(!unlocked ? $"🔒 Lv {unlockLevel} 필요\n" : string.Empty)}마나 {skill.manaCost:0} / 쿨 {skill.cooldownSeconds:0.#}초 / 피해 {skill.damageCoefficient:0.#}x\n{skill.flavorText}"
                : string.Empty;
            if (background != null)
                background.color = unlocked ? new Color(0.08f, 0.09f, 0.13f, 0.95f) : new Color(0.05f, 0.05f, 0.06f, 0.92f);
            equipButton.interactable = unlocked;
            unequipButton.interactable = equippedSlot >= 0;
        }

        private void EnsureUi()
        {
            background = GetComponent<Image>();
            if (background == null)
                background = gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.13f, 0.95f);

            if (titleLabel == null)
                titleLabel = CreateText("Title", new Vector2(0f, 0.55f), new Vector2(1f, 1f), 16f, FontStyles.Bold);
            if (detailLabel == null)
                detailLabel = CreateText("Detail", new Vector2(0f, 0.18f), new Vector2(1f, 0.62f), 12f, FontStyles.Normal);
            if (equipButton == null)
                equipButton = CreateButton("EquipButton", "장착", new Vector2(0.62f, 0f), new Vector2(0.80f, 0.2f));
            if (unequipButton == null)
                unequipButton = CreateButton("UnequipButton", "해제", new Vector2(0.82f, 0f), new Vector2(1f, 0.2f));

            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(() => equipClicked?.Invoke(skill));
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(() => unequipClicked?.Invoke(skill));
        }

        private TMP_Text CreateText(string name, Vector2 min, Vector2 max, float size, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(10f, 4f);
            rect.offsetMax = new Vector2(-10f, -4f);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;
            ApplyProjectFont(text);
            return text;
        }

        private Button CreateButton(string name, string text, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.16f, 0.33f, 0.62f, 0.95f);
            Button button = go.GetComponent<Button>();
            TMP_Text label = CreateText("Label", Vector2.zero, Vector2.one, 12f, FontStyles.Bold);
            label.transform.SetParent(go.transform, false);
            label.alignment = TextAlignmentOptions.Center;
            label.text = text;
            return button;
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

        private void ApplyButtonFont(Button button)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            ApplyProjectFont(label);
        }
    }
}
