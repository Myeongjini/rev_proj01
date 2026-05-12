using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Accessory;
using WizardGrower.Armor;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class WeaponSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text equippedLabel;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private TMP_Text enhancementLabel;

        private WeaponInventory inventory;
        private WeaponDefinition weapon;
        private ArmorInventory armorInventory;
        private ArmorDefinition armor;
        private AccessoryInventory accessoryInventory;
        private AccessoryDefinition accessory;

        public event Action<WeaponDefinition> Selected;
        public event Action<ArmorDefinition> SelectedArmor;
        public event Action<AccessoryDefinition> SelectedAccessory;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (enhancementLabel == null)
                enhancementLabel = FindText("EnhancementLabel");
        }

        public void Bind(WeaponInventory inventory, WeaponDefinition weapon, bool owned)
        {
            this.inventory = inventory;
            this.weapon = weapon;
            armorInventory = null;
            armor = null;
            accessoryInventory = null;
            accessory = null;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = true;
                button.onClick.AddListener(Select);
            }
            if (icon != null)
                icon.sprite = weapon != null ? weapon.icon : null;
            if (frame != null)
                frame.color = weapon != null ? RarityVisuals.ColorFor(weapon.upperGrade) : Color.white;
            Refresh();
        }

        public void BindArmor(ArmorInventory inventory, ArmorDefinition armor)
        {
            armorInventory = inventory;
            this.armor = armor;
            this.inventory = null;
            weapon = null;
            accessoryInventory = null;
            accessory = null;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = true;
                button.onClick.AddListener(Select);
            }
            if (icon != null)
                icon.sprite = armor != null ? armor.icon : null;
            if (frame != null)
                frame.color = armor != null ? RarityVisuals.ColorFor(armor.upperGrade) : Color.white;
            Refresh();
        }

        public void BindAccessory(AccessoryInventory inventory, AccessoryDefinition accessory)
        {
            accessoryInventory = inventory;
            this.accessory = accessory;
            this.inventory = null;
            weapon = null;
            armorInventory = null;
            armor = null;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = true;
                button.onClick.AddListener(Select);
            }
            if (icon != null)
                icon.sprite = accessory != null ? accessory.icon : null;
            if (frame != null)
                frame.color = accessory != null ? RarityVisuals.ColorFor(accessory.upperGrade) : Color.white;
            Refresh();
        }

        public void Refresh()
        {
            if (accessory != null)
            {
                int accessoryCount = accessoryInventory != null ? accessoryInventory.GetCount(accessory.accessoryId) : 0;
                bool accessoryOwned = accessoryCount > 0;
                bool accessoryEquipped = accessoryInventory != null && accessoryInventory.GetEquippedId(accessory.slot) == accessory.accessoryId;
                if (label != null)
                    label.text = $"{WeaponGradeLabels.LowerKo(accessory.lowerGrade)}\n{accessory.displayName}";
                if (equippedLabel != null)
                    equippedLabel.text = accessoryEquipped ? "장착중" : string.Empty;
                if (countLabel != null)
                    countLabel.text = accessoryOwned ? $"x{accessoryCount}" : string.Empty;
                if (enhancementLabel != null)
                {
                    int level = accessoryInventory != null ? accessoryInventory.GetEnhancementLevel(accessory.accessoryId) : 0;
                    enhancementLabel.text = accessoryOwned && level > 0 ? $"+{level}" : string.Empty;
                }
                if (icon != null)
                    icon.color = accessoryOwned ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.75f);
                if (button != null)
                    button.interactable = true;
                return;
            }

            if (armor != null)
            {
                int armorCount = armorInventory != null ? armorInventory.GetCount(armor.armorId) : 0;
                bool armorOwned = armorCount > 0;
                bool armorEquipped = armorInventory != null && armorInventory.GetEquippedId(armor.slot) == armor.armorId;
                if (label != null)
                    label.text = $"{WeaponGradeLabels.LowerKo(armor.lowerGrade)}\n{armor.displayName}";
                if (equippedLabel != null)
                    equippedLabel.text = armorEquipped ? "장착중" : string.Empty;
                if (countLabel != null)
                    countLabel.text = armorOwned ? $"x{armorCount}" : string.Empty;
                if (enhancementLabel != null)
                {
                    int level = armorInventory != null ? armorInventory.GetEnhancementLevel(armor.armorId) : 0;
                    enhancementLabel.text = armorOwned && level > 0 ? $"+{level}" : string.Empty;
                }
                if (icon != null)
                    icon.color = armorOwned ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.75f);
                if (button != null)
                    button.interactable = true;
                return;
            }

            if (weapon == null)
                return;

            int count = inventory != null ? inventory.GetCount(weapon.weaponId) : 0;
            bool owned = count > 0;
            bool equipped = inventory != null && inventory.EquippedWeaponId == weapon.weaponId;
            if (label != null)
                label.text = $"{WeaponGradeLabels.LowerKo(weapon.lowerGrade)}\n{weapon.displayName}";
            if (equippedLabel != null)
                equippedLabel.text = equipped ? "장착중" : string.Empty;
            if (countLabel != null)
                countLabel.text = $"x{count}";
            if (enhancementLabel != null)
            {
                int level = inventory != null ? inventory.GetEnhancementLevel(weapon.weaponId) : 0;
                enhancementLabel.text = owned && level > 0 ? $"+{level}" : string.Empty;
            }
            if (icon != null)
                icon.color = owned ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.75f);
            if (button != null)
                button.interactable = true;
        }

        private void Select()
        {
            if (accessory != null)
                SelectedAccessory?.Invoke(accessory);
            else if (armor != null)
                SelectedArmor?.Invoke(armor);
            else if (weapon != null)
                Selected?.Invoke(weapon);
        }

        private TMP_Text FindText(string objectName)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
                if (texts[i] != null && texts[i].name == objectName)
                    return texts[i];
            return null;
        }

    }
}
