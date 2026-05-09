using UnityEngine;
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
                ctx.Wizard.Stats.RecomputeWithEquipped(ctx.WeaponInventory.Equipped != null ? ctx.WeaponInventory.Equipped.statBonuses : (WizardGrower.Weapons.WeaponStats?)null);
            }
            ctx.Wallet.SetGold(data.gold);
            ctx.Wallet.SetGems(data.gems);
            if (ctx.GachaService != null)
                ctx.GachaService.LoadState(data.summonLevel, data.summonPullsInLevel, data.pityCounter);
            if (ctx.SkillCastOrchestrator != null)
            {
                ctx.SkillCastOrchestrator.LoadOwnedSkills(data.ownedSkillIds);
                ctx.SkillCastOrchestrator.LoadEquippedSlots(data.equippedSkillSlots);
            }
            if (ctx.MissionService != null)
                ctx.MissionService.Load(data.dailyMissions, data.repeatMissions, ctx.MissionResetService != null ? ctx.MissionResetService.CurrentServerUtcMs : System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
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
            data.pityCounter = ctx.GachaService != null ? ctx.GachaService.CurrentPity : 0;
            data.summonLevel = ctx.GachaService != null ? ctx.GachaService.CurrentSummonLevel : 1;
            data.summonPullsInLevel = ctx.GachaService != null ? ctx.GachaService.SummonPullsInLevel : 0;
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
            data.currentChapter = ctx.StageManager != null ? ctx.StageManager.CurrentChapterNumber : 1;
            data.currentStage = ctx.StageManager != null ? ctx.StageManager.CurrentStageNumber : 1;
            data.stats = ctx.Wizard != null ? ctx.Wizard.Stats.CaptureSnapshot() : new PlayerStatsSnapshot();
            data.upgrades = ctx.UpgradeSystem != null ? ctx.UpgradeSystem.CaptureLevels() : new System.Collections.Generic.List<UpgradeLevelEntry>();
            if (ctx.WeaponInventory != null)
            {
                data.ownedWeapons = new System.Collections.Generic.List<WizardGrower.Weapons.OwnedWeaponEntry>(ctx.WeaponInventory.CaptureForSave());
                data.equippedWeaponId = ctx.WeaponInventory.EquippedWeaponId;
            }
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
            context.UpgradeSystem.UpgradePurchased += (_, _, _) => QueueSave();
            context.StageManager.StateChanged += (_, _, _) => QueueSave();
            if (context.WeaponInventory != null)
            {
                context.WeaponInventory.EquippedChanged += _ => QueueSave();
                context.WeaponInventory.InventoryChanged += QueueSave;
            }
            if (context.GachaService != null)
            {
                context.GachaService.PityChanged += _ => QueueSave();
                context.GachaService.StateChanged += QueueSave;
            }
            if (context.SkillCastOrchestrator != null)
                context.SkillCastOrchestrator.SlotChanged += (_, _) => QueueSave();
            if (context.MissionService != null)
                context.MissionService.StateChanged += QueueSave;
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
