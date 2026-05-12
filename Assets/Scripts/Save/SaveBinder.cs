using UnityEngine;
using WizardGrower.Accessory;
using WizardGrower.Core;

namespace WizardGrower.Save
{
    public class SaveBinder : MonoBehaviour
    {
        [SerializeField] private float saveDebounceSeconds = 1f;

        private GameContext context;
        private SaveService service;
        private string userId = "local";
        private bool saveQueued;
        private float nextSaveTime;

        public void ApplyToGame(SaveData data, GameContext ctx)
        {
            if (data == null || ctx == null)
                return;

            ctx.Wizard.Stats.ApplySnapshot(data.stats);
            if (ctx.WeaponInventory != null)
            {
                ctx.WeaponInventory.LoadFromSave(data.ownedWeapons, data.equippedWeaponId);
            }
            if (ctx.ArmorInventory != null)
            {
                ctx.ArmorInventory.LoadFromSave(data.ownedArmors, data.equippedArmors);
                if (ctx.EliteSpawnTracker != null)
                    ctx.EliteSpawnTracker.LoadCounter(data.eliteSpawnCounter);
            }
            if (ctx.AccessoryInventory != null)
                ctx.AccessoryInventory.LoadFromSave(data.ownedAccessories, data.equippedAccessories);
            ctx.Wizard.Stats.RecomputeWithEquipment(
                ctx.WeaponInventory != null && ctx.WeaponInventory.Equipped != null ? ctx.WeaponInventory.Equipped.statBonuses : (WizardGrower.Weapons.WeaponStats?)null,
                ctx.WeaponInventory != null ? ctx.WeaponInventory.GetEnhancementLevel(ctx.WeaponInventory.EquippedWeaponId) : 0,
                ctx.ArmorInventory != null ? ctx.ArmorInventory.CaptureEquippedStats() : default,
                ctx.AccessoryInventory != null ? ctx.AccessoryInventory.CaptureEquippedStats() : default);
            ctx.Wallet.SetGold(data.gold);
            ctx.Wallet.SetGems(data.gems);
            ctx.Wallet.SetEnhancementStone(data.enhancementStone);
            if (ctx.GachaService != null)
                ctx.GachaService.LoadState(data.summonLevel, data.summonPullsInLevel, data.pityCounter);
            if (ctx.PlayerLevelService != null)
                ctx.PlayerLevelService.LoadState(data.playerLevel, data.playerCurrentExp);
            if (ctx.SkillCastOrchestrator != null)
            {
                ctx.SkillCastOrchestrator.LoadOwnedSkills(data.ownedSkillIds);
                ctx.SkillCastOrchestrator.LoadEquippedSlots(data.equippedSkillSlots);
            }
            if (ctx.MissionService != null)
                ctx.MissionService.Load(data.dailyMissions, data.repeatMissions, ctx.MissionResetService != null ? ctx.MissionResetService.CurrentServerUtcMs : System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (ctx.AttendanceService != null)
                ctx.AttendanceService.Load(data.attendance);
            ctx.UpgradeSystem.LoadLevels(data.upgrades);
            ctx.StageManager.LoadProgress(data.currentChapter, data.currentStage);
            ctx.Progression.RecordCombatPower(ctx.Wizard.Stats.CombatPower);
            ctx.Progression.RecordStage(ctx.StageManager.CurrentStageNumber);
        }

        public SaveData CaptureFromGame(GameContext ctx)
        {
            SaveData data = new SaveData();
            if (ctx == null)
                return data;

            data.userId = string.IsNullOrEmpty(userId) ? "local" : userId;
            data.gold = ctx.Wallet != null ? ctx.Wallet.Gold : 0;
            data.gems = ctx.Wallet != null ? ctx.Wallet.Gems : 300;
            data.enhancementStone = ctx.Wallet != null ? ctx.Wallet.EnhancementStone : 0;
            data.pityCounter = ctx.GachaService != null ? ctx.GachaService.CurrentPity : 0;
            data.summonLevel = ctx.GachaService != null ? ctx.GachaService.CurrentSummonLevel : 1;
            data.summonPullsInLevel = ctx.GachaService != null ? ctx.GachaService.SummonPullsInLevel : 0;
            data.playerLevel = ctx.PlayerLevelService != null ? ctx.PlayerLevelService.CurrentLevel : 1;
            data.playerCurrentExp = ctx.PlayerLevelService != null ? ctx.PlayerLevelService.CurrentExp : 0;
            if (ctx.SkillCastOrchestrator != null)
            {
                data.ownedSkillIds = ctx.SkillCastOrchestrator.CaptureOwnedSkillIds();
                data.equippedSkillSlots = ctx.SkillCastOrchestrator.CaptureEquippedSkillSlots();
            }
            if (ctx.MissionService != null)
            {
                data.dailyMissions = ctx.MissionService.CaptureDaily();
                data.repeatMissions = ctx.MissionService.CaptureRepeat();
            }
            if (ctx.AttendanceService != null)
                data.attendance = ctx.AttendanceService.Capture();
            data.currentChapter = ctx.StageManager != null ? ctx.StageManager.CurrentChapterNumber : 1;
            data.currentStage = ctx.StageManager != null ? ctx.StageManager.CurrentStageNumber : 1;
            data.stats = ctx.Wizard != null ? ctx.Wizard.Stats.CaptureSnapshot() : new PlayerStatsSnapshot();
            data.upgrades = ctx.UpgradeSystem != null ? ctx.UpgradeSystem.CaptureLevels() : new System.Collections.Generic.List<UpgradeLevelEntry>();
            if (ctx.WeaponInventory != null)
            {
                data.ownedWeapons = new System.Collections.Generic.List<WizardGrower.Weapons.OwnedWeaponEntry>(ctx.WeaponInventory.CaptureForSave());
                data.equippedWeaponId = ctx.WeaponInventory.EquippedWeaponId;
            }
            if (ctx.ArmorInventory != null)
            {
                data.ownedArmors = new System.Collections.Generic.List<WizardGrower.Armor.OwnedArmorEntry>(ctx.ArmorInventory.CaptureForSave());
                data.equippedArmors = new System.Collections.Generic.List<WizardGrower.Armor.EquippedArmorEntry>(ctx.ArmorInventory.CaptureEquippedForSave());
                data.equippedArmorBySlot = new System.Collections.Generic.Dictionary<string, string>();
                for (int i = 0; i < data.equippedArmors.Count; i++)
                    data.equippedArmorBySlot[data.equippedArmors[i].slot.ToString()] = data.equippedArmors[i].armorId;
            }
            if (ctx.AccessoryInventory != null)
            {
                data.ownedAccessories = new System.Collections.Generic.List<OwnedAccessoryEntry>(ctx.AccessoryInventory.CaptureForSave());
                data.equippedAccessories = new System.Collections.Generic.List<EquippedAccessoryEntry>(ctx.AccessoryInventory.CaptureEquippedForSave());
                data.equippedAccessoryBySlot = new System.Collections.Generic.Dictionary<string, string>();
                for (int i = 0; i < data.equippedAccessories.Count; i++)
                    data.equippedAccessoryBySlot[data.equippedAccessories[i].slot.ToString()] = data.equippedAccessories[i].accessoryId;
            }
            data.eliteSpawnCounter = ctx.EliteSpawnTracker != null ? ctx.EliteSpawnTracker.Counter : 0;
            data.goldDungeon = ctx.GoldDungeonService != null && ctx.SaveService != null && ctx.SaveService.CurrentData.goldDungeon != null
                ? ctx.SaveService.CurrentData.goldDungeon
                : new WizardGrower.Dungeons.GoldDungeonState();
            data.expDungeon = ctx.EXPDungeonService != null && ctx.SaveService != null && ctx.SaveService.CurrentData.expDungeon != null
                ? ctx.SaveService.CurrentData.expDungeon
                : new WizardGrower.Dungeons.EXPDungeonState();
            data.enhancementStoneDungeon = ctx.EnhancementStoneDungeonService != null && ctx.SaveService != null && ctx.SaveService.CurrentData.enhancementStoneDungeon != null
                ? ctx.SaveService.CurrentData.enhancementStoneDungeon
                : new WizardGrower.Dungeons.EnhancementStoneDungeonState();
            return data;
        }

        public void RegisterAutoSaveTriggers(GameContext ctx, SaveService service)
        {
            context = ctx;
            this.service = service;
            if (context == null || this.service == null)
                return;

            context.Wallet.GoldChanged += _ => QueueSave();
            context.Wallet.GemsChanged += _ => QueueSave();
            context.Wallet.EnhancementStoneChanged += _ => QueueSave();
            context.UpgradeSystem.UpgradePurchased += (_, _, _) => QueueSave();
            context.StageManager.StateChanged += (_, _, _) => QueueSave();
            if (context.WeaponInventory != null)
            {
                context.WeaponInventory.EquippedChanged += _ => QueueSave();
                context.WeaponInventory.InventoryChanged += QueueSave;
            }
            if (context.ArmorInventory != null)
            {
                context.ArmorInventory.EquippedChanged += _ => QueueSave();
                context.ArmorInventory.InventoryChanged += QueueSave;
            }
            if (context.AccessoryInventory != null)
            {
                context.AccessoryInventory.EquippedChanged += _ => QueueSave();
                context.AccessoryInventory.InventoryChanged += QueueSave;
            }
            if (context.EliteSpawnTracker != null)
                context.EliteSpawnTracker.CounterChanged += _ => QueueSave();
            if (context.GachaService != null)
            {
                context.GachaService.PityChanged += _ => QueueSave();
                context.GachaService.StateChanged += QueueSave;
            }
            if (context.SkillCastOrchestrator != null)
                context.SkillCastOrchestrator.SlotChanged += (_, _) => QueueSave();
            if (context.MissionService != null)
                context.MissionService.StateChanged += QueueSave;
            if (context.AttendanceService != null)
                context.AttendanceService.StateChanged += QueueSave;
            if (context.GoldDungeonService != null)
                context.GoldDungeonService.StateChanged += QueueSave;
            if (context.EXPDungeonService != null)
                context.EXPDungeonService.StateChanged += QueueSave;
            if (context.EnhancementStoneDungeonService != null)
                context.EnhancementStoneDungeonService.StateChanged += QueueSave;
            if (context.PlayerLevelService != null)
                context.PlayerLevelService.StateChanged += QueueSave;
        }

        public void SetUserId(string uid)
        {
            userId = string.IsNullOrEmpty(uid) ? "local" : uid;
            QueueSave();
        }

        public void SaveNow(GameContext ctx, SaveService service)
        {
            if (ctx == null || service == null)
                return;

            service.SetCurrentData(CaptureFromGame(ctx));
            service.Save();
            saveQueued = false;
        }

        private void Update()
        {
            if (!saveQueued || Time.unscaledTime < nextSaveTime)
                return;

            SaveNow(context, service);
        }

        private void QueueSave()
        {
            saveQueued = true;
            nextSaveTime = Time.unscaledTime + saveDebounceSeconds;
        }
    }
}
