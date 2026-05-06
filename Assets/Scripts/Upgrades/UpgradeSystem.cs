using System;
using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Economy;
using WizardGrower.Player;

namespace WizardGrower.Upgrades
{
    public class UpgradeSystem : MonoBehaviour
    {
        [SerializeField] private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private PlayerWizard wizard;
        [SerializeField] private PlayerMana mana;

        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public event Action<UpgradeDefinition, int, int> UpgradePurchased;

        public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

        public void Initialize(CurrencyWallet wallet, PlayerWizard wizard, PlayerMana mana)
        {
            this.wallet = wallet;
            this.wizard = wizard;
            this.mana = mana;
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (upgrades.Count > 0)
                return;

            upgrades.Add(new UpgradeDefinition { id = "attack", displayName = "Attack", type = UpgradeType.Attack, baseCost = 20, value = 5f });
            upgrades.Add(new UpgradeDefinition { id = "mana", displayName = "Mana", type = UpgradeType.Mana, baseCost = 25, value = 15f });
            upgrades.Add(new UpgradeDefinition { id = "critical", displayName = "Critical", type = UpgradeType.Critical, baseCost = 35, value = 0.03f });
        }

        public int GetLevel(UpgradeDefinition definition)
        {
            return definition != null && levels.TryGetValue(definition.id, out int level) ? level : 0;
        }

        public int GetCost(UpgradeDefinition definition)
        {
            int level = GetLevel(definition);
            return Mathf.RoundToInt(definition.baseCost * Mathf.Pow(definition.costScale, level));
        }

        public bool TryPurchase(UpgradeDefinition definition)
        {
            if (definition == null || wallet == null || wizard == null)
                return false;

            int cost = GetCost(definition);
            if (!wallet.TrySpendGold(cost))
                return false;

            Apply(definition);
            int nextLevel = GetLevel(definition) + 1;
            levels[definition.id] = nextLevel;
            UpgradePurchased?.Invoke(definition, nextLevel, GetCost(definition));
            return true;
        }

        private void Apply(UpgradeDefinition definition)
        {
            switch (definition.type)
            {
                case UpgradeType.Attack:
                    wizard.Stats.AddAutoDamage(definition.value);
                    break;
                case UpgradeType.Mana:
                    mana.IncreaseMax(definition.value);
                    mana.IncreaseRegeneration(1f);
                    break;
                case UpgradeType.Critical:
                    wizard.Stats.AddCriticalChance(definition.value);
                    wizard.Stats.AddCriticalMultiplier(0.05f);
                    break;
            }
        }
    }
}
