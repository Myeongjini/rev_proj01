using System;
using TMPro;
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
    public struct GoldDungeonResult
    {
        public int killCount;
        public long earnedGold;
        public int difficulty;
    }

    public class GoldDungeonBootstrap : MonoBehaviour
    {
        [SerializeField] private float durationSeconds = 60f;
        [SerializeField] private int difficulty = 1;
        [SerializeField] private int goldPerKill = 10;
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

        public event Action<GoldDungeonResult> Completed;

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
                spawner.SpawnNormalGroup(50f, goldPerKill, 0f);
        }

        private void CompleteAndReturn()
        {
            if (!running)
                return;

            running = false;
            GoldDungeonResult result = new GoldDungeonResult
            {
                killCount = killCount,
                earnedGold = (long)killCount * goldPerKill,
                difficulty = Mathf.Max(1, difficulty)
            };
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
                spawner.SpawnNormalReplacement(50f, goldPerKill, 0f);
        }
    }
}
