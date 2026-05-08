using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class WeaponInventoryPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private WeaponSlotView slotPrefab;
        [SerializeField] private WeaponDetailView detailView;

        private readonly List<WeaponSlotView> slots = new List<WeaponSlotView>();
        private WeaponInventory inventory;
        private WeaponDatabase database;
        private bool visible;
        private WeaponDefinition selectedWeapon;

        public void Initialize(WeaponInventory inventory, WeaponDatabase database)
        {
            if (this.inventory != null)
            {
                this.inventory.EquippedChanged -= OnEquippedChanged;
                this.inventory.InventoryChanged -= OnInventoryChanged;
            }
            if (detailView != null)
                detailView.EquipRequested -= OnEquipRequested;

            this.inventory = inventory;
            this.database = database;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (this.inventory != null)
            {
                this.inventory.EquippedChanged += OnEquippedChanged;
                this.inventory.InventoryChanged += OnInventoryChanged;
            }
            if (detailView != null)
                detailView.EquipRequested += OnEquipRequested;
            GridLayoutGroup grid = slotContainer != null ? slotContainer.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
            }

            Rebuild();
            if (detailView != null)
                detailView.Clear();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.EquippedChanged -= OnEquippedChanged;
                inventory.InventoryChanged -= OnInventoryChanged;
            }
            if (detailView != null)
                detailView.EquipRequested -= OnEquipRequested;
        }

        public void Toggle()
        {
            SetVisible(!visible);
        }

        public void SetVisible(bool show)
        {
            visible = show;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.interactable = show;
                canvasGroup.blocksRaycasts = show;
            }
            else
            {
                gameObject.SetActive(show);
            }

            if (show)
                Refresh();
        }

        private void Rebuild()
        {
            if (slotContainer == null || slotPrefab == null || database == null)
                return;

            for (int i = slotContainer.childCount - 1; i >= 0; i--)
                Destroy(slotContainer.GetChild(i).gameObject);
            slots.Clear();

            IReadOnlyList<WeaponDefinition> ordered = database.OrderedWeapons;
            for (int i = 0; i < ordered.Count; i++)
            {
                WeaponDefinition weapon = ordered[i];
                if (weapon == null)
                    continue;

                WeaponSlotView slot = Instantiate(slotPrefab, slotContainer);
                slot.Selected += OnSlotSelected;
                slot.Bind(inventory, weapon, inventory != null && inventory.Has(weapon.weaponId));
                slots.Add(slot);
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] != null)
                    slots[i].Refresh();
            RefreshDetail();
        }

        private void OnEquippedChanged(WeaponDefinition weapon)
        {
            Refresh();
        }

        private void OnInventoryChanged()
        {
            Refresh();
        }

        private void OnSlotSelected(WeaponDefinition weapon)
        {
            selectedWeapon = weapon;
            RefreshDetail();
        }

        private void RefreshDetail()
        {
            if (detailView == null)
                return;

            if (selectedWeapon == null)
            {
                detailView.Clear();
                return;
            }

            int count = inventory != null ? inventory.GetCount(selectedWeapon.weaponId) : 0;
            bool equipped = inventory != null && inventory.EquippedWeaponId == selectedWeapon.weaponId;
            detailView.Show(selectedWeapon, count, equipped);
        }

        private void OnEquipRequested(WeaponDefinition weapon)
        {
            if (inventory != null && weapon != null && inventory.TryEquip(weapon.weaponId))
                Refresh();
        }
    }
}
