using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WizardGrower.Combat;
using WizardGrower.Enemies;
using WizardGrower.Player;
using WizardGrower.UI;

namespace WizardGrower.Dungeons
{
    [Serializable]
    public struct EnhancementStoneDungeonResult
    {
        public int killCount;
        public long earnedStone;
        public int difficulty;
    }

    public static class EnhancementStoneDungeonSceneTransfer
    {
        public static event Action PendingResultChanged;

        public static EnhancementStoneDungeonResult? PendingResult { get; private set; }

        public static void SetPending(EnhancementStoneDungeonResult result)
        {
            PendingResult = result;
            PendingResultChanged?.Invoke();
        }

        public static void Clear()
        {
            PendingResult = null;
        }
    }

    public class EnhancementStoneDungeonBootstrap : MonoBehaviour
    {
        [SerializeField] private float durationSeconds = 60f;
        [SerializeField] private int difficulty = 1;
        [SerializeField] private int stonePerKill = 1;
        [SerializeField] private PlayerWizard wizard;
        [SerializeField] private PlayerMovementController movement;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private ProjectileFactory projectileFactory;
        [SerializeField] private AutoAttackController autoAttack;
        [SerializeField] private CountdownTimerView timer;
        [SerializeField] private Button abandonButton;
        [SerializeField] private string mainSceneName = "MainScene";

        private float remaining;
        private bool running;
        private int killCount;

        public event Action<EnhancementStoneDungeonResult> Completed;

        private void Start()
        {
            EnsureRuntimeBindings();
            Begin();
        }

        private void Update()
        {
            if (!running)
                return;

            remaining -= Time.deltaTime;
            if (timer != null)
                timer.Refresh(Mathf.Max(0f, remaining), durationSeconds);
            if (remaining <= 0f)
                CompleteAndReturn();
        }

        public void Abandon()
        {
            CompleteAndReturn();
        }

        private void Begin()
        {
            remaining = durationSeconds;
            running = true;
            killCount = 0;
            if (timer != null)
                timer.Refresh(remaining, durationSeconds);
            if (movement != null)
                movement.SetAutoMode(true);
            if (spawner != null)
                spawner.SpawnNormalGroup(50f, 0, 0f);
        }

        private void CompleteAndReturn()
        {
            if (!running)
                return;

            running = false;
            EnhancementStoneDungeonResult result = new EnhancementStoneDungeonResult
            {
                killCount = killCount,
                earnedStone = (long)killCount * Mathf.Max(1, stonePerKill),
                difficulty = Mathf.Max(1, difficulty)
            };
            EnhancementStoneDungeonSceneTransfer.SetPending(result);
            Completed?.Invoke(result);
            SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Single);
        }

        private void EnsureRuntimeBindings()
        {
            if (wizard == null)
                wizard = FindAnyObjectByType<PlayerWizard>();
            if (movement == null && wizard != null)
                movement = wizard.GetComponent<PlayerMovementController>();
            if (spawner == null)
                spawner = FindAnyObjectByType<EnemySpawner>();
            if (projectileFactory == null)
                projectileFactory = FindAnyObjectByType<ProjectileFactory>();
            if (autoAttack == null)
                autoAttack = FindAnyObjectByType<AutoAttackController>();
            if (timer == null)
                timer = FindAnyObjectByType<CountdownTimerView>();

            if (movement != null)
                movement.Initialize(wizard, spawner);
            if (autoAttack != null)
                autoAttack.Initialize(wizard, movement, spawner, projectileFactory, new CombatCalculator(wizard != null ? wizard.Stats : null));
            if (spawner != null)
                spawner.EnemyKilled += OnEnemyKilled;
            if (abandonButton != null)
                abandonButton.onClick.AddListener(Abandon);
        }

        private void OnEnemyKilled(EnemyBase enemy)
        {
            killCount++;
            if (spawner != null)
                spawner.SpawnNormalReplacement(50f, 0, 0f);
        }
    }
}
