using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Enhancement;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class WeaponDetailView : MonoBehaviour
    {
        [SerializeField] private TMP_Text infoLabel;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonLabel;
        [SerializeField] private Button enhanceButton;
        [SerializeField] private TMP_Text enhanceButtonLabel;

        private WeaponDefinition currentWeapon;
        private int currentCount;
        private bool currentEquipped;
        private int currentEnhancementLevel;
        private bool currentCanEnhance;

        public event Action<WeaponDefinition> EquipRequested;
        public event Action<WeaponDefinition> EnhanceRequested;

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
            currentWeapon = null;
            currentCount = 0;
            currentEquipped = false;
            currentEnhancementLevel = 0;
            currentCanEnhance = false;
            if (infoLabel != null)
                infoLabel.text = "Select a weapon";
            RefreshButton();
        }

        public void Show(WeaponDefinition weapon, int ownedCount, bool equipped, int enhancementLevel = 0, bool canEnhance = false)
        {
            currentWeapon = weapon;
            currentCount = Mathf.Max(0, ownedCount);
            currentEquipped = equipped;
            currentEnhancementLevel = EnhancementCostCalculator.ClampLevel(enhancementLevel);
            currentCanEnhance = canEnhance;

            if (infoLabel != null)
            {
                if (weapon == null)
                {
                    infoLabel.text = "Select a weapon";
                }
                else
                {
                    WeaponStats s = weapon.statBonuses;
                    infoLabel.text =
                        $"{weapon.displayName} +{currentEnhancementLevel}\n" +
                        $"{WeaponGradeLabels.Display(weapon.upperGrade, weapon.lowerGrade)}  x{currentCount}" +
                        (currentEquipped ? "  장착중" : "") + "\n" +
                        $"Attack +{s.attackDamage:0}  Speed +{s.attackSpeedBonus:0.###}\n" +
                        $"Crit +{s.criticalChance:P0}  Crit Dmg +{s.criticalMultiplier:0.##}\n" +
                        $"Armor Pen +{s.armorPenetration:0}  HP +{s.maxHealth:0}  Mana +{s.maxMana:0}\n" +
                        weapon.flavorText;
                }
            }

            RefreshButton();
        }

        private void RefreshButton()
        {
            bool canEquip = currentWeapon != null && currentCount > 0 && !currentEquipped;
            if (equipButton != null)
                equipButton.interactable = canEquip;
            if (equipButtonLabel != null)
                equipButtonLabel.text = currentEquipped ? "장착중" : "장착";
            if (enhanceButton != null)
                enhanceButton.interactable = currentWeapon != null && currentCount > 0 && currentCanEnhance;
            if (enhanceButtonLabel != null)
                enhanceButtonLabel.text = currentEnhancementLevel >= EnhancementCostCalculator.MaxLevel ? "최대 강화" : "강화";
        }

        private void RequestEquip()
        {
            if (currentWeapon != null && currentCount > 0 && !currentEquipped)
                EquipRequested?.Invoke(currentWeapon);
        }

        private void RequestEnhance()
        {
            if (currentWeapon != null && currentCount > 0)
                EnhanceRequested?.Invoke(currentWeapon);
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
