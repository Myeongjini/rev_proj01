using System.Collections.Generic;
using System;
using System.Collections;
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
        [SerializeField] private Button synthesizeAllButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private WeaponFusionResultView fusionResultView;
        [SerializeField] private RectTransform drawerRoot;
        [SerializeField] private float openY = 0f;
        [SerializeField] private float closeY = -760f;
        [SerializeField] private float animDuration = 0.25f;

        private readonly List<WeaponSlotView> slots = new List<WeaponSlotView>();
        private WeaponInventory inventory;
        private WeaponDatabase database;
        private WeaponFusionService fusionService;
        private bool visible;
        private WeaponDefinition selectedWeapon;
        private Coroutine slideRoutine;
        public bool IsVisible => visible;
        public event Action<bool> OpenStateChanged;

        public void Initialize(WeaponInventory inventory, WeaponDatabase database, WeaponFusionService fusionService = null)
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
            this.fusionService = fusionService ?? new WeaponFusionService();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (drawerRoot == null)
                drawerRoot = transform as RectTransform;

            if (this.inventory != null)
            {
                this.inventory.EquippedChanged += OnEquippedChanged;
                this.inventory.InventoryChanged += OnInventoryChanged;
            }
            if (detailView != null)
                detailView.EquipRequested += OnEquipRequested;
            if (synthesizeAllButton != null)
            {
                synthesizeAllButton.onClick.RemoveListener(OnSynthesizeAll);
                synthesizeAllButton.onClick.AddListener(OnSynthesizeAll);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
            GridLayoutGroup grid = slotContainer != null ? slotContainer.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
            }

            Rebuild();
            if (detailView != null)
                detailView.Clear();
            RefreshFusionButton();
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
            if (synthesizeAllButton != null)
                synthesizeAllButton.onClick.RemoveListener(OnSynthesizeAll);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Toggle()
        {
            SetVisible(!visible);
        }

        public void Open()
        {
            SetVisible(true);
        }

        public void Close()
        {
            SetVisible(false);
        }

        public void SetVisible(bool show)
        {
            if (visible == show && (canvasGroup == null || canvasGroup.alpha == (show ? 1f : 0f)))
                return;

            visible = show;
            if (canvasGroup != null)
            {
                canvasGroup.interactable = show;
                canvasGroup.blocksRaycasts = show;
                if (show)
                    canvasGroup.alpha = 1f;
            }
            else
            {
                gameObject.SetActive(show);
            }

            if (slideRoutine != null)
                StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(Animate(show ? openY : closeY, show));

            if (show)
                Refresh();
            OpenStateChanged?.Invoke(show);
        }

        private IEnumerator Animate(float targetY, bool open)
        {
            if (drawerRoot == null)
                yield break;

            float elapsed = 0f;
            Vector2 start = drawerRoot.anchoredPosition;
            Vector2 end = new Vector2(start.x, targetY);
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                drawerRoot.anchoredPosition = Vector2.Lerp(start, end, elapsed / animDuration);
                yield return null;
            }

            drawerRoot.anchoredPosition = end;
            if (!open && canvasGroup != null)
                canvasGroup.alpha = 0f;
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
            RefreshFusionButton();
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

        private void OnSynthesizeAll()
        {
            if (fusionService == null || inventory == null || database == null)
                return;

            IReadOnlyList<WeaponFusionResult> results = fusionService.SynthesizeAll(inventory, database);
            if (fusionResultView != null)
                fusionResultView.Show(results, database);
            Refresh();
        }

        private void RefreshFusionButton()
        {
            if (synthesizeAllButton != null)
                synthesizeAllButton.interactable = fusionService != null && fusionService.CanFuseAny(inventory, database);
        }
    }
}
