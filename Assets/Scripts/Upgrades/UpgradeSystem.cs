using System;
using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Economy;
using WizardGrower.Player;
using WizardGrower.Save;

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
            upgrades.Add(new UpgradeDefinition { id = "attack", displayName = "Attack", type = UpgradeType.AttackDamage, baseCost = 20, value = 5f });
            upgrades.Add(new UpgradeDefinition { id = "attack_speed", displayName = "Attack Speed", type = UpgradeType.AttackSpeed, baseCost = 40, value = 0.05f });
            upgrades.Add(new UpgradeDefinition { id = "crit_chance", displayName = "Critical Chance", type = UpgradeType.CriticalChance, baseCost = 35, value = 0.03f });
            upgrades.Add(new UpgradeDefinition { id = "crit_mult", displayName = "Critical Damage", type = UpgradeType.CriticalMultiplier, baseCost = 50, value = 0.1f });
            upgrades.Add(new UpgradeDefinition { id = "armor_pen", displayName = "Armor Pen.", type = UpgradeType.ArmorPenetration, baseCost = 45, value = 1f });
            upgrades.Add(new UpgradeDefinition { id = "max_hp", displayName = "Max Health", type = UpgradeType.MaxHealth, baseCost = 25, value = 20f });
            upgrades.Add(new UpgradeDefinition { id = "mana", displayName = "Mana", type = UpgradeType.Mana, baseCost = 25, value = 15f });
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

        public List<UpgradeLevelEntry> CaptureLevels()
        {
            List<UpgradeLevelEntry> entries = new List<UpgradeLevelEntry>();
            foreach (UpgradeDefinition definition in upgrades)
            {
                int level = GetLevel(definition);
                if (level <= 0)
                    continue;

                entries.Add(new UpgradeLevelEntry { id = definition.id, level = level });
            }
            return entries;
        }

        public void LoadLevels(List<UpgradeLevelEntry> entries)
        {
            levels.Clear();
            if (entries != null)
            {
                foreach (UpgradeLevelEntry entry in entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id))
                        continue;

                    string migratedId = MigrateUpgradeId(entry.id);
                    if (!ContainsUpgrade(migratedId))
                        continue;

                    int current = levels.TryGetValue(migratedId, out int existing) ? existing : 0;
                    levels[migratedId] = current + Mathf.Max(0, entry.level);
                }
            }

            UpgradePurchased?.Invoke(null, 0, 0);
        }

        public bool TryPurchase(UpgradeDefinition definition)
        {
            if (definition == null || wallet == null || wizard == null)
                return false;

            int cost = GetCost(definition);
            if (!wallet.TrySpendGold(cost, $"upgrade_{definition.id}"))
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
                case UpgradeType.AttackDamage:
                    wizard.Stats.AddAttackDamage(definition.value);
                    break;
                case UpgradeType.AttackSpeed:
                    wizard.Stats.AddAutoFireRate(definition.value);
                    break;
                case UpgradeType.AutoDamage:
                case UpgradeType.ManualDamage:
                    wizard.Stats.AddAttackDamage(definition.value);
                    break;
                case UpgradeType.AutoFireRate:
                case UpgradeType.ManualFireRate:
                    wizard.Stats.AddAutoFireRate(definition.value);
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
            return upgrades.Count == 7
                && upgrades[0].id == "attack"
                && upgrades[1].id == "attack_speed"
                && upgrades[2].id == "crit_chance"
                && upgrades[3].id == "crit_mult"
                && upgrades[4].id == "armor_pen"
                && upgrades[5].id == "max_hp"
                && upgrades[6].id == "mana";
        }

        private bool ContainsUpgrade(string id)
        {
            foreach (UpgradeDefinition definition in upgrades)
            {
                if (definition.id == id)
                    return true;
            }
            return false;
        }

        private static string MigrateUpgradeId(string id)
        {
            switch (id)
            {
                case "auto_dmg":
                case "manual_dmg":
                    return "attack";
                case "auto_speed":
                case "manual_speed":
                    return "attack_speed";
                default:
                    return id;
            }
        }
    }
}
