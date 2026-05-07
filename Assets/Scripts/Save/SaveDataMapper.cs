using System.Collections.Generic;

namespace WizardGrower.Save
{
    public static class SaveDataMapper
    {
        public static SaveDataDocument ToDocument(SaveData data)
        {
            data ??= new SaveData();

            return new SaveDataDocument
            {
                SaveVersion = data.saveVersion,
                UserId = string.IsNullOrEmpty(data.userId) ? "local" : data.userId,
                UpdatedAtUnixMs = data.updatedAtUnixMs,
                Gold = data.gold,
                CurrentChapter = data.currentChapter,
                CurrentStage = data.currentStage,
                Stats = ToDocument(data.stats),
                Upgrades = ToDocument(data.upgrades)
            };
        }

        public static SaveData FromDocument(SaveDataDocument doc)
        {
            if (doc == null)
                return null;

            return new SaveData
            {
                saveVersion = doc.SaveVersion,
                userId = string.IsNullOrEmpty(doc.UserId) ? "local" : doc.UserId,
                updatedAtUnixMs = doc.UpdatedAtUnixMs,
                gold = doc.Gold,
                currentChapter = doc.CurrentChapter,
                currentStage = doc.CurrentStage,
                stats = FromDocument(doc.Stats),
                upgrades = FromDocument(doc.Upgrades)
            };
        }

        private static PlayerStatsSnapshotDoc ToDocument(PlayerStatsSnapshot snapshot)
        {
            snapshot ??= new PlayerStatsSnapshot();
            return new PlayerStatsSnapshotDoc
            {
                AutoAttackDamage = snapshot.autoAttackDamage,
                ManualAttackDamage = snapshot.manualAttackDamage,
                AutoAttackInterval = snapshot.autoAttackInterval,
                ManualAttackInterval = snapshot.manualAttackInterval,
                CriticalChance = snapshot.criticalChance,
                CriticalMultiplier = snapshot.criticalMultiplier,
                ArmorPenetration = snapshot.armorPenetration,
                MaxHealth = snapshot.maxHealth,
                CurrentHealth = snapshot.currentHealth
            };
        }

        private static PlayerStatsSnapshot FromDocument(PlayerStatsSnapshotDoc doc)
        {
            if (doc == null)
                return new PlayerStatsSnapshot();

            return new PlayerStatsSnapshot
            {
                autoAttackDamage = doc.AutoAttackDamage,
                manualAttackDamage = doc.ManualAttackDamage,
                autoAttackInterval = doc.AutoAttackInterval,
                manualAttackInterval = doc.ManualAttackInterval,
                criticalChance = doc.CriticalChance,
                criticalMultiplier = doc.CriticalMultiplier,
                armorPenetration = doc.ArmorPenetration,
                maxHealth = doc.MaxHealth,
                currentHealth = doc.CurrentHealth
            };
        }

        private static List<UpgradeLevelEntryDoc> ToDocument(List<UpgradeLevelEntry> entries)
        {
            List<UpgradeLevelEntryDoc> docs = new List<UpgradeLevelEntryDoc>();
            if (entries == null)
                return docs;

            foreach (UpgradeLevelEntry entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                docs.Add(new UpgradeLevelEntryDoc
                {
                    Id = entry.id,
                    Level = entry.level
                });
            }
            return docs;
        }

        private static List<UpgradeLevelEntry> FromDocument(List<UpgradeLevelEntryDoc> docs)
        {
            List<UpgradeLevelEntry> entries = new List<UpgradeLevelEntry>();
            if (docs == null)
                return entries;

            foreach (UpgradeLevelEntryDoc doc in docs)
            {
                if (doc == null || string.IsNullOrEmpty(doc.Id))
                    continue;

                entries.Add(new UpgradeLevelEntry
                {
                    id = doc.Id,
                    level = doc.Level
                });
            }
            return entries;
        }
    }
}
