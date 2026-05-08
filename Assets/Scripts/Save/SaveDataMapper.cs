using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Skills;
using WizardGrower.Weapons;

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
                Gems = data.gems,
                PityCounter = data.pityCounter,
                SummonLevel = Mathf.Max(1, data.summonLevel),
                SummonPullsInLevel = Mathf.Max(0, data.summonPullsInLevel),
                CurrentChapter = data.currentChapter,
                CurrentStage = data.currentStage,
                Stats = ToDocument(data.stats),
                Upgrades = ToDocument(data.upgrades),
                EquippedWeaponId = string.IsNullOrEmpty(data.equippedWeaponId) ? WeaponInventory.StarterWeaponId : data.equippedWeaponId,
                OwnedWeapons = ToDocument(data.ownedWeapons),
                OwnedWeaponIds = data.ownedWeaponIds != null ? new List<string>(data.ownedWeaponIds) : new List<string> { "wand_starter" },
                OwnedSkillIds = NormalizeOwnedSkills(data.ownedSkillIds),
                EquippedSkillSlots = NormalizeEquippedSkills(data.equippedSkillSlots)
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
                gems = doc.SaveVersion < 2 && doc.Gems <= 0 ? 300 : doc.Gems,
                pityCounter = Mathf.Max(0, doc.PityCounter),
                summonLevel = Mathf.Max(1, doc.SummonLevel),
                summonPullsInLevel = Mathf.Max(0, doc.SummonPullsInLevel),
                currentChapter = doc.CurrentChapter,
                currentStage = doc.CurrentStage,
                stats = FromDocument(doc.Stats),
                upgrades = FromDocument(doc.Upgrades),
                equippedWeaponId = string.IsNullOrEmpty(doc.EquippedWeaponId) ? WeaponInventory.StarterWeaponId : doc.EquippedWeaponId,
                ownedWeapons = FromDocument(doc.OwnedWeapons),
                ownedWeaponIds = doc.OwnedWeaponIds != null ? new List<string>(doc.OwnedWeaponIds) : new List<string> { "wand_starter" },
                ownedSkillIds = NormalizeOwnedSkills(doc.OwnedSkillIds),
                equippedSkillSlots = NormalizeEquippedSkills(doc.EquippedSkillSlots)
            };
        }

        public static List<string> NormalizeOwnedSkills(List<string> ownedSkillIds)
        {
            List<string> normalized = ownedSkillIds != null ? new List<string>(ownedSkillIds) : new List<string>();
            if (normalized.Count == 0)
                normalized.AddRange(SkillId.DefaultOwned);
            return normalized;
        }

        public static List<string> NormalizeEquippedSkills(List<string> equippedSkillSlots)
        {
            List<string> normalized = equippedSkillSlots != null ? new List<string>(equippedSkillSlots) : new List<string>();
            while (normalized.Count < SkillCastOrchestrator.SlotCount)
                normalized.Add(string.Empty);
            if (normalized.Count > SkillCastOrchestrator.SlotCount)
                normalized.RemoveRange(SkillCastOrchestrator.SlotCount, normalized.Count - SkillCastOrchestrator.SlotCount);
            if (normalized.TrueForAll(string.IsNullOrEmpty))
                normalized[0] = SkillId.Meteor;
            return normalized;
        }

        private static PlayerStatsSnapshotDoc ToDocument(PlayerStatsSnapshot snapshot)
        {
            snapshot ??= new PlayerStatsSnapshot();
            return new PlayerStatsSnapshotDoc
            {
                AttackDamage = snapshot.attackDamage,
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
                attackDamage = doc.AttackDamage > 0f ? doc.AttackDamage : (doc.AutoAttackDamage > 0f ? doc.AutoAttackDamage : 10f),
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

        private static List<OwnedWeaponEntryDoc> ToDocument(List<OwnedWeaponEntry> entries)
        {
            List<OwnedWeaponEntryDoc> docs = new List<OwnedWeaponEntryDoc>();
            if (entries == null)
                return docs;

            foreach (OwnedWeaponEntry entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.weaponId) || entry.count <= 0)
                    continue;

                docs.Add(new OwnedWeaponEntryDoc
                {
                    WeaponId = entry.weaponId,
                    Count = entry.count
                });
            }
            return docs;
        }

        private static List<OwnedWeaponEntry> FromDocument(List<OwnedWeaponEntryDoc> docs)
        {
            List<OwnedWeaponEntry> entries = new List<OwnedWeaponEntry>();
            if (docs == null)
                return entries;

            foreach (OwnedWeaponEntryDoc doc in docs)
            {
                if (doc == null || string.IsNullOrEmpty(doc.WeaponId) || doc.Count <= 0)
                    continue;

                entries.Add(new OwnedWeaponEntry(doc.WeaponId, doc.Count));
            }
            return entries;
        }
    }
}
