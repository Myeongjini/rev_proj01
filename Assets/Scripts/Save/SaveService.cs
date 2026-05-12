using System.IO;
using UnityEngine;
using WizardGrower.Skills;
using WizardGrower.Weapons;

namespace WizardGrower.Save
{
    public class SaveService : MonoBehaviour
    {
        private const string FileName = "save.json";

        public SaveData CurrentData { get; private set; } = new SaveData();
        public string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public bool TryLoad()
        {
            if (!File.Exists(FilePath))
                return false;

            string json = File.ReadAllText(FilePath);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded == null)
                return false;

            CurrentData = MigrateIfNeeded(loaded);
            return true;
        }

        public void SetCurrentData(SaveData data)
        {
            CurrentData = MigrateIfNeeded(data ?? new SaveData());
        }

        public void OverwriteFromServer(SaveData remote)
        {
            CurrentData = MigrateIfNeeded(remote ?? new SaveData());
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(CurrentData, true);
            File.WriteAllText(FilePath, json);
        }

        public void Save()
        {
            CurrentData.updatedAtUnixMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(CurrentData, true);
            File.WriteAllText(FilePath, json);
        }

        public void Reset()
        {
            CurrentData = new SaveData();
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }

        private SaveData MigrateIfNeeded(SaveData data)
        {
            if (data.saveVersion < 1)
                data.saveVersion = 1;

            if (string.IsNullOrEmpty(data.userId))
                data.userId = "local";

            if (data.currentChapter <= 0)
                data.currentChapter = 1;

            if (data.currentStage <= 0)
                data.currentStage = 1;

            if (data.stats == null)
                data.stats = new PlayerStatsSnapshot();
            MigrateStatsSnapshot(data.stats);

            if (data.upgrades == null)
                data.upgrades = new System.Collections.Generic.List<UpgradeLevelEntry>();

            bool migratingToVersion2 = data.saveVersion < 2;
            if (migratingToVersion2)
                data.saveVersion = 2;

            if (data.ownedWeaponIds == null)
                data.ownedWeaponIds = new System.Collections.Generic.List<string>();
            if (data.ownedWeapons == null)
                data.ownedWeapons = new System.Collections.Generic.List<OwnedWeaponEntry>();

            if (data.saveVersion < 3)
                MigrateWeaponInventoryToV3(data);

            if (data.saveVersion < 4)
                MigrateSkillsToV4(data);

            if (data.saveVersion < 5)
                MigrateOfflineRewardToV5(data);

            if (data.saveVersion < 6)
                MigrateGoldDungeonToV6(data);

            if (data.saveVersion < 7)
                MigratePlayerExpAndEXPDungeonToV7(data);

            if (data.saveVersion < 8)
                MigrateWalletAuthorityToV8(data);

            if (data.saveVersion < 9)
                MigrateArmorAndDefenseToV9(data);

            if (data.saveVersion < 10)
                MigrateAccessoriesToV10(data);

            if (data.saveVersion < 11)
                MigrateEnhancementStoneDungeonToV11(data);

            data.saveVersion = Mathf.Max(data.saveVersion, 11);

            if (data.ownedWeapons.Count == 0)
                data.ownedWeapons.Add(new OwnedWeaponEntry(WeaponInventory.StarterWeaponId, 1));

            if (string.IsNullOrEmpty(data.equippedWeaponId))
                data.equippedWeaponId = WeaponInventory.StarterWeaponId;

            if (migratingToVersion2)
            {
                data.gems = 300;
                data.pityCounter = 0;
            }

            data.gems = Mathf.Max(0, data.gems);
            data.enhancementStone = Mathf.Max(0, data.enhancementStone);
            data.pityCounter = Mathf.Max(0, data.pityCounter);
            data.summonLevel = Mathf.Max(1, data.summonLevel);
            data.summonPullsInLevel = Mathf.Max(0, data.summonPullsInLevel);
            data.playerLevel = Mathf.Clamp(data.playerLevel <= 0 ? 1 : data.playerLevel, 1, 50);
            data.playerCurrentExp = Mathf.Max(0, data.playerCurrentExp);
            data.ownedSkillIds = SaveDataMapper.NormalizeOwnedSkills(data.ownedSkillIds);
            data.equippedSkillSlots = SaveDataMapper.NormalizeEquippedSkills(data.equippedSkillSlots);
            if (data.dailyMissions == null)
                data.dailyMissions = new System.Collections.Generic.List<WizardGrower.Missions.DailyMissionState>();
            if (data.repeatMissions == null)
                data.repeatMissions = new System.Collections.Generic.List<WizardGrower.Missions.RepeatMissionState>();
            if (data.attendance == null)
                data.attendance = new WizardGrower.Attendance.AttendanceState();
            data.lastSeenAtUtcMs = System.Math.Max(0, data.lastSeenAtUtcMs);
            data.offlineRewardPending = System.Math.Max(0, data.offlineRewardPending);
            data.offlineRewardPendingExp = System.Math.Max(0, data.offlineRewardPendingExp);
            NormalizeGoldDungeon(data);
            NormalizeEXPDungeon(data);
            NormalizeEnhancementStoneDungeon(data);
            NormalizeArmor(data);
            NormalizeAccessory(data);
            NormalizeEnhancementLevels(data);

            return data;
        }

