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

        private void Update()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].RefreshCooldownState(currentMana);
            }
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
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = SkillCastOrchestrator.SlotCount;
                grid.cellSize = new Vector2(64f, 64f);
                grid.spacing = new Vector2(8f, 0f);
                grid.childAlignment = TextAnchor.MiddleCenter;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    Transform existing = slotContainer.childCount > i ? slotContainer.GetChild(i) : null;
                    if (existing != null)
                        slots[i] = existing.GetComponent<SkillBarSlotView>();
                    if (slots[i] == null)
                    {
                        if (slotPrefab == null)
                            continue;

                        SkillBarSlotView slot = Instantiate(slotPrefab, slotContainer);
                        slot.name = $"SkillSlot{i + 1}";
                        slots[i] = slot;
                    }
                }
            }
        }

        private void RefreshAll()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                SkillRuntime runtime = orchestrator != null ? orchestrator.GetSlot(i) : null;
                SkillDefinition skill = runtime != null ? runtime.Definition : null;
                bool locked = orchestrator != null && skill != null && !orchestrator.IsSkillUnlocked(skill);
                int unlockLevel = orchestrator != null ? orchestrator.GetUnlockLevel(skill) : 1;
                if (slots[i] == null)
                    continue;

                slots[i].Bind(i, runtime, OnSlotClicked, locked, unlockLevel);
                slots[i].Refresh(currentMana);
            }
        }

        private void OnSlotChanged(int slotIndex, SkillDefinition _)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length)
                return;

            SkillRuntime runtime = orchestrator != null ? orchestrator.GetSlot(slotIndex) : null;
            SkillDefinition skill = runtime != null ? runtime.Definition : null;
            bool locked = orchestrator != null && skill != null && !orchestrator.IsSkillUnlocked(skill);
            int unlockLevel = orchestrator != null ? orchestrator.GetUnlockLevel(skill) : 1;
            if (slots[slotIndex] == null)
                return;

            slots[slotIndex].Bind(slotIndex, runtime, OnSlotClicked, locked, unlockLevel);
            slots[slotIndex].Refresh(currentMana);
        }

        private void OnManaChanged(float current, float _)
        {
            currentMana = current;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].Refresh(currentMana);
            }
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
