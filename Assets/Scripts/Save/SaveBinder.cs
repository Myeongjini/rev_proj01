using UnityEngine;
using WizardGrower.Core;

namespace WizardGrower.Save
{
    public class SaveBinder : MonoBehaviour
    {
        [SerializeField] private float saveDebounceSeconds = 1f;

        private GameContext context;
        private SaveService service;
        private bool saveQueued;
        private float nextSaveTime;

        public void ApplyToGame(SaveData data, GameContext ctx)
        {
            if (data == null || ctx == null)
                return;

            ctx.Wizard.Stats.ApplySnapshot(data.stats);
            ctx.Wallet.SetGold(data.gold);
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

            data.gold = ctx.Wallet != null ? ctx.Wallet.Gold : 0;
            data.currentChapter = ctx.StageManager != null ? ctx.StageManager.CurrentChapterNumber : 1;
            data.currentStage = ctx.StageManager != null ? ctx.StageManager.CurrentStageNumber : 1;
            data.stats = ctx.Wizard != null ? ctx.Wizard.Stats.CaptureSnapshot() : new PlayerStatsSnapshot();
            data.upgrades = ctx.UpgradeSystem != null ? ctx.UpgradeSystem.CaptureLevels() : new System.Collections.Generic.List<UpgradeLevelEntry>();
            return data;
        }

        public void RegisterAutoSaveTriggers(GameContext ctx, SaveService service)
        {
            context = ctx;
            this.service = service;
            if (context == null || this.service == null)
                return;

            context.Wallet.GoldChanged += _ => QueueSave();
            context.UpgradeSystem.UpgradePurchased += (_, _, _) => QueueSave();
            context.StageManager.StateChanged += (_, _, _) => QueueSave();
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