        private static void MigrateAccessoriesToV10(SaveData data)
        {
            NormalizeAccessory(data);
            data.saveVersion = 10;
        }

        private static void MigrateEnhancementStoneDungeonToV11(SaveData data)
        {
            data.enhancementStone = Mathf.Max(0, data.enhancementStone);
            NormalizeEnhancementLevels(data);
            NormalizeEnhancementStoneDungeon(data);
            data.saveVersion = 11;
        }

        private static void MigrateArmorAndDefenseToV9(SaveData data)
        {
            NormalizeArmor(data);
            data.eliteSpawnCounter = Mathf.Max(0, data.eliteSpawnCounter);
        }

        private static void MigrateWalletAuthorityToV8(SaveData data)
        {
            data.gold = Mathf.Max(0, data.gold);
            data.gems = Mathf.Max(0, data.gems);
            data.saveVersion = 8;
        }

        private static void NormalizeArmor(SaveData data)
        {
            if (data.ownedArmors == null)
                data.ownedArmors = new System.Collections.Generic.List<WizardGrower.Armor.OwnedArmorEntry>();
            if (data.equippedArmors == null)
                data.equippedArmors = new System.Collections.Generic.List<WizardGrower.Armor.EquippedArmorEntry>();
            if (data.equippedArmorBySlot == null)
                data.equippedArmorBySlot = new System.Collections.Generic.Dictionary<string, string>();
            data.eliteSpawnCounter = Mathf.Max(0, data.eliteSpawnCounter);
            if (data.stats != null)
                data.stats.defense = Mathf.Max(0f, data.stats.defense);
        }

        private static void NormalizeAccessory(SaveData data)
        {
            if (data.ownedAccessories == null)
                data.ownedAccessories = new System.Collections.Generic.List<WizardGrower.Accessory.OwnedAccessoryEntry>();
            if (data.equippedAccessories == null)
                data.equippedAccessories = new System.Collections.Generic.List<WizardGrower.Accessory.EquippedAccessoryEntry>();
            if (data.equippedAccessoryBySlot == null)
                data.equippedAccessoryBySlot = new System.Collections.Generic.Dictionary<string, string>();
        }

        private static void NormalizeEnhancementLevels(SaveData data)
        {
            if (data.ownedWeapons != null)
                for (int i = 0; i < data.ownedWeapons.Count; i++)
                    if (data.ownedWeapons[i] != null)
                        data.ownedWeapons[i].enhancementLevel = Mathf.Clamp(data.ownedWeapons[i].enhancementLevel, 0, 10);

            if (data.ownedArmors != null)
                for (int i = 0; i < data.ownedArmors.Count; i++)
                    if (data.ownedArmors[i] != null)
                        data.ownedArmors[i].enhancementLevel = Mathf.Clamp(data.ownedArmors[i].enhancementLevel, 0, 10);

            if (data.ownedAccessories != null)
                for (int i = 0; i < data.ownedAccessories.Count; i++)
                    if (data.ownedAccessories[i] != null)
                        data.ownedAccessories[i].enhancementLevel = Mathf.Clamp(data.ownedAccessories[i].enhancementLevel, 0, 10);
        }

        private static void MigratePlayerExpAndEXPDungeonToV7(SaveData data)
        {
            if (data.playerLevel <= 0)
                data.playerLevel = 1;
            data.playerCurrentExp = System.Math.Max(0, data.playerCurrentExp);
            NormalizeEXPDungeon(data);
            data.offlineRewardPendingExp = System.Math.Max(0, data.offlineRewardPendingExp);
        }

        private static void MigrateGoldDungeonToV6(SaveData data)
        {
            NormalizeGoldDungeon(data);
        }

