using System.Collections.Generic;
using System;
using UnityEngine;
using WizardGrower.Attendance;
using WizardGrower.Dungeons;
using WizardGrower.Missions;
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
                PlayerLevel = Mathf.Max(1, data.playerLevel),
                PlayerCurrentExp = Mathf.Max(0, data.playerCurrentExp),
                CurrentChapter = data.currentChapter,
                CurrentStage = data.currentStage,
                Stats = ToDocument(data.stats),
                Upgrades = ToDocument(data.upgrades),
                EquippedWeaponId = string.IsNullOrEmpty(data.equippedWeaponId) ? WeaponInventory.StarterWeaponId : data.equippedWeaponId,
                OwnedWeapons = ToDocument(data.ownedWeapons),
                OwnedWeaponIds = data.ownedWeaponIds != null ? new List<string>(data.ownedWeaponIds) : new List<string> { "wand_starter" },
                OwnedSkillIds = NormalizeOwnedSkills(data.ownedSkillIds),
                EquippedSkillSlots = NormalizeEquippedSkills(data.equippedSkillSlots),
                DailyMissions = ToDailyMissionDocs(data.dailyMissions),
                RepeatMissions = ToRepeatMissionDocs(data.repeatMissions),
                Attendance = ToAttendanceDoc(data.attendance),
                LastSeenAtUtcMs = data.lastSeenAtUtcMs,
                OfflineRewardPending = Math.Max(0, data.offlineRewardPending),
                OfflineRewardPendingExp = Math.Max(0, data.offlineRewardPendingExp),
                GoldDungeon = ToGoldDungeonDoc(data.goldDungeon),
                ExpDungeon = ToEXPDungeonDoc(data.expDungeon)
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
                playerLevel = Mathf.Max(1, doc.PlayerLevel),
                playerCurrentExp = Mathf.Max(0, doc.PlayerCurrentExp),
                currentChapter = doc.CurrentChapter,
                currentStage = doc.CurrentStage,
                stats = FromDocument(doc.Stats),
                upgrades = FromDocument(doc.Upgrades),
                equippedWeaponId = string.IsNullOrEmpty(doc.EquippedWeaponId) ? WeaponInventory.StarterWeaponId : doc.EquippedWeaponId,
                ownedWeapons = FromDocument(doc.OwnedWeapons),
                ownedWeaponIds = doc.OwnedWeaponIds != null ? new List<string>(doc.OwnedWeaponIds) : new List<string> { "wand_starter" },
                ownedSkillIds = NormalizeOwnedSkills(doc.OwnedSkillIds),
                equippedSkillSlots = NormalizeEquippedSkills(doc.EquippedSkillSlots),
                dailyMissions = FromDailyMissionDocs(doc.DailyMissions),
                repeatMissions = FromRepeatMissionDocs(doc.RepeatMissions),
                attendance = FromAttendanceDoc(doc.Attendance),
                lastSeenAtUtcMs = Math.Max(0, doc.LastSeenAtUtcMs),
                offlineRewardPending = Math.Max(0, doc.OfflineRewardPending),
                offlineRewardPendingExp = Math.Max(0, doc.OfflineRewardPendingExp),
                goldDungeon = FromGoldDungeonDoc(doc.GoldDungeon),
                expDungeon = FromEXPDungeonDoc(doc.ExpDungeon)
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

        private static List<DailyMissionStateDoc> ToDailyMissionDocs(List<DailyMissionState> states)
        {
            List<DailyMissionStateDoc> docs = new List<DailyMissionStateDoc>();
            if (states == null)
                return docs;

            for (int i = 0; i < states.Count; i++)
            {
                DailyMissionState state = states[i];
                if (state == null || string.IsNullOrEmpty(state.missionId))
                    continue;

                docs.Add(new DailyMissionStateDoc
                {
                    MissionId = state.missionId,
                    Progress = state.progress,
                    Claimed = state.claimed,
                    LastResetUtcMs = state.lastResetUtcMs
                });
            }

            return docs;
        }

        private static List<DailyMissionState> FromDailyMissionDocs(List<DailyMissionStateDoc> docs)
        {
            List<DailyMissionState> states = new List<DailyMissionState>();
            if (docs == null)
                return states;

            for (int i = 0; i < docs.Count; i++)
            {
                DailyMissionStateDoc doc = docs[i];
                if (doc == null || string.IsNullOrEmpty(doc.MissionId))
                    continue;

                states.Add(new DailyMissionState
                {
                    missionId = doc.MissionId,
                    progress = Mathf.Max(0, doc.Progress),
                    claimed = doc.Claimed,
                    lastResetUtcMs = doc.LastResetUtcMs
                });
            }

            return states;
        }

        private static List<RepeatMissionStateDoc> ToRepeatMissionDocs(List<RepeatMissionState> states)
        {
            List<RepeatMissionStateDoc> docs = new List<RepeatMissionStateDoc>();
            if (states == null)
                return docs;

            for (int i = 0; i < states.Count; i++)
            {
                RepeatMissionState state = states[i];
                if (state == null || string.IsNullOrEmpty(state.missionId))
                    continue;

                docs.Add(new RepeatMissionStateDoc
                {
                    MissionId = state.missionId,
                    CurrentTargetN = Mathf.Max(1, state.currentTargetN),
                    RunningCounter = Mathf.Max(0, state.runningCounter)
                });
            }

            return docs;
        }

        private static List<RepeatMissionState> FromRepeatMissionDocs(List<RepeatMissionStateDoc> docs)
        {
            List<RepeatMissionState> states = new List<RepeatMissionState>();
            if (docs == null)
                return states;

            for (int i = 0; i < docs.Count; i++)
            {
                RepeatMissionStateDoc doc = docs[i];
                if (doc == null || string.IsNullOrEmpty(doc.MissionId))
                    continue;

                states.Add(new RepeatMissionState
                {
                    missionId = doc.MissionId,
                    currentTargetN = Mathf.Max(1, doc.CurrentTargetN),
                    runningCounter = Mathf.Max(0, doc.RunningCounter)
                });
            }

            return states;
        }

        private static AttendanceStateDoc ToAttendanceDoc(AttendanceState state)
        {
            state ??= new AttendanceState();
            return new AttendanceStateDoc
            {
                CurrentDayIndex = Mathf.Clamp(state.currentDayIndex <= 0 ? 1 : state.currentDayIndex, 1, 10),
                LastClaimedUtcMs = state.lastClaimedUtcMs,
                TotalCheckIns = Mathf.Max(0, state.totalCheckIns)
            };
        }

        private static AttendanceState FromAttendanceDoc(AttendanceStateDoc doc)
        {
            if (doc == null)
                return new AttendanceState();

            return new AttendanceState
            {
                currentDayIndex = Mathf.Clamp(doc.CurrentDayIndex <= 0 ? 1 : doc.CurrentDayIndex, 1, 10),
                lastClaimedUtcMs = doc.LastClaimedUtcMs,
                totalCheckIns = Mathf.Max(0, doc.TotalCheckIns)
            };
        }

        private static GoldDungeonStateDoc ToGoldDungeonDoc(GoldDungeonState state)
        {
            state ??= new GoldDungeonState();
            return new GoldDungeonStateDoc
            {
                LastEntryDateUtcMs = Math.Max(0, state.lastEntryDateUtcMs),
                TodayEntryCount = Mathf.Max(0, state.todayEntryCount),
                BestScore = Math.Max(0, state.bestScore)
            };
        }

        private static GoldDungeonState FromGoldDungeonDoc(GoldDungeonStateDoc doc)
        {
            if (doc == null)
                return new GoldDungeonState();

            return new GoldDungeonState
            {
                lastEntryDateUtcMs = Math.Max(0, doc.LastEntryDateUtcMs),
                todayEntryCount = Mathf.Max(0, doc.TodayEntryCount),
                bestScore = Math.Max(0, doc.BestScore)
            };
        }

        private static EXPDungeonStateDoc ToEXPDungeonDoc(EXPDungeonState state)
        {
            state ??= new EXPDungeonState();
            return new EXPDungeonStateDoc
            {
                LastEntryDateUtcMs = Math.Max(0, state.lastEntryDateUtcMs),
                TodayEntryCount = Mathf.Max(0, state.todayEntryCount),
                BestScore = Math.Max(0, state.bestScore)
            };
        }

        private static EXPDungeonState FromEXPDungeonDoc(EXPDungeonStateDoc doc)
        {
            if (doc == null)
                return new EXPDungeonState();

            return new EXPDungeonState
            {
                lastEntryDateUtcMs = Math.Max(0, doc.LastEntryDateUtcMs),
                todayEntryCount = Mathf.Max(0, doc.TodayEntryCount),
                bestScore = Math.Max(0, doc.BestScore)
            };
        }
    }
}
