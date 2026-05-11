using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Armor;

namespace WizardGrower.UI
{
    public class ArmorInventoryTab : MonoBehaviour
    {
        [SerializeField] private ArmorSlot currentSlot = ArmorSlot.Helmet;
        [SerializeField] private ArmorSlotTabBar slotTabBar;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private WeaponSlotView slotPrefab;
        [SerializeField] private ArmorDetailView detailView;
        [SerializeField] private Button synthesizeAllButton;
        [SerializeField] private TMP_Text emptyLabel;

        private readonly List<WeaponSlotView> slots = new List<WeaponSlotView>();
        private ArmorInventory inventory;
        private ArmorDatabase database;
        private ArmorFusionService fusionService;
        private ArmorDefinition selectedArmor;

        public void Initialize(ArmorInventory inventory, ArmorDatabase database, ArmorFusionService fusionService)
        {
            if (this.inventory != null)
                this.inventory.InventoryChanged -= OnInventoryChanged;
            if (slotTabBar != null)
                slotTabBar.SlotChanged -= OnSlotChanged;
            if (detailView != null)
                detailView.EquipRequested -= OnEquipRequested;

            this.inventory = inventory;
            this.database = database;
            this.fusionService = fusionService;

            if (this.inventory != null)
                this.inventory.InventoryChanged += OnInventoryChanged;
            if (slotTabBar != null)
                slotTabBar.SlotChanged += OnSlotChanged;
            if (detailView != null)
                detailView.EquipRequested += OnEquipRequested;
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
            if (synthesizeAllButton != null)
                synthesizeAllButton.onClick.RemoveListener(OnSynthesizeAll);
        }

        private void OnSlotChanged(ArmorSlot slot)
        {
            currentSlot = slot;
            selectedArmor = null;
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

            IReadOnlyList<ArmorDefinition> row = database.GetRow(currentSlot, WizardGrower.Weapons.WeaponUpperGrade.Common);
            for (int i = 0; i < row.Count; i++)
            {
                ArmorDefinition armor = row[i];
                if (armor == null)
                    continue;

                WeaponSlotView slot = Instantiate(slotPrefab, slotContainer);
                slot.SelectedArmor += OnSlotSelected;
                slot.BindArmor(inventory, armor);
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

        private void OnSlotSelected(ArmorDefinition armor)
        {
            selectedArmor = armor;
            RefreshDetail();
        }

        private void RefreshDetail()
        {
            if (detailView == null)
                return;

            if (selectedArmor == null)
            {
                detailView.Clear();
                return;
            }

            int count = inventory != null ? inventory.GetCount(selectedArmor.armorId) : 0;
            bool equipped = inventory != null && inventory.GetEquippedId(selectedArmor.slot) == selectedArmor.armorId;
            detailView.Show(selectedArmor, count, equipped);
        }

        private void OnEquipRequested(ArmorDefinition armor)
        {
            if (inventory != null && armor != null && inventory.TryEquip(armor.slot, armor.armorId))
                Refresh();
        }

        private void OnSynthesizeAll()
        {
            fusionService?.SynthesizeAll(inventory, database);
            Refresh();
        }
    }
}
