using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.Upgrades
{
    public class UpgradeButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;

        private UpgradeSystem system;
        private UpgradeDefinition definition;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        public void Bind(UpgradeSystem system, UpgradeDefinition definition, Sprite iconSprite)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();

            this.system = system;
            this.definition = definition;
            if (icon != null)
                icon.sprite = iconSprite;
            if (button != null)
                button.onClick.AddListener(() => Buy());
            Refresh();
        }

        public void Refresh()
        {
            if (label == null || system == null || definition == null)
                return;

            label.text = $"{definition.displayName}\nLv {system.GetLevel(definition)}  {system.GetCost(definition)}G";
        }

        private void Buy()
        {
            if (system != null && system.TryPurchase(definition))
                Refresh();
        }
    }
}
