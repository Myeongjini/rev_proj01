using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
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

        private WeaponInventory inventory;
        private WeaponDefinition weapon;

        public event Action<WeaponDefinition> Selected;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        public void Bind(WeaponInventory inventory, WeaponDefinition weapon, bool owned)
        {
            this.inventory = inventory;
            this.weapon = weapon;
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

        public void Refresh()
        {
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
            if (icon != null)
                icon.color = owned ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.75f);
            if (button != null)
                button.interactable = true;
        }

        private void Select()
        {
            if (weapon != null)
                Selected?.Invoke(weapon);
        }

    }
}
