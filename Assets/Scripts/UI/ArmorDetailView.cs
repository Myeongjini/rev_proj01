using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Armor;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class ArmorDetailView : MonoBehaviour
    {
        [SerializeField] private TMP_Text infoLabel;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonLabel;

        private ArmorDefinition currentArmor;
        private int currentCount;
        private bool currentEquipped;

        public event Action<ArmorDefinition> EquipRequested;

        private void Awake()
        {
            if (equipButton != null)
                equipButton.onClick.AddListener(RequestEquip);
        }

        public void Clear()
        {
            currentArmor = null;
            currentCount = 0;
            currentEquipped = false;
            if (infoLabel != null)
                infoLabel.text = "방어구를 선택하세요";
            RefreshButton();
        }

        public void Show(ArmorDefinition armor, int ownedCount, bool equipped)
        {
            currentArmor = armor;
            currentCount = Mathf.Max(0, ownedCount);
            currentEquipped = equipped;

            if (infoLabel != null)
            {
                if (armor == null)
                {
                    infoLabel.text = "방어구를 선택하세요";
                }
                else
                {
                    ArmorStats s = armor.statBonuses;
                    infoLabel.text =
                        $"{armor.displayName}\n" +
                        $"{ArmorSlotTabBar.SlotKo(armor.slot)} / {WeaponGradeLabels.Display(armor.upperGrade, armor.lowerGrade)}  x{currentCount}" +
                        (currentEquipped ? "  장착중" : "") + "\n" +
                        $"방어 +{s.defense:0}  공격 +{s.attackDamageBonus:0}\n" +
                        $"치확 +{s.criticalChance:P0}  치피 +{s.criticalMultiplier:0.##}\n" +
                        $"HP +{s.maxHealth:0}  Mana +{s.maxMana:0}\n" +
                        armor.flavorText;
                }
            }

            RefreshButton();
        }

        private void RefreshButton()
        {
            bool canEquip = currentArmor != null && currentCount > 0 && !currentEquipped;
            if (equipButton != null)
                equipButton.interactable = canEquip;
            if (equipButtonLabel != null)
                equipButtonLabel.text = currentEquipped ? "장착중" : "장착";
        }

        private void RequestEquip()
        {
            if (currentArmor != null && currentCount > 0 && !currentEquipped)
                EquipRequested?.Invoke(currentArmor);
        }
    }
}
