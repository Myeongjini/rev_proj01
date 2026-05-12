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
        [SerializeField] private Image background;

        private SkillRuntime runtime;
        private System.Action<int> clicked;
        private int slotIndex;
        private bool locked;
        private int unlockLevel = 1;
        private float currentMana;
        private float cooldownRemaining;
        private bool projectFontApplied;

        private void Awake()
        {
            ResolveReferences();
        }

        public void Bind(int slotIndex, SkillRuntime runtime, System.Action<int> clicked, bool locked = false, int unlockLevel = 1)
        {
            if (this.runtime != null)
                this.runtime.CooldownChanged -= OnCooldownChanged;

            ResolveReferences();
            this.slotIndex = slotIndex;
            this.runtime = runtime;
            this.clicked = clicked;
            this.locked = locked;
            this.unlockLevel = Mathf.Max(1, unlockLevel);
            cooldownRemaining = this.runtime != null ? this.runtime.CooldownRemaining : 0f;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => this.clicked?.Invoke(this.slotIndex));
            }

            if (this.runtime != null)
                this.runtime.CooldownChanged += OnCooldownChanged;

            Refresh(currentMana);
        }

        public void Refresh(float currentMana)
        {
            ResolveReferences();
            this.currentMana = currentMana;
            SkillDefinition skill = runtime != null ? runtime.Definition : null;
            bool hasSkill = skill != null;

            if (iconImage != null)
            {
                iconImage.enabled = hasSkill && skill.icon != null;
                iconImage.sprite = hasSkill ? skill.icon : null;
                iconImage.color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.18f);
            }

            RefreshLabel();

            bool insufficient = hasSkill && currentMana < skill.manaCost;
            if (lockOverlay != null)
                lockOverlay.enabled = locked || insufficient;

            if (button != null)
                button.interactable = hasSkill && !locked && !insufficient && cooldownRemaining <= 0f;

            OnCooldownChanged(runtime != null ? runtime.CooldownRemaining : 0f);
        }

        public void RefreshCooldownState(float currentMana)
        {
            this.currentMana = currentMana;
            OnCooldownChanged(runtime != null ? runtime.CooldownRemaining : 0f);
        }

        private void OnCooldownChanged(float remaining)
        {
            cooldownRemaining = Mathf.Max(0f, remaining);

            float duration = runtime != null ? runtime.CooldownDuration : 1f;
            if (cooldownFill != null)
            {
                cooldownFill.fillAmount = cooldownRemaining > 0f ? Mathf.Clamp01(cooldownRemaining / duration) : 0f;
                cooldownFill.enabled = cooldownFill.fillAmount > 0f;
            }

            SkillDefinition skill = runtime != null ? runtime.Definition : null;
            bool insufficient = skill != null && currentMana < skill.manaCost;
            if (button != null)
                button.interactable = skill != null && !locked && !insufficient && cooldownRemaining <= 0f;
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (label == null)
                return;

            if (!projectFontApplied)
            {
                ApplyProjectFont(label);
                projectFontApplied = true;
            }
            SkillDefinition skill = runtime != null ? runtime.Definition : null;
            if (locked)
            {
                label.text = $"잠금\nLv {unlockLevel}";
                return;
            }

            if (skill == null)
            {
                label.text = "-";
                return;
            }

            label.text = cooldownRemaining > 0f
                ? $"{cooldownRemaining:0.0}s"
                : skill.displayName;
        }

        private void ResolveReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (background == null)
                background = GetComponent<Image>();
            if (background != null)
                background.color = new Color(0.08f, 0.10f, 0.14f, 0.94f);

            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null || images[i] == background)
                    continue;
                if (iconImage == null && images[i].name == "Icon")
                    iconImage = images[i];
                else if (cooldownFill == null && images[i].name == "Cooldown")
                    cooldownFill = images[i];
                else if (lockOverlay == null && images[i].name == "ManaLock")
                    lockOverlay = images[i];
            }

            if (cooldownFill != null)
            {
                cooldownFill.type = Image.Type.Filled;
                cooldownFill.fillMethod = Image.FillMethod.Radial360;
                cooldownFill.fillOrigin = (int)Image.Origin360.Top;
                cooldownFill.raycastTarget = false;
            }
            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);
            if (lockOverlay != null)
                lockOverlay.raycastTarget = false;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
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
