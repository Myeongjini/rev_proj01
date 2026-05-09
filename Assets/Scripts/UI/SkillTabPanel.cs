using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Skills;

namespace WizardGrower.UI
{
    public class SkillTabPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private SkillCardView cardPrefab;
        [SerializeField] private SkillSlotPicker slotPicker;

        private SkillCastOrchestrator orchestrator;
        private bool isOpen;

        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            EnsureUi();
            Close();
        }

        public void Bind(SkillCastOrchestrator orchestrator)
        {
            if (this.orchestrator != null)
                this.orchestrator.SlotChanged -= OnSlotChanged;
            this.orchestrator = orchestrator;
            if (this.orchestrator != null)
                this.orchestrator.SlotChanged += OnSlotChanged;
            if (slotPicker != null)
                slotPicker.Bind(orchestrator);
            Refresh();
        }

        public void Open()
        {
            EnsureUi();
            isOpen = true;
            gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            Refresh();
            OpenStateChanged?.Invoke(true);
        }

        public void Close()
        {
            isOpen = false;
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            if (slotPicker != null)
                slotPicker.Hide();
            gameObject.SetActive(false);
            OpenStateChanged?.Invoke(false);
        }

        private void Refresh()
        {
            if (cardContainer == null)
                return;

            for (int i = cardContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = cardContainer.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            if (orchestrator == null)
                return;

            for (int i = 0; i < orchestrator.OwnedSkills.Count; i++)
            {
                SkillDefinition skill = orchestrator.OwnedSkills[i];
                if (skill == null)
                    continue;

                SkillCardView card = cardPrefab != null
                    ? Instantiate(cardPrefab, cardContainer)
                    : new GameObject("SkillCard", typeof(RectTransform), typeof(Image), typeof(SkillCardView)).GetComponent<SkillCardView>();
                if (card.transform.parent != cardContainer)
                    card.transform.SetParent(cardContainer, false);
                RectTransform rect = card.transform as RectTransform;
                if (rect != null)
                    rect.sizeDelta = new Vector2(620f, 116f);
                card.Bind(skill, FindEquippedSlot(skill.skillId), ShowPicker, Unequip);
            }
        }

        private int FindEquippedSlot(string skillId)
        {
            if (orchestrator == null)
                return -1;

            for (int i = 0; i < SkillCastOrchestrator.SlotCount; i++)
            {
                if (orchestrator.GetEquippedSkillId(i) == skillId)
                    return i;
            }
            return -1;
        }

        private void ShowPicker(SkillDefinition skill)
        {
            if (slotPicker != null)
                slotPicker.Show(skill);
        }

        private void Unequip(SkillDefinition skill)
        {
            int slot = skill != null ? FindEquippedSlot(skill.skillId) : -1;
            if (slot >= 0)
                orchestrator.EquipSkill(slot, string.Empty);
            Refresh();
        }

        private void OnSlotChanged(int _, SkillDefinition __)
        {
            if (isOpen)
                Refresh();
        }

        private void EnsureUi()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();

            Image bg = GetComponent<Image>();
            if (bg == null)
                bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);

            if (closeButton == null)
                closeButton = CreateButton("CloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -30f), new Vector2(48f, 44f));
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);

            if (cardContainer == null)
            {
                GameObject container = new GameObject("Cards", typeof(RectTransform), typeof(VerticalLayoutGroup));
                container.transform.SetParent(transform, false);
                RectTransform rect = container.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -10f);
                rect.sizeDelta = new Vector2(660f, -80f);
                VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                cardContainer = container.transform;
            }

            if (slotPicker == null)
            {
                GameObject picker = new GameObject("SkillSlotPicker", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(SkillSlotPicker));
                picker.transform.SetParent(transform, false);
                RectTransform rect = picker.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                slotPicker = picker.GetComponent<SkillSlotPicker>();
            }
        }

        private Button CreateButton(string name, string text, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.36f, 1f);
            Button button = go.GetComponent<Button>();
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TMP_Text label = labelGo.GetComponent<TMP_Text>();
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18f;
            label.color = Color.white;
            ApplyProjectFont(label);
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

        private void OnDestroy()
        {
            if (orchestrator != null)
                orchestrator.SlotChanged -= OnSlotChanged;
        }
    }
}
