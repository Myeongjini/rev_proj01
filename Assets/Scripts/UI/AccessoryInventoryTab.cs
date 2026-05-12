using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Accessory;
using WizardGrower.Enhancement;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class AccessoryInventoryTab : MonoBehaviour
    {
        [SerializeField] private AccessorySlot currentSlot = AccessorySlot.Ring;
        [SerializeField] private AccessorySlotTabBar slotTabBar;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private WeaponSlotView slotPrefab;
        [SerializeField] private AccessoryDetailView detailView;
        [SerializeField] private Button synthesizeAllButton;
        [SerializeField] private TMP_Text emptyLabel;

        private readonly List<WeaponSlotView> slots = new List<WeaponSlotView>();
        private AccessoryInventory inventory;
        private AccessoryDatabase database;
        private AccessoryFusionService fusionService;
        private EnhancementService enhancementService;
        private EnhancementModal enhancementModal;
        private AccessoryDefinition selectedAccessory;

        public void Initialize(AccessoryInventory inventory, AccessoryDatabase database, AccessoryFusionService fusionService, EnhancementService enhancementService = null, EnhancementModal enhancementModal = null)
        {
            if (this.inventory != null)
                this.inventory.InventoryChanged -= OnInventoryChanged;
            if (slotTabBar != null)
                slotTabBar.SlotChanged -= OnSlotChanged;
            if (detailView != null)
                detailView.EquipRequested -= OnEquipRequested;
            if (detailView != null)
                detailView.EnhanceRequested -= OnEnhanceRequested;

            this.inventory = inventory;
            this.database = database;
            this.fusionService = fusionService;
            this.enhancementService = enhancementService;
            this.enhancementModal = enhancementModal;

            if (this.inventory != null)
                this.inventory.InventoryChanged += OnInventoryChanged;
            if (slotTabBar != null)
                slotTabBar.SlotChanged += OnSlotChanged;
            if (detailView != null)
                detailView.EquipRequested += OnEquipRequested;
            if (detailView != null)
                detailView.EnhanceRequested += OnEnhanceRequested;
            if (synthesizeAllButton != null)
            {
                synthesizeAllButton.onClick.RemoveListener(OnSynthesizeAll);
                synthesizeAllButton.onClick.AddListener(OnSynthesizeAll);
            }

            GridLayoutGroup grid = slotContainer != null ? slotContainer.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
            }

            Rebuild();
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory != null)
                inventory.InventoryChanged -= OnInventoryChanged;
            if (slotTabBar != null)
                slotTabBar.SlotChanged -= OnSlotChanged;
            if (detailView != null)
                detailView.EquipRequested -= OnEquipRequested;
            if (detailView != null)
                detailView.EnhanceRequested -= OnEnhanceRequested;
            if (synthesizeAllButton != null)
                synthesizeAllButton.onClick.RemoveListener(OnSynthesizeAll);
        }

        private void OnSlotChanged(AccessorySlot slot)
        {
            currentSlot = slot;
            selectedAccessory = null;
            Rebuild();
            Refresh();
        }

        private void Rebuild()
        {
            if (slotContainer == null || slotPrefab == null || database == null)
                return;

            for (int i = slotContainer.childCount - 1; i >= 0; i--)
                Destroy(slotContainer.GetChild(i).gameObject);
            slots.Clear();

            IReadOnlyList<AccessoryDefinition> row = database.GetRow(currentSlot, WeaponUpperGrade.Common);
            for (int i = 0; i < row.Count; i++)
            {
                AccessoryDefinition accessory = row[i];
                if (accessory == null)
                    continue;

                WeaponSlotView slot = Instantiate(slotPrefab, slotContainer);
                slot.SelectedAccessory += OnSlotSelected;
                slot.BindAccessory(inventory, accessory);
                slots.Add(slot);
            }
        }

        private void Refresh()
        {
            if (emptyLabel != null)
                emptyLabel.gameObject.SetActive(slots.Count == 0);

            for (int i = 0; i < slots.Count; i++)
                if (slots[i] != null)
                    slots[i].Refresh();
            RefreshDetail();
            if (synthesizeAllButton != null)
                synthesizeAllButton.interactable = fusionService != null && fusionService.CanFuseAny(inventory, database);
        }

        private void OnInventoryChanged()
        {
            Refresh();
        }

        private void OnSlotSelected(AccessoryDefinition accessory)
        {
            selectedAccessory = accessory;
            RefreshDetail();
        }

        private void RefreshDetail()
        {
            if (detailView == null)
                return;

            if (selectedAccessory == null)
            {
                detailView.Clear();
                return;
            }

            int count = inventory != null ? inventory.GetCount(selectedAccessory.accessoryId) : 0;
            bool equipped = inventory != null && inventory.GetEquippedId(selectedAccessory.slot) == selectedAccessory.accessoryId;
            int level = inventory != null ? inventory.GetEnhancementLevel(selectedAccessory.accessoryId) : 0;
            detailView.Show(selectedAccessory, count, equipped, level, enhancementService != null && enhancementService.CanEnhance(EnhancementSlotKind.Accessory, selectedAccessory.accessoryId));
        }

        private void OnEquipRequested(AccessoryDefinition accessory)
        {
            if (inventory != null && accessory != null && inventory.TryEquip(accessory.slot, accessory.accessoryId))
                Refresh();
        }

        private void OnEnhanceRequested(AccessoryDefinition accessory)
        {
            if (accessory == null || enhancementModal == null)
                return;
            enhancementModal.ShowAccessory(accessory, inventory, Refresh);
        }

        private void OnSynthesizeAll()
        {
            fusionService?.SynthesizeAll(inventory, database);
            Refresh();
        }
    }
}
