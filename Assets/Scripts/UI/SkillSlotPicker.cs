using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Skills;

namespace WizardGrower.UI
{
    public class SkillSlotPicker : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Transform buttonContainer;

        private SkillCastOrchestrator orchestrator;
        private SkillDefinition pendingSkill;
        private readonly Button[] slotButtons = new Button[SkillCastOrchestrator.SlotCount];

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void Bind(SkillCastOrchestrator orchestrator)
        {
            this.orchestrator = orchestrator;
        }

        public void Show(SkillDefinition skill)
        {
            pendingSkill = skill;
            EnsureUi();
            Refresh();
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void Refresh()
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                TMP_Text label = slotButtons[i].GetComponentInChildren<TMP_Text>(true);
                string current = orchestrator != null ? orchestrator.GetEquippedSkillId(i) : string.Empty;
                label.text = string.IsNullOrEmpty(current) ? $"슬롯 {i + 1}\n비어있음" : $"슬롯 {i + 1}\n{current}";
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() =>
                {
                    if (orchestrator != null && pendingSkill != null)
                        orchestrator.EquipSkill(index, pendingSkill.skillId);
                    Hide();
                });
            }
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
            bg.color = new Color(0f, 0f, 0f, 0.70f);

            if (buttonContainer == null)
            {
                GameObject container = new GameObject("Slots", typeof(RectTransform), typeof(GridLayoutGroup));
                container.transform.SetParent(transform, false);
                RectTransform rect = container.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.12f, 0.36f);
                rect.anchorMax = new Vector2(0.88f, 0.62f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 5;
                grid.cellSize = new Vector2(110f, 76f);
                grid.spacing = new Vector2(10f, 0f);
                grid.childAlignment = TextAnchor.MiddleCenter;
                buttonContainer = container.transform;
            }

            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] != null)
                    continue;
                GameObject go = new GameObject($"SlotButton{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(buttonContainer, false);
                go.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.30f, 0.96f);
                slotButtons[i] = go.GetComponent<Button>();
                GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                TMP_Text label = labelGo.GetComponent<TMP_Text>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 12f;
                label.color = Color.white;
                ApplyProjectFont(label);
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

        private void SetVisible(bool visible)
        {
            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }
            gameObject.SetActive(visible);
        }
    }
}
