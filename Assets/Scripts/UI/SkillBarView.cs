using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Player;
using WizardGrower.Skills;

namespace WizardGrower.UI
{
    public class SkillBarView : MonoBehaviour
    {
        [SerializeField] private SkillBarSlotView slotPrefab;
        [SerializeField] private Transform slotContainer;

        private readonly SkillBarSlotView[] slots = new SkillBarSlotView[SkillCastOrchestrator.SlotCount];
        private SkillCastOrchestrator orchestrator;
        private PlayerMana mana;
        private float currentMana;

        private void Awake()
        {
            EnsureLayout();
        }

        public void Bind(SkillCastOrchestrator orchestrator, PlayerMana mana)
        {
            if (this.orchestrator != null)
                this.orchestrator.SlotChanged -= OnSlotChanged;
            if (this.mana != null)
                this.mana.Changed -= OnManaChanged;

            this.orchestrator = orchestrator;
            this.mana = mana;
            currentMana = mana != null ? mana.Current : 0f;

            EnsureLayout();
            if (this.orchestrator != null)
                this.orchestrator.SlotChanged += OnSlotChanged;
            if (this.mana != null)
                this.mana.Changed += OnManaChanged;

            RefreshAll();
        }

        private void EnsureLayout()
        {
            if (slotContainer == null)
                slotContainer = transform;

            GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = slotContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = SkillCastOrchestrator.SlotCount;
            grid.cellSize = new Vector2(64f, 64f);
            grid.spacing = new Vector2(8f, 0f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    Transform existing = slotContainer.childCount > i ? slotContainer.GetChild(i) : null;
                    if (existing != null)
                        slots[i] = existing.GetComponent<SkillBarSlotView>();
                    if (slots[i] == null)
                    {
                        GameObject go = slotPrefab != null
                            ? Instantiate(slotPrefab.gameObject, slotContainer)
                            : new GameObject($"SkillSlot{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(SkillBarSlotView));
                        go.transform.SetParent(slotContainer, false);
                        slots[i] = go.GetComponent<SkillBarSlotView>();
                    }
                }
            }
        }

        private void RefreshAll()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                SkillRuntime runtime = orchestrator != null ? orchestrator.GetSlot(i) : null;
                slots[i].Bind(i, runtime, OnSlotClicked);
                slots[i].Refresh(currentMana);
            }
        }

        private void OnSlotChanged(int slotIndex, SkillDefinition _)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length)
                return;

            slots[slotIndex].Bind(slotIndex, orchestrator != null ? orchestrator.GetSlot(slotIndex) : null, OnSlotClicked);
            slots[slotIndex].Refresh(currentMana);
        }

        private void OnManaChanged(float current, float _)
        {
            currentMana = current;
            for (int i = 0; i < slots.Length; i++)
                slots[i].Refresh(currentMana);
        }

        private void OnSlotClicked(int slotIndex)
        {
            orchestrator?.TryManualCast(slotIndex);
        }

        private void OnDestroy()
        {
            if (orchestrator != null)
                orchestrator.SlotChanged -= OnSlotChanged;
            if (mana != null)
                mana.Changed -= OnManaChanged;
        }
    }
}
