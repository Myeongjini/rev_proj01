using System;
using UnityEngine;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Player;

namespace WizardGrower.Stages
{
    public class StageManager : MonoBehaviour
    {
        [SerializeField] private StageDefinition definition = new StageDefinition();
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private BossStageController bossStageController;
        [SerializeField] private PlayerProgression progression;

        private readonly EnemyScalingService scaling = new EnemyScalingService();
        private int currentStage = 1;
        private int killsInStage;
        private bool bossStage;

        public event Action<int, bool, int, int> StageChanged;
        public event Action<string> Feedback;

        public int CurrentStage => currentStage;
        public bool IsBossStage => bossStage;
        public int KillsInStage => killsInStage;
        public int KillsRequired => definition.killsPerStage;

        public void Initialize(EnemySpawner spawner, CurrencyWallet wallet, BossStageController bossStageController, PlayerProgression progression)
        {
            this.spawner = spawner;
            this.wallet = wallet;
            this.bossStageController = bossStageController;
            this.progression = progression;

            spawner.EnemyKilled += OnEnemyKilled;
            bossStageController.Failed += OnBossFailed;
            SpawnForCurrentStage();
        }

        private void OnEnemyKilled(EnemyBase enemy)
        {
            wallet.AddGold(enemy.RewardGold);

            if (bossStage)
            {
                bossStageController.StopTimer();
                bossStage = false;
                currentStage++;
                killsInStage = 0;
                Feedback?.Invoke("Boss Cleared!");
            }
            else
            {
                killsInStage++;
                if (killsInStage >= definition.killsPerStage)
                {
                    currentStage++;
                    killsInStage = 0;
                }
            }

            progression.RecordStage(currentStage);
            SpawnForCurrentStage();
        }

        private void OnBossFailed()
        {
            bossStage = false;
            killsInStage = 0;
            Feedback?.Invoke("Boss Failed");
            SpawnForCurrentStage();
        }

        private void SpawnForCurrentStage()
        {
            bossStage = currentStage > 0 && currentStage % definition.bossInterval == 0;
            int reward = scaling.GetReward(currentStage);

            if (bossStage)
            {
                spawner.SpawnBoss(scaling.GetBossHealth(currentStage), reward * 10);
                bossStageController.StartTimer(definition.bossTimeLimit);
            }
            else
            {
                bossStageController.StopTimer();
                spawner.SpawnNormal(scaling.GetNormalHealth(currentStage), reward);
            }

            StageChanged?.Invoke(currentStage, bossStage, killsInStage, definition.killsPerStage);
        }
    }
}