        private static void NormalizeGoldDungeon(SaveData data)
        {
            if (data.goldDungeon == null)
                data.goldDungeon = new WizardGrower.Dungeons.GoldDungeonState();
            data.goldDungeon.lastEntryDateUtcMs = System.Math.Max(0, data.goldDungeon.lastEntryDateUtcMs);
            data.goldDungeon.todayEntryCount = Mathf.Max(0, data.goldDungeon.todayEntryCount);
            data.goldDungeon.bestScore = System.Math.Max(0, data.goldDungeon.bestScore);
        }

        private static void NormalizeEXPDungeon(SaveData data)
        {
            if (data.expDungeon == null)
                data.expDungeon = new WizardGrower.Dungeons.EXPDungeonState();
            data.expDungeon.lastEntryDateUtcMs = System.Math.Max(0, data.expDungeon.lastEntryDateUtcMs);
            data.expDungeon.todayEntryCount = Mathf.Max(0, data.expDungeon.todayEntryCount);
            data.expDungeon.bestScore = System.Math.Max(0, data.expDungeon.bestScore);
        }

        private static void NormalizeEnhancementStoneDungeon(SaveData data)
        {
            if (data.enhancementStoneDungeon == null)
                data.enhancementStoneDungeon = new WizardGrower.Dungeons.EnhancementStoneDungeonState();
            data.enhancementStoneDungeon.lastEntryDateUtcMs = System.Math.Max(0, data.enhancementStoneDungeon.lastEntryDateUtcMs);
            data.enhancementStoneDungeon.todayEntryCount = Mathf.Max(0, data.enhancementStoneDungeon.todayEntryCount);
            data.enhancementStoneDungeon.bestScore = System.Math.Max(0, data.enhancementStoneDungeon.bestScore);
        }

        private static void MigrateOfflineRewardToV5(SaveData data)
        {
            if (data.lastSeenAtUtcMs <= 0)
                data.lastSeenAtUtcMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            data.offlineRewardPending = System.Math.Max(0, data.offlineRewardPending);
        }

        private static void MigrateSkillsToV4(SaveData data)
        {
            data.ownedSkillIds = new System.Collections.Generic.List<string>(SkillId.DefaultOwned);
            data.equippedSkillSlots = new System.Collections.Generic.List<string>
            {
                SkillId.Meteor,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };
        }

        private static void MigrateWeaponInventoryToV3(SaveData data)
        {
            data.ownedWeapons.Clear();
            if (data.ownedWeaponIds != null)
            {
                for (int i = 0; i < data.ownedWeaponIds.Count; i++)
                    AddMigratedWeapon(data.ownedWeapons, MapV6WeaponId(data.ownedWeaponIds[i]), 1);
            }

            if (data.ownedWeapons.Count == 0)
                data.ownedWeapons.Add(new OwnedWeaponEntry(WeaponInventory.StarterWeaponId, 1));

            data.equippedWeaponId = MapV6WeaponId(data.equippedWeaponId);
            if (string.IsNullOrEmpty(data.equippedWeaponId))
                data.equippedWeaponId = WeaponInventory.StarterWeaponId;
        }

        private static void AddMigratedWeapon(System.Collections.Generic.List<OwnedWeaponEntry> entries, string weaponId, int count)
        {
            if (string.IsNullOrEmpty(weaponId) || count <= 0)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].weaponId == weaponId)
                {
                    entries[i].count += count;
                    return;
                }
            }

            entries.Add(new OwnedWeaponEntry(weaponId, count));
        }

        private static string MapV6WeaponId(string weaponId)
        {
            switch (weaponId)
            {
                case "wand_starter":
                case "common_beginner_staff":
                    return WeaponInventory.StarterWeaponId;
                case "apprentice_staff":
                case "crystal_wand":
                    return "normal_beginner_staff";
                case "wizards_stave":
                case "flame_rod":
                    return "advanced_beginner_staff";
                case "arcane_scepter":
                    return "epic_beginner_staff";
                default:
                    return weaponId;
            }
        }

        private static void MigrateStatsSnapshot(PlayerStatsSnapshot stats)
        {
            if (stats == null)
                return;

            if (stats.attackDamage <= 0f)
                stats.attackDamage = stats.autoAttackDamage > 0f ? stats.autoAttackDamage : 10f;
            if (stats.autoAttackDamage <= 0f)
                stats.autoAttackDamage = stats.attackDamage;
            if (stats.manualAttackDamage <= 0f)
                stats.manualAttackDamage = stats.attackDamage * 2f;
            stats.defense = Mathf.Max(0f, stats.defense);
            stats.maxMana = Mathf.Max(0f, stats.maxMana);
        }
    }
}
