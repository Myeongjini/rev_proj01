using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Accessory;
using WizardGrower.Enhancement;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class AccessoryDetailView : MonoBehaviour
    {
        [SerializeField] private TMP_Text infoLabel;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonLabel;
        [SerializeField] private Button enhanceButton;
        [SerializeField] private TMP_Text enhanceButtonLabel;

        private AccessoryDefinition currentAccessory;
        private int currentCount;
        private bool currentEquipped;
        private int currentEnhancementLevel;
        private bool currentCanEnhance;

        public event Action<AccessoryDefinition> EquipRequested;
        public event Action<AccessoryDefinition> EnhanceRequested;

        private void Awake()
        {
            ResolveReferences();
            if (equipButton != null)
                equipButton.onClick.AddListener(RequestEquip);
            if (enhanceButton != null)
                enhanceButton.onClick.AddListener(RequestEnhance);
        }

        public void Clear()
        {
            currentAccessory = null;
            currentCount = 0;
            currentEquipped = false;
            currentEnhancementLevel = 0;
            currentCanEnhance = false;
            if (infoLabel != null)
                infoLabel.text = "장신구를 선택하세요";
            RefreshButton();
        }

        public void Show(AccessoryDefinition accessory, int ownedCount, bool equipped, int enhancementLevel = 0, bool canEnhance = false)
        {
            currentAccessory = accessory;
            currentCount = Mathf.Max(0, ownedCount);
            currentEquipped = equipped;
            currentEnhancementLevel = EnhancementCostCalculator.ClampLevel(enhancementLevel);
            currentCanEnhance = canEnhance;

            if (infoLabel != null)
            {
                if (accessory == null)
                {
                    infoLabel.text = "장신구를 선택하세요";
                }
                else
                {
                    AccessoryStats s = accessory.statBonuses;
                    infoLabel.text =
                        $"{accessory.displayName} +{currentEnhancementLevel}\n" +
                        $"{AccessorySlotTabBar.SlotKo(accessory.slot)} / {WeaponGradeLabels.Display(accessory.upperGrade, accessory.lowerGrade)}  x{currentCount}" +
                        (currentEquipped ? "  장착중" : "") + "\n" +
                        $"공격 +{s.attackDamage:0.#}  관통 +{s.armorPenetration:0.#}\n" +
                        $"치확 +{s.criticalChance:P0}  치피 +{s.criticalMultiplier:0.##}\n" +
                        $"HP +{s.maxHealth:0.#}  Mana +{s.maxMana:0.#}\n" +
                        accessory.flavorText;
                }
            }

            RefreshButton();
        }

        private void RefreshButton()
        {
            bool canEquip = currentAccessory != null && currentCount > 0 && !currentEquipped;
            if (equipButton != null)
                equipButton.interactable = canEquip;
            if (equipButtonLabel != null)
                equipButtonLabel.text = currentEquipped ? "장착중" : "장착";
            if (enhanceButton != null)
                enhanceButton.interactable = currentAccessory != null && currentCount > 0 && currentCanEnhance;
            if (enhanceButtonLabel != null)
                enhanceButtonLabel.text = currentEnhancementLevel >= EnhancementCostCalculator.MaxLevel ? "최대 강화" : "강화";
        }

        private void RequestEquip()
        {
            if (currentAccessory != null && currentCount > 0 && !currentEquipped)
                EquipRequested?.Invoke(currentAccessory);
        }

        private void RequestEnhance()
        {
            if (currentAccessory != null && currentCount > 0)
                EnhanceRequested?.Invoke(currentAccessory);
        }

        private void ResolveReferences()
        {
            if (enhanceButton == null)
                enhanceButton = FindButton("EnhanceButton");
            if (enhanceButtonLabel == null && enhanceButton != null)
                enhanceButtonLabel = enhanceButton.GetComponentInChildren<TMP_Text>(true);
        }

        private Button FindButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null && buttons[i].name == objectName)
                    return buttons[i];
            return null;
        }
    }
}
