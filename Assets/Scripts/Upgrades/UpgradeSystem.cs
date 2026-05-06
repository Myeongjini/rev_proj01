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
            if (HasCurrentDefaultSet())
                return;

            upgrades.Clear();
            upgrades.Add(new UpgradeDefinition { id = "auto_dmg", displayName = "자동공격력", type = UpgradeType.AutoDamage, baseCost = 20, value = 5f });
            upgrades.Add(new UpgradeDefinition { id = "manual_dmg", displayName = "수동공격력", type = UpgradeType.ManualDamage, baseCost = 30, value = 8f });
            upgrades.Add(new UpgradeDefinition { id = "auto_speed", displayName = "자동발사속도", type = UpgradeType.AutoFireRate, baseCost = 40, value = 0.05f });
            upgrades.Add(new UpgradeDefinition { id = "crit_chance", displayName = "크리확률", type = UpgradeType.CriticalChance, baseCost = 35, value = 0.03f });
            upgrades.Add(new UpgradeDefinition { id = "crit_mult", displayName = "크리데미지", type = UpgradeType.CriticalMultiplier, baseCost = 50, value = 0.1f });
            upgrades.Add(new UpgradeDefinition { id = "armor_pen", displayName = "방어관통", type = UpgradeType.ArmorPenetration, baseCost = 45, value = 1f });
            upgrades.Add(new UpgradeDefinition { id = "max_hp", displayName = "최대체력", type = UpgradeType.MaxHealth, baseCost = 25, value = 20f });
            upgrades.Add(new UpgradeDefinition { id = "mana", displayName = "마나", type = UpgradeType.Mana, baseCost = 25, value = 15f });
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
                case UpgradeType.AutoDamage:
                    wizard.Stats.AddAutoDamage(definition.value);
                    break;
                case UpgradeType.ManualDamage:
                    wizard.Stats.AddManualDamage(definition.value);
                    break;
                case UpgradeType.AutoFireRate:
                    wizard.Stats.AddAutoFireRate(definition.value);
                    break;
                case UpgradeType.ManualFireRate:
                    wizard.Stats.AddManualFireRate(definition.value);
                    break;
                case UpgradeType.CriticalChance:
                    wizard.Stats.AddCriticalChance(definition.value);
                    break;
                case UpgradeType.CriticalMultiplier:
                    wizard.Stats.AddCriticalMultiplier(definition.value);
                    break;
                case UpgradeType.ArmorPenetration:
                    wizard.Stats.AddArmorPenetration(definition.value);
                    break;
                case UpgradeType.MaxHealth:
                    wizard.Stats.AddMaxHealth(definition.value);
                    break;
                case UpgradeType.Mana:
                    if (mana != null)
                    {
                        mana.IncreaseMax(definition.value);
                        mana.IncreaseRegeneration(1f);
                    }
                    break;
            }
        }

        private bool HasCurrentDefaultSet()
        {
            return upgrades.Count == 8
                && upgrades[0].id == "auto_dmg"
                && upgrades[1].id == "manual_dmg"
                && upgrades[2].id == "auto_speed"
                && upgrades[3].id == "crit_chance"
                && upgrades[4].id == "crit_mult"
                && upgrades[5].id == "armor_pen"
                && upgrades[6].id == "max_hp"
                && upgrades[7].id == "mana";
        }
    }
}
