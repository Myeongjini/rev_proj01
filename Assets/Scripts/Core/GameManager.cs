using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Ads;
using WizardGrower.Attendance;
using WizardGrower.Auth;
using WizardGrower.Combat;
using WizardGrower.Dungeons;
using WizardGrower.Enemies;
using WizardGrower.Login;
using WizardGrower.Missions;
using WizardGrower.Offline;
using WizardGrower.Player;
using WizardGrower.UI;

namespace WizardGrower.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameContext context;

        private CombatCalculator calculator;
        private CombatPowerService combatPower;
        private WizardGrower.Weapons.WeaponFusionService weaponFusion;

        private void Awake()
        {
            if (context == null)
                context = GetComponent<GameContext>();

            context.SaveService.TryLoad();
            calculator = new CombatCalculator(context.Wizard.Stats);
            combatPower = new CombatPowerService();
            context.SetCombatPowerService(combatPower);
            weaponFusion = new WizardGrower.Weapons.WeaponFusionService();
            context.SetWeaponFusionService(weaponFusion);
            EnsureMissionServices();
            EnsureAttendanceServices();
            EnsureOfflineTimeTracker();
            EnsureAdSimulationService();
            EnsureGoldDungeonService();

            context.Movement.Initialize(context.Wizard, context.EnemySpawner);
            if (context.WeaponInventory != null)
                context.WeaponInventory.Initialize(context.WeaponDatabase);
            if (context.ProjectileFactory != null)
                context.ProjectileFactory.BindWeaponInventory(context.WeaponInventory);
            if (context.GachaService != null)
                context.GachaService.Initialize(context.Wallet, context.WeaponInventory, context.GachaDefinition, context.SaveService);
            context.UpgradeSystem.Initialize(context.Wallet, context.Wizard, context.Mana);
            context.AutoAttack.Initialize(context.Wizard, context.Movement, context.EnemySpawner, context.ProjectileFactory, calculator);
            context.ClickAttack.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, calculator);
            if (context.ActiveSkill != null)
                context.ActiveSkill.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, context.Mana, calculator);
            if (context.SkillCastOrchestrator != null)
                context.SkillCastOrchestrator.Initialize(context.SkillDatabase, context.Wizard, context.EnemySpawner, context.ProjectileFactory, context.Mana, calculator);
            if (context.MissionService != null)
                context.MissionService.Initialize(context.MissionDatabase, context.Wallet, context.EnemySpawner, context.StageManager, context.GachaService, weaponFusion, context.MissionResetService);
            EnsureGoldDungeonEntryPanel();
            context.HUD.Initialize(context.StageManager, context.Wallet, context.Wizard, context.Mana, context.EnemySpawner, context.BossStage, context.UpgradeSystem, context.ActiveSkill, context.ClickAttack, context.Movement, context.ChatService, context.WeaponInventory, context.WeaponDatabase, context.GachaService, context.GachaDefinition, combatPower, weaponFusion, context.SkillCastOrchestrator, context.MissionService, context.AttendanceService, context.GoldDungeonEntryPanel);
            context.StageManager.Initialize(context.ChapterDatabase, context.EnemySpawner, context.Wallet, context.BossStage, context.Progression);
            context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
            if (context.OfflineTime != null)
                context.OfflineTime.Initialize(context.SaveService, context.SaveBinder, context.MissionResetService);
            EnsureOfflineRewardService();
            EnsureStartupPopupServices();
            EnsureGoldDungeonResultModal();
            if (context.WeaponVisual != null)
                context.WeaponVisual.Bind(context.Wizard, context.WeaponInventory, context.ProjectileFactory);
            if (context.WeaponInventory != null)
                context.WeaponInventory.EquippedChanged += OnWeaponEquipped;
            combatPower.Initialize(context.Wizard.Stats, context.Mana);
            if (context.CombatPowerPopup != null)
                context.CombatPowerPopup.Bind(combatPower);
            context.HUD.BindCombatPower(combatPower);
            context.Progression.RecordCombatPower(combatPower.CurrentPower);
            context.SaveBinder.RegisterAutoSaveTriggers(context, context.SaveService);

            context.EnemySpawner.EnemyDamaged += OnEnemyDamaged;
            context.EnemySpawner.EnemySpawned += OnEnemySpawned;
            combatPower.PowerChanged += power => context.Progression.RecordCombatPower(power);
            ConsumeBootstrappedAuthentication();
        }

        private async void Start()
        {
            if (context == null || context.StartupPopupQueue == null)
                return;

            await context.StartupPopupQueue.RunAsync();
        }

        private void EnsureMissionServices()
        {
            MissionResetService resetService = context.MissionResetService != null
                ? context.MissionResetService
                : context.gameObject.AddComponent<MissionResetService>();
            MissionService missionService = context.MissionService != null
                ? context.MissionService
                : context.gameObject.AddComponent<MissionService>();
            MissionDatabase missionDatabase = context.MissionDatabase != null
                ? context.MissionDatabase
                : MissionService.CreateDefaultDatabase();

            context.SetMissionServices(missionDatabase, missionService, resetService);
        }

        private void EnsureAttendanceServices()
        {
            AttendanceConfig config = context.AttendanceConfig != null
                ? context.AttendanceConfig
                : AttendanceConfig.CreateDefault();
            AttendanceService service = context.AttendanceService ?? new AttendanceService();
            service.Initialize(config, context.Wallet, context.MissionResetService);
            context.SetAttendanceServices(config, service);
        }

        private void EnsureOfflineTimeTracker()
        {
            OfflineTimeTracker offlineTime = context.OfflineTime != null
                ? context.OfflineTime
                : context.gameObject.AddComponent<OfflineTimeTracker>();
            context.SetOfflineServices(offlineTime);
        }

        private void EnsureOfflineRewardService()
        {
            OfflineRewardService reward = context.OfflineReward != null
                ? context.OfflineReward
                : context.gameObject.AddComponent<OfflineRewardService>();
            reward.Initialize(context.OfflineTime, context.Wallet, context.StageManager, context.Wizard, context.SaveService);
            context.SetOfflineServices(context.OfflineTime, reward);
        }

        private void EnsureAdSimulationService()
        {
            AdSimulationService adSimulation = context.AdSimulation != null
                ? context.AdSimulation
                : context.GetComponent<AdSimulationService>();
            if (adSimulation == null)
                adSimulation = context.gameObject.AddComponent<AdSimulationService>();
            context.SetStartupPopupServices(context.StartupPopupQueue, context.OfflineRewardModal, adSimulation);
        }

        private void EnsureGoldDungeonService()
        {
            GoldDungeonService service = context.GoldDungeonService != null
                ? context.GoldDungeonService
                : context.GetComponent<GoldDungeonService>();
            if (service == null)
                service = context.gameObject.AddComponent<GoldDungeonService>();
            service.Initialize(context.SaveService, context.MissionResetService, context.Wallet, context.StageManager, context.AdSimulation);
            context.SetGoldDungeonService(service);
        }

        private void EnsureStartupPopupServices()
        {
            GameStartupPopupQueue popupQueue = context.StartupPopupQueue != null
                ? context.StartupPopupQueue
                : context.GetComponent<GameStartupPopupQueue>();
            if (popupQueue == null)
                popupQueue = context.gameObject.AddComponent<GameStartupPopupQueue>();
            AdSimulationService adSimulation = context.AdSimulation;
            if (adSimulation == null)
            {
                EnsureAdSimulationService();
                adSimulation = context.AdSimulation;
            }
            OfflineRewardModal modal = context.OfflineRewardModal != null
                ? context.OfflineRewardModal
                : FindOfflineRewardModalInScene();
            if (modal == null)
                modal = CreateOfflineRewardModal();

            modal.Bind(context.OfflineReward, adSimulation);
            popupQueue.Register(modal);
            context.SetStartupPopupServices(popupQueue, modal, adSimulation);
        }

        private void EnsureGoldDungeonEntryPanel()
        {
            GoldDungeonEntryPanel entryPanel = context.GoldDungeonEntryPanel != null
                ? context.GoldDungeonEntryPanel
                : FindGoldDungeonEntryPanelInScene();
            if (entryPanel == null)
                entryPanel = CreateGoldDungeonEntryPanel();

            entryPanel.Bind(context.GoldDungeonService);
            context.SetGoldDungeonEntryPanel(entryPanel);
        }

        private GoldDungeonEntryPanel FindGoldDungeonEntryPanelInScene()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                GoldDungeonEntryPanel panel = canvas.GetComponentInChildren<GoldDungeonEntryPanel>(true);
                if (panel != null)
                    return panel;
            }

            GoldDungeonEntryPanel[] panels = FindObjectsByType<GoldDungeonEntryPanel>(FindObjectsInactive.Include);
            return panels != null && panels.Length > 0 ? panels[0] : null;
        }

        private GoldDungeonEntryPanel CreateGoldDungeonEntryPanel()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            Transform parent = canvas != null ? canvas.transform : context.transform;
            GameObject panelGo = new GameObject("GoldDungeonEntryPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(GoldDungeonEntryPanel));
            panelGo.transform.SetParent(parent, false);
            RectTransform rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panelGo.transform.SetAsLastSibling();
            panelGo.SetActive(false);
            return panelGo.GetComponent<GoldDungeonEntryPanel>();
        }

        private void EnsureGoldDungeonResultModal()
        {
            GoldDungeonResultModal resultModal = context.GoldDungeonResultModal != null
                ? context.GoldDungeonResultModal
                : FindGoldDungeonResultModalInScene();
            if (resultModal == null)
                resultModal = CreateGoldDungeonResultModal();

            resultModal.Bind(context.GoldDungeonService, context.AdSimulation);
            if (context.StartupPopupQueue != null)
                context.StartupPopupQueue.Register(resultModal);
            context.SetGoldDungeonResultModal(resultModal);
        }

        private GoldDungeonResultModal FindGoldDungeonResultModalInScene()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                GoldDungeonResultModal modal = canvas.GetComponentInChildren<GoldDungeonResultModal>(true);
                if (modal != null)
                    return modal;
            }

            GoldDungeonResultModal[] modals = FindObjectsByType<GoldDungeonResultModal>(FindObjectsInactive.Include);
            return modals != null && modals.Length > 0 ? modals[0] : null;
        }

        private GoldDungeonResultModal CreateGoldDungeonResultModal()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            Transform parent = canvas != null ? canvas.transform : context.transform;
            GameObject modalGo = new GameObject("GoldDungeonResultModal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(GoldDungeonResultModal));
            modalGo.transform.SetParent(parent, false);
            RectTransform rect = modalGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            modalGo.transform.SetAsLastSibling();
            modalGo.SetActive(false);
            return modalGo.GetComponent<GoldDungeonResultModal>();
        }

        private OfflineRewardModal FindOfflineRewardModalInScene()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                OfflineRewardModal modal = canvas.GetComponentInChildren<OfflineRewardModal>(true);
                if (modal != null)
                    return modal;
            }

            OfflineRewardModal[] modals = FindObjectsByType<OfflineRewardModal>(FindObjectsInactive.Include);
            return modals != null && modals.Length > 0 ? modals[0] : null;
        }

        private OfflineRewardModal CreateOfflineRewardModal()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            Transform parent = canvas != null ? canvas.transform : context.transform;
            GameObject modalGo = new GameObject("OfflineRewardModal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(OfflineRewardModal));
            modalGo.transform.SetParent(parent, false);
            RectTransform rect = modalGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image overlay = modalGo.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.72f);
            modalGo.transform.SetAsLastSibling();
            modalGo.SetActive(false);
            return modalGo.GetComponent<OfflineRewardModal>();
        }

        private void Update()
        {
            if (context == null || context.SkillCastOrchestrator == null || context.Movement == null)
                return;

            if (context.Movement.AutoModeEnabled && !context.Movement.IsManualMoving)
                context.SkillCastOrchestrator.TickAutoCast(context.Mana);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && context != null)
            {
                if (context.OfflineTime != null)
                    context.OfflineTime.RecordLastSeenAndSave(context);
                context.SaveBinder.SaveNow(context, context.SaveService);
                if (context.SyncCoordinator != null)
                    context.SyncCoordinator.FlushNow();
            }
        }

        private void OnApplicationQuit()
        {
            if (context != null)
            {
                if (context.OfflineTime != null)
                    context.OfflineTime.RecordLastSeenAndSave(context);
                context.SaveBinder.SaveNow(context, context.SaveService);
                if (context.SyncCoordinator != null)
                    context.SyncCoordinator.FlushNow();
            }
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

        private void OnWeaponEquipped(WizardGrower.Weapons.WeaponDefinition weapon)
        {
            if (context == null || context.Wizard == null)
                return;

            context.Wizard.Stats.RecomputeWithEquipped(weapon != null ? weapon.statBonuses : (WizardGrower.Weapons.WeaponStats?)null);
            if (context.CombatPower != null)
                context.CombatPower.Recalculate(true);
        }

        private async void ConsumeBootstrappedAuthentication()
        {
            AuthBootstrapHolder holder = AuthBootstrapHolder.Instance;
            if (holder == null || holder.Auth == null || holder.Profile == null)
            {
                Debug.LogWarning("MainScene started without AuthBootstrapHolder. Start from LoginScene to enable Firebase auth.");
                return;
            }

            try
            {
                context.SetAuthenticationServices(holder.Auth, holder.Profile, holder.Config != null ? holder.Config : context.AuthConfig, holder.CloudSync);
                string uid = context.AuthService.CurrentUid;
                if (string.IsNullOrEmpty(uid))
                {
                    Debug.LogWarning("Bootstrapped auth has no UID. Cloud sync skipped.");
                    return;
                }

                context.AuthService.UserChanged += OnUserChanged;
                UserProfile profile = await context.UserProfileService.GetOrCreateAsync(uid, context.AuthService.CurrentAccountType);
                string displayName = profile != null ? profile.DisplayName : string.Empty;
                if (context.ChatService != null)
                    await context.ChatService.InitializeAsync(uid, displayName);
                if (context.PresenceCoordinator != null)
                    context.PresenceCoordinator.Begin(context, uid, displayName);
                if (context.SyncCoordinator != null)
                    await context.SyncCoordinator.StartSyncAsync(uid, context);
                else
                    context.SaveBinder.SetUserId(uid);
                Debug.Log($"MainScene consumed bootstrapped Firebase UID: {uid}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Bootstrapped auth consumption failed: {ex.Message}");
            }
        }

        private async void OnUserChanged(string uid, AccountType type)
        {
            if (context == null || context.UserProfileService == null || string.IsNullOrEmpty(uid))
                return;

            try
            {
                await context.UserProfileService.GetOrCreateAsync(uid, type);
                await context.UserProfileService.UpdateAccountTypeAsync(uid, type);
                await context.UserProfileService.TouchLastLoginAsync(uid);
                if (context.SyncCoordinator != null)
                    await context.SyncCoordinator.OnUidChanged(uid);
                else
                    context.SaveBinder.SetUserId(uid);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Profile update failed: {ex.Message}");
            }
        }
    }
}
