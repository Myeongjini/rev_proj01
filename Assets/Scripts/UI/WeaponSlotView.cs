using TMPro;
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

        private WeaponInventory inventory;
        private WeaponDefinition weapon;

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
                button.interactable = owned;
                button.onClick.AddListener(Equip);
            }
            if (icon != null)
                icon.sprite = weapon != null ? weapon.icon : null;
            if (frame != null)
                frame.color = weapon != null ? GetRarityColor(weapon.rarity) : Color.white;
            Refresh();
        }

        public void Refresh()
        {
            if (weapon == null)
                return;

            bool owned = inventory != null && inventory.IsOwned(weapon.weaponId);
            bool equipped = inventory != null && inventory.EquippedWeaponId == weapon.weaponId;
            if (label != null)
                label.text = owned ? weapon.displayName : weapon.displayName + "\n미보유";
            if (equippedLabel != null)
                equippedLabel.text = equipped ? "장착중" : string.Empty;
            if (button != null)
                button.interactable = owned;
        }

        private void Equip()
        {
            if (inventory != null && weapon != null)
            {
                inventory.TryEquip(weapon.weaponId);
                Refresh();
            }
        }

        private static Color GetRarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return new Color(0.30f, 0.90f, 0.78f, 1f);
                case Rarity.Rare: return new Color(0.35f, 0.58f, 1f, 1f);
                case Rarity.Epic: return new Color(0.72f, 0.35f, 1f, 1f);
                case Rarity.Legendary: return new Color(1f, 0.58f, 0.18f, 1f);
                case Rarity.Mythic: return new Color(1f, 0.22f, 0.34f, 1f);
                default: return new Color(0.80f, 0.82f, 0.86f, 1f);
            }
        }
    }
}
