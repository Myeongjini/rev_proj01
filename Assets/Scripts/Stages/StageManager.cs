using System;
using System.Collections;
using UnityEngine;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Player;

namespace WizardGrower.Stages
{
    public class StageManager : MonoBehaviour
    {
        [SerializeField] private ChapterDatabase chapterDatabase;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private BossStageController bossStageController;
        [SerializeField] private PlayerProgression progression;

        private int currentChapter = 1;
        private int currentStageNumber = 1;
        private StageMode mode = StageMode.Field;
        private int fieldSpawnVersion;

        public event Action<ChapterDefinition, StageDefinition, StageMode> StateChanged;
        public event Action<string> Feedback;
        public event Action<bool> BossEntryAvailabilityChanged;

        public ChapterDefinition CurrentChapter { get; private set; }
        public StageDefinition CurrentStage { get; private set; }
        public StageMode Mode => mode;
        public int CurrentChapterNumber => currentChapter;
        public int CurrentStageNumber => currentStageNumber;
        public bool CanEnterBoss => mode == StageMode.Field && CurrentStage != null;

        public void Initialize(ChapterDatabase db, EnemySpawner spawner, CurrencyWallet wallet, BossStageController bossStageController, PlayerProgression progression)
        {
            chapterDatabase = db != null ? db : chapterDatabase;
            this.spawner = spawner;
            this.wallet = wallet;
            this.bossStageController = bossStageController;
            this.progression = progression;

            spawner.EnemyKilled += OnEnemyKilled;
            bossStageController.Failed += OnBossFailed;

            currentChapter = 1;
            currentStageNumber = 1;
            mode = StageMode.Field;
            ResolveCurrentStage();
            SpawnFieldEnemies();
            RaiseStateChanged();
        }

        public bool EnterBossRoom()
        {
            if (!CanEnterBoss)
                return false;

            fieldSpawnVersion++;
            StopAllCoroutines();
            mode = StageMode.BossRoom;
            SpawnBossEnemy();
            bossStageController.StartTimer(CurrentStage.bossTimeLimit);
            RaiseStateChanged();
            return true;
        }

        public void LoadProgress(int chapter, int stage)
        {
            StopAllCoroutines();
            fieldSpawnVersion++;
            mode = StageMode.Field;
            currentChapter = Mathf.Max(1, chapter);
            currentStageNumber = Mathf.Max(1, stage);
            ResolveCurrentStage();

            if (CurrentChapter == null)
            {
                currentChapter = 1;
                currentStageNumber = 1;
                ResolveCurrentStage();
            }

            if (CurrentChapter != null && CurrentChapter.stages != null && CurrentChapter.stages.Length > 0)
            {
                currentStageNumber = Mathf.Clamp(currentStageNumber, 1, CurrentChapter.stages.Length);
                ResolveCurrentStage();
            }

            SpawnFieldEnemies();
            progression.RecordStage(currentStageNumber);
            RaiseStateChanged();
        }

        private void OnEnemyKilled(EnemyBase enemy)
        {
            wallet.AddGold(enemy.RewardGold);

            if (mode == StageMode.BossRoom)
            {
                bossStageController.StopTimer();
                Feedback?.Invoke("Boss Cleared!");
                AdvanceToNextStage();
                return;
            }

            StartCoroutine(RespawnFieldEnemyAfterDelay(fieldSpawnVersion, CurrentStage));
            RaiseStateChanged();
        }

        private void OnBossFailed()
        {
            ReturnToField();
            Feedback?.Invoke("Boss Failed");
        }

        private void AdvanceToNextStage()
        {
            currentStageNumber++;
            if (CurrentChapter != null && CurrentChapter.stages != null && currentStageNumber > CurrentChapter.stages.Length)
            {
                currentChapter++;
                currentStageNumber = 1;
            }

            ChapterDefinition nextChapter = chapterDatabase != null ? chapterDatabase.GetChapter(currentChapter) : null;
            if (nextChapter == null)
            {
                currentChapter = CurrentChapter != null ? CurrentChapter.chapterNumber : 1;
                currentStageNumber = CurrentStage != null ? CurrentStage.stageNumber : 1;
                mode = StageMode.Field;
                Feedback?.Invoke("All Cleared");
                SpawnFieldEnemies();
                RaiseStateChanged();
                return;
            }

            mode = StageMode.Field;
            ResolveCurrentStage();
            progression.RecordStage(currentStageNumber);
            SpawnFieldEnemies();
            RaiseStateChanged();
        }

        private void ReturnToField()
        {
            bossStageController.StopTimer();
            mode = StageMode.Field;
            SpawnFieldEnemies();
            RaiseStateChanged();
        }

        private void SpawnFieldEnemies()
        {
            ResolveCurrentStage();
            if (CurrentStage == null)
                return;

            fieldSpawnVersion++;
            bossStageController.StopTimer();
            spawner.SpawnNormalGroup(CurrentStage.fieldMonsterHealth, CurrentStage.fieldMonsterReward, CurrentStage.fieldMonsterArmor);
        }

        private IEnumerator RespawnFieldEnemyAfterDelay(int version, StageDefinition stage)
        {
            if (stage == null)
                yield break;

            yield return new WaitForSeconds(stage.fieldRespawnDelay);

            if (mode != StageMode.Field || version != fieldSpawnVersion)
                yield break;

            spawner.SpawnNormalReplacement(stage.fieldMonsterHealth, stage.fieldMonsterReward, stage.fieldMonsterArmor);
        }

        private void SpawnBossEnemy()
        {
            ResolveCurrentStage();
            if (CurrentStage == null)
                return;

            spawner.SpawnBoss(CurrentStage.bossHealth, CurrentStage.bossReward, CurrentStage.bossArmor);
        }

        private void ResolveCurrentStage()
        {
            CurrentChapter = chapterDatabase != null ? chapterDatabase.GetChapter(currentChapter) : null;
            if (CurrentChapter == null || CurrentChapter.stages == null || CurrentChapter.stages.Length == 0)
            {
                CurrentStage = null;
                return;
            }

            int index = Mathf.Clamp(currentStageNumber - 1, 0, CurrentChapter.stages.Length - 1);
            CurrentStage = CurrentChapter.stages[index];
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(CurrentChapter, CurrentStage, mode);
            BossEntryAvailabilityChanged?.Invoke(CanEnterBoss);
        }
    }
}
