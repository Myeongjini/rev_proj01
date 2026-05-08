using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class WeaponDetailView : MonoBehaviour
    {
        [SerializeField] private TMP_Text infoLabel;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonLabel;

        private WeaponDefinition currentWeapon;
        private int currentCount;
        private bool currentEquipped;

        public event Action<WeaponDefinition> EquipRequested;

        private void Awake()
        {
            if (equipButton != null)
                equipButton.onClick.AddListener(RequestEquip);
        }

        public void Clear()
        {
            currentWeapon = null;
            currentCount = 0;
            currentEquipped = false;
            if (infoLabel != null)
                infoLabel.text = "Select a weapon";
            RefreshButton();
        }

        public void Show(WeaponDefinition weapon, int ownedCount, bool equipped)
        {
            currentWeapon = weapon;
            currentCount = Mathf.Max(0, ownedCount);
            currentEquipped = equipped;

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
                        $"{weapon.displayName}\n" +
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
        }

        private void RequestEquip()
        {
            if (currentWeapon != null && currentCount > 0 && !currentEquipped)
                EquipRequested?.Invoke(currentWeapon);
        }
    }
}
