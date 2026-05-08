using System.IO;
using UnityEngine;

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

            if (data.upgrades == null)
                data.upgrades = new System.Collections.Generic.List<UpgradeLevelEntry>();

            bool migratingToVersion2 = data.saveVersion < 2;
            if (migratingToVersion2)
                data.saveVersion = 2;

            if (data.ownedWeaponIds == null)
                data.ownedWeaponIds = new System.Collections.Generic.List<string>();

            if (!data.ownedWeaponIds.Contains("wand_starter"))
                data.ownedWeaponIds.Insert(0, "wand_starter");

            if (string.IsNullOrEmpty(data.equippedWeaponId) || !data.ownedWeaponIds.Contains(data.equippedWeaponId))
                data.equippedWeaponId = "wand_starter";

            if (migratingToVersion2)
            {
                data.gems = 300;
                data.pityCounter = 0;
            }

            data.gems = Mathf.Max(0, data.gems);
            data.pityCounter = Mathf.Max(0, data.pityCounter);

            return data;
        }
    }
}
