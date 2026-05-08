using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class WeaponInventoryPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private WeaponSlotView slotPrefab;

        private readonly List<WeaponSlotView> slots = new List<WeaponSlotView>();
        private WeaponInventory inventory;
        private WeaponDatabase database;
        private bool visible;

        public void Initialize(WeaponInventory inventory, WeaponDatabase database)
        {
            this.inventory = inventory;
            this.database = database;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (this.inventory != null)
            {
                this.inventory.EquippedChanged += OnEquippedChanged;
                this.inventory.WeaponObtained += OnWeaponObtained;
            }

            Rebuild();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.EquippedChanged -= OnEquippedChanged;
                inventory.WeaponObtained -= OnWeaponObtained;
            }
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
            if (slotContainer == null || slotPrefab == null || database == null || database.weapons == null)
                return;

            for (int i = slotContainer.childCount - 1; i >= 0; i--)
                Destroy(slotContainer.GetChild(i).gameObject);
            slots.Clear();

            for (int i = 0; i < database.weapons.Length; i++)
            {
                WeaponDefinition weapon = database.weapons[i];
                if (weapon == null)
                    continue;

                WeaponSlotView slot = Instantiate(slotPrefab, slotContainer);
                slot.Bind(inventory, weapon, inventory != null && inventory.IsOwned(weapon.weaponId));
                slots.Add(slot);
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] != null)
                    slots[i].Refresh();
        }

        private void OnEquippedChanged(WeaponDefinition weapon)
        {
            Refresh();
        }

        private void OnWeaponObtained(WeaponDefinition weapon)
        {
            Rebuild();
        }
    }
}
