using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Accessory;
using WizardGrower.Armor;
using WizardGrower.Enhancement;
using WizardGrower.Economy;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class EnhancementModal : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text statsLabel;
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private Button enhanceButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject successVfxPrefab;

        private EnhancementService service;
        private CurrencyWallet wallet;
        private EnhancementSlotKind slotKind;
        private string itemId;
        private string itemName;
        private Func<int> getLevel;
        private Func<string> getStatsText;
        private Action refreshed;
        private bool busy;

        private void Awake()
        {
            ResolveReferences();
            if (enhanceButton != null)
                enhanceButton.onClick.AddListener(Enhance);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Bind(EnhancementService service, CurrencyWallet wallet)
        {
            this.service = service;
            this.wallet = wallet;
            if (this.wallet != null)
                this.wallet.EnhancementStoneChanged += _ => Refresh();
        }

        public void ShowWeapon(WeaponDefinition weapon, WeaponInventory inventory, Action refreshed)
        {
            if (weapon == null || inventory == null)
                return;
            Show(EnhancementSlotKind.Weapon, weapon.weaponId, weapon.displayName,
                () => inventory.GetEnhancementLevel(weapon.weaponId),
                () => FormatWeaponStats(weapon.statBonuses, inventory.GetEnhancementLevel(weapon.weaponId)),
                refreshed);
        }

        public void ShowArmor(ArmorDefinition armor, ArmorInventory inventory, Action refreshed)
        {
            if (armor == null || inventory == null)
                return;
            Show(EnhancementSlotKind.Armor, armor.armorId, armor.displayName,
                () => inventory.GetEnhancementLevel(armor.armorId),
                () => FormatArmorStats(armor.statBonuses, inventory.GetEnhancementLevel(armor.armorId)),
                refreshed);
        }

        public void ShowAccessory(AccessoryDefinition accessory, AccessoryInventory inventory, Action refreshed)
        {
            if (accessory == null || inventory == null)
                return;
            Show(EnhancementSlotKind.Accessory, accessory.accessoryId, accessory.displayName,
                () => inventory.GetEnhancementLevel(accessory.accessoryId),
                () => FormatAccessoryStats(accessory.statBonuses, inventory.GetEnhancementLevel(accessory.accessoryId)),
                refreshed);
        }

        public void Close()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private void Show(EnhancementSlotKind slotKind, string itemId, string itemName, Func<int> getLevel, Func<string> getStatsText, Action refreshed)
        {
            this.slotKind = slotKind;
            this.itemId = itemId;
            this.itemName = itemName;
            this.getLevel = getLevel;
            this.getStatsText = getStatsText;
            this.refreshed = refreshed;
            busy = false;
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            Refresh();
        }

        private async void Enhance()
        {
            if (busy || service == null || string.IsNullOrEmpty(itemId))
                return;

            int level = getLevel != null ? getLevel() : 0;
            busy = true;
            Refresh();
            bool enhanced = await service.TryEnhanceAsync(slotKind, itemId, level);
            busy = false;
            if (enhanced)
            {
                SpawnSuccessVfx();
                refreshed?.Invoke();
            }
            else if (feedbackLabel != null)
            {
                feedbackLabel.text = "강화 조건을 확인해주세요";
            }
            Refresh();
        }

        private void Refresh()
        {
            int level = getLevel != null ? getLevel() : 0;
            int cost = EnhancementCostCalculator.GetCost(level);
            bool capped = level >= EnhancementCostCalculator.MaxLevel;
            bool canAfford = wallet != null && wallet.EnhancementStone >= cost;
            if (titleLabel != null)
                titleLabel.text = $"{itemName} 강화";
            if (levelLabel != null)
                levelLabel.text = capped ? $"+{level} 최대 강화" : $"+{level} → +{level + 1}";
            if (statsLabel != null)
                statsLabel.text = getStatsText != null ? getStatsText() : string.Empty;
            if (costLabel != null)
                costLabel.text = capped ? "최대 강화 도달" : $"강화석 {cost:N0} / 보유 {wallet?.EnhancementStone ?? 0:N0}";
            if (feedbackLabel != null && !busy)
                feedbackLabel.text = capped ? "최대 강화 도달" : canAfford ? string.Empty : "강화석이 부족합니다";
            if (enhanceButton != null)
                enhanceButton.interactable = !busy && !capped && canAfford;
        }

        private string FormatWeaponStats(WeaponStats stats, int level)
        {
            WeaponStats current = WeaponStatComposer.ApplyEnhancement(stats, level);
            WeaponStats next = WeaponStatComposer.ApplyEnhancement(stats, level + 1);
            return $"공격 {current.attackDamage:0.#} → {next.attackDamage:0.#}\n치확 {current.criticalChance:P0} → {next.criticalChance:P0}\nHP {current.maxHealth:0.#} → {next.maxHealth:0.#}";
        }

        private string FormatArmorStats(ArmorStats stats, int level)
        {
            ArmorStats current = ArmorStatComposer.ApplyEnhancement(stats, level);
            ArmorStats next = ArmorStatComposer.ApplyEnhancement(stats, level + 1);
            return $"방어 {current.defense:0.#} → {next.defense:0.#}\n공격 {current.attackDamageBonus:0.#} → {next.attackDamageBonus:0.#}\nHP {current.maxHealth:0.#} → {next.maxHealth:0.#}";
        }

        private string FormatAccessoryStats(AccessoryStats stats, int level)
        {
            AccessoryStats current = AccessoryStatComposer.ApplyEnhancement(stats, level);
            AccessoryStats next = AccessoryStatComposer.ApplyEnhancement(stats, level + 1);
            return $"공격 {current.attackDamage:0.#} → {next.attackDamage:0.#}\n관통 {current.armorPenetration:0.#} → {next.armorPenetration:0.#}\n방어 {current.defense:0.#} → {next.defense:0.#}";
        }

        private void SpawnSuccessVfx()
        {
            if (successVfxPrefab == null)
                return;
            GameObject instance = Instantiate(successVfxPrefab, transform, false);
            Destroy(instance, 0.4f);
        }

        private void ResolveReferences()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
        }
    }
}
