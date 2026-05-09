using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Skills;

namespace WizardGrower.UI
{
    public class SkillBarSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownFill;
        [SerializeField] private Image lockOverlay;
        [SerializeField] private TMP_Text label;

        private SkillRuntime runtime;
        private System.Action<int> clicked;
        private int slotIndex;

        private void Awake()
        {
            EnsureUi();
        }

        public void Bind(int slotIndex, SkillRuntime runtime, System.Action<int> clicked)
        {
            if (this.runtime != null)
                this.runtime.CooldownChanged -= OnCooldownChanged;

            EnsureUi();
            this.slotIndex = slotIndex;
            this.runtime = runtime;
            this.clicked = clicked;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => this.clicked?.Invoke(this.slotIndex));
            }

            if (this.runtime != null)
                this.runtime.CooldownChanged += OnCooldownChanged;

            Refresh(0f);
        }

        public void Refresh(float currentMana)
        {
            EnsureUi();
            SkillDefinition skill = runtime != null ? runtime.Definition : null;
            bool hasSkill = skill != null;

            if (iconImage != null)
            {
                iconImage.enabled = hasSkill && skill.icon != null;
                iconImage.sprite = hasSkill ? skill.icon : null;
                iconImage.color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.18f);
            }

            if (label != null)
            {
                ApplyProjectFont(label);
                label.text = hasSkill ? skill.displayName : "-";
            }

            bool insufficient = hasSkill && currentMana < skill.manaCost;
            if (lockOverlay != null)
                lockOverlay.enabled = insufficient;

            if (button != null)
                button.interactable = hasSkill;

            OnCooldownChanged(runtime != null ? runtime.CooldownRemaining : 0f);
        }

        private void OnCooldownChanged(float remaining)
        {
            if (cooldownFill == null)
                return;

            float duration = runtime != null ? runtime.CooldownDuration : 1f;
            cooldownFill.fillAmount = remaining > 0f ? Mathf.Clamp01(remaining / duration) : 0f;
            cooldownFill.enabled = cooldownFill.fillAmount > 0f;
        }

        private void EnsureUi()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();

            Image background = GetComponent<Image>();
            if (background == null)
                background = gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.10f, 0.14f, 0.94f);

            if (iconImage == null)
                iconImage = CreateImage("Icon", new Color(0.2f, 0.35f, 0.5f, 0.6f));
            if (cooldownFill == null)
            {
                cooldownFill = CreateImage("Cooldown", new Color(0f, 0f, 0f, 0.58f));
                cooldownFill.type = Image.Type.Filled;
                cooldownFill.fillMethod = Image.FillMethod.Radial360;
                cooldownFill.fillOrigin = (int)Image.Origin360.Top;
                cooldownFill.raycastTarget = false;
            }
            if (lockOverlay == null)
            {
                lockOverlay = CreateImage("ManaLock", new Color(0f, 0f, 0f, 0.42f));
                lockOverlay.raycastTarget = false;
            }
            if (label == null)
                label = CreateLabel();
        }

        private Image CreateImage(string objectName, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        private TMP_Text CreateLabel()
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(0f, 10f);
            rect.sizeDelta = new Vector2(0f, 24f);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 11f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 7f;
            text.fontSizeMax = 11f;
            text.color = Color.white;
            ApplyProjectFont(text);
            return text;
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

        private void OnDestroy()
        {
            if (runtime != null)
                runtime.CooldownChanged -= OnCooldownChanged;
        }
    }
}
