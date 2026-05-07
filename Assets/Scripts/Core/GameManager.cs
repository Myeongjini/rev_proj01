using UnityEngine;
using WizardGrower.Auth;
using WizardGrower.Combat;
using WizardGrower.Enemies;
using WizardGrower.UI;

namespace WizardGrower.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameContext context;

        private CombatCalculator calculator;

        private void Awake()
        {
            if (context == null)
                context = GetComponent<GameContext>();

            context.SaveService.TryLoad();
            calculator = new CombatCalculator(context.Wizard.Stats);

            context.Movement.Initialize(context.Wizard, context.EnemySpawner);
            context.UpgradeSystem.Initialize(context.Wallet, context.Wizard, context.Mana);
            context.AutoAttack.Initialize(context.Wizard, context.Movement, context.EnemySpawner, context.ProjectileFactory, calculator);
            context.ClickAttack.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, calculator);
            context.ActiveSkill.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, context.Mana, calculator);
            context.HUD.Initialize(context.StageManager, context.Wallet, context.Wizard, context.Mana, context.EnemySpawner, context.BossStage, context.UpgradeSystem, context.ActiveSkill, context.ClickAttack, context.Movement);
            context.StageManager.Initialize(context.ChapterDatabase, context.EnemySpawner, context.Wallet, context.BossStage, context.Progression);
            context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
            context.SaveBinder.RegisterAutoSaveTriggers(context, context.SaveService);

            context.EnemySpawner.EnemyDamaged += OnEnemyDamaged;
            context.EnemySpawner.EnemySpawned += OnEnemySpawned;
            context.Wizard.Stats.Changed += () => context.Progression.RecordCombatPower(context.Wizard.Stats.CombatPower);
            InitializeAuthenticationAsync();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && context != null)
                context.SaveBinder.SaveNow(context, context.SaveService);
        }

        private void OnApplicationQuit()
        {
            if (context != null)
                context.SaveBinder.SaveNow(context, context.SaveService);
        }

        private void OnEnemySpawned(EnemyBase enemy)
        {
            BossEnemy boss = enemy as BossEnemy;
            if (boss != null)
                boss.BossAttacked += context.Wizard.TakeBossHit;
        }

        private void OnEnemyDamaged(EnemyBase enemy, DamageInfo info)
        {
            if (context.FloatingText != null)
                context.FloatingText.Spawn(enemy.transform.position, info);
        }

        private async void InitializeAuthenticationAsync()
        {
            if (context.AuthService == null || context.UserProfileService == null)
                return;

            try
            {
                await context.AuthService.InitializeAsync(context.AuthConfig);
                string uid = await context.AuthService.SignInAnonymouslyAsync();
                context.SaveBinder.SetUserId(uid);
                await context.UserProfileService.GetOrCreateAsync(uid, context.AuthService.CurrentAccountType);
                context.AuthService.UserChanged += OnUserChanged;
                LoginPanel loginPanel = FindAnyObjectByType<LoginPanel>(FindObjectsInactive.Include);
                if (loginPanel != null)
                    loginPanel.Bind(context.AuthService, context.UserProfileService);
                Debug.Log($"Firebase anonymous UID: {uid}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Auth initialization failed: {ex.Message}");
            }
        }

        private async void OnUserChanged(string uid, AccountType type)
        {
            if (context == null || context.UserProfileService == null || string.IsNullOrEmpty(uid))
                return;

            try
            {
                context.SaveBinder.SetUserId(uid);
                await context.UserProfileService.GetOrCreateAsync(uid, type);
                await context.UserProfileService.UpdateAccountTypeAsync(uid, type);
                await context.UserProfileService.TouchLastLoginAsync(uid);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Profile update failed: {ex.Message}");
            }
        }
    }
}
