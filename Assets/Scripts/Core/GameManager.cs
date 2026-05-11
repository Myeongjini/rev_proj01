using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WizardGrower.Ads;
using WizardGrower.Armor;
using WizardGrower.Attendance;
using WizardGrower.Auth;
using WizardGrower.Cloud;
using WizardGrower.Combat;
using WizardGrower.Dungeons;
using WizardGrower.Drops;
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
        private ArmorFusionService armorFusion;

        private void Awake()
        {
            if (context == null)
                context = GetComponent<GameContext>();

            context.SaveService.TryLoad();
            calculator = new CombatCalculator(context.Wizard.Stats);
            combatPower = new CombatPowerService();
            context.SetCombatPowerService(combatPower);
            EnsureCloudFunctionsClient();
            if (context.Wallet != null)
                context.Wallet.InitializeAuthority(context.CloudFunctionsClient);
            weaponFusion = new WizardGrower.Weapons.WeaponFusionService();
            context.SetWeaponFusionService(weaponFusion);
            armorFusion = new ArmorFusionService();
            EnsureMissionServices();
            EnsureAttendanceServices();
            EnsureOfflineTimeTracker();
            EnsureAdSimulationService();
            EnsureGoldDungeonService();
            EnsurePlayerLevelServices();
            if (context.GoldDungeonService != null)
                context.GoldDungeonService.AttachPlayerLevel(context.PlayerLevelService);
            EnsureEXPDungeonService();

            context.Movement.Initialize(context.Wizard, context.EnemySpawner);
            if (context.WeaponInventory != null)
                context.WeaponInventory.Initialize(context.WeaponDatabase);
            EnsureArmorServices();
            if (context.ProjectileFactory != null)
                context.ProjectileFactory.BindWeaponInventory(context.WeaponInventory);
            if (context.GachaService != null)
                context.GachaService.Initialize(context.Wallet, context.WeaponInventory, context.GachaDefinition, context.SaveService, context.CloudFunctionsClient);
            context.UpgradeSystem.Initialize(context.Wallet, context.Wizard, context.Mana);
            context.AutoAttack.Initialize(context.Wizard, context.Movement, context.EnemySpawner, context.ProjectileFactory, calculator);
            context.ClickAttack.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, calculator);
            if (context.ActiveSkill != null)
                context.ActiveSkill.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, context.Mana, calculator);
            if (context.SkillCastOrchestrator != null)
                context.SkillCastOrchestrator.Initialize(context.SkillDatabase, context.Wizard, context.EnemySpawner, context.ProjectileFactory, context.Mana, calculator, context.PlayerLevelService);
            if (context.MissionService != null)
                context.MissionService.Initialize(context.MissionDatabase, context.Wallet, context.EnemySpawner, context.StageManager, context.GachaService, weaponFusion, context.MissionResetService);
            EnsureGoldDungeonEntryPanel();
            context.HUD.Initialize(context.StageManager, context.Wallet, context.Wizard, context.Mana, context.EnemySpawner, context.BossStage, context.UpgradeSystem, context.ActiveSkill, context.ClickAttack, context.Movement, context.ChatService, context.WeaponInventory, context.WeaponDatabase, context.GachaService, context.GachaDefinition, combatPower, weaponFusion, context.SkillCastOrchestrator, context.MissionService, context.AttendanceService, context.GoldDungeonEntryPanel, context.PlayerLevelService, context.PlayerExpBar, context.ArmorInventory, context.ArmorDatabase, armorFusion);
            context.StageManager.Initialize(context.ChapterDatabase, context.EnemySpawner, context.Wallet, context.BossStage, context.Progression);
            context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
            if (context.OfflineTime != null)
                context.OfflineTime.Initialize(context.SaveService, context.SaveBinder, context.MissionResetService);
            EnsureOfflineRewardService();
            EnsureStartupPopupServices();
            EnsureGoldDungeonResultModal();
            EnsureEXPDungeonResultModal();
            if (context.WeaponVisual != null)
                context.WeaponVisual.Bind(context.Wizard, context.WeaponInventory, context.ProjectileFactory);
            if (context.WeaponInventory != null)
                context.WeaponInventory.EquippedChanged += OnWeaponEquipped;
            if (context.ArmorInventory != null)
                context.ArmorInventory.EquippedChanged += OnArmorEquipped;
            combatPower.Initialize(context.Wizard.Stats, context.Mana);
            if (context.CombatPowerPopup != null)
                context.CombatPowerPopup.Bind(combatPower);
            if (context.LevelUpPopup != null)
                context.LevelUpPopup.Bind(context.PlayerLevelService);
            if (context.SkillUnlockPopup != null)
                context.SkillUnlockPopup.Bind(context.PlayerLevelService, context.SkillDatabase);
            if (context.PlayerLevelService != null)
                context.PlayerLevelService.Initialize(context.Wizard.Stats, context.EnemySpawner, combatPower);
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

        private void EnsureArmorServices()
        {
            ArmorInventory inventory = context.ArmorInventory != null
                ? context.ArmorInventory
                : context.GetComponent<ArmorInventory>();
            if (inventory == null)
                inventory = context.gameObject.AddComponent<ArmorInventory>();

            ArmorDatabase database = context.ArmorDatabase;
            inventory.Initialize(database);

            ArmorDropTable dropTable = context.ArmorDropTable != null
                ? context.ArmorDropTable
                : ScriptableObject.CreateInstance<ArmorDropTable>();

            EliteSpawnTracker tracker = context.EliteSpawnTracker != null
                ? context.EliteSpawnTracker
                : context.GetComponent<EliteSpawnTracker>();
            if (tracker == null)
                tracker = context.gameObject.AddComponent<EliteSpawnTracker>();

            tracker.Initialize(context.EnemySpawner, inventory, database, dropTable, context.ArmorAcquiredPopup);
            context.SetArmorServices(database, inventory, armorFusion, tracker, dropTable, context.ArmorAcquiredPopup);
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
            reward.Initialize(context.OfflineTime, context.Wallet, context.StageManager, context.Wizard, context.SaveService, context.PlayerLevelService);
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

        private void EnsureEXPDungeonService()
        {
            EXPDungeonService service = context.EXPDungeonService != null
                ? context.EXPDungeonService
                : context.GetComponent<EXPDungeonService>();
            if (service == null)
                service = context.gameObject.AddComponent<EXPDungeonService>();
            service.Initialize(context.SaveService, context.MissionResetService, context.PlayerLevelService, context.AdSimulation);
            context.SetEXPDungeonService(service);
        }

        private void EnsurePlayerLevelServices()
        {
            PlayerLevelService service = context.PlayerLevelService != null
                ? context.PlayerLevelService
                : context.GetComponent<PlayerLevelService>();
            if (service == null)
                service = context.gameObject.AddComponent<PlayerLevelService>();

            LevelUpPopupView popup = context.LevelUpPopup != null
                ? context.LevelUpPopup
                : FindLevelUpPopupInScene();
            if (popup == null)
                popup = CreateLevelUpPopup();

            SkillUnlockPopupView skillUnlockPopup = context.SkillUnlockPopup != null
                ? context.SkillUnlockPopup
                : FindSkillUnlockPopupInScene();
            if (skillUnlockPopup == null)
                skillUnlockPopup = CreateSkillUnlockPopup();

            context.SetPlayerLevelServices(service, popup, context.PlayerExpBar, skillUnlockPopup);
        }

        private LevelUpPopupView FindLevelUpPopupInScene()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                LevelUpPopupView popup = canvas.GetComponentInChildren<LevelUpPopupView>(true);
                if (popup != null)
                    return popup;
            }

            LevelUpPopupView[] popups = FindObjectsByType<LevelUpPopupView>(FindObjectsInactive.Include);
            return popups != null && popups.Length > 0 ? popups[0] : null;
        }

        private LevelUpPopupView CreateLevelUpPopup()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            Transform parent = canvas != null ? canvas.transform : context.transform;
            GameObject popupGo = new GameObject("LevelUpPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(LevelUpPopupView));
            popupGo.transform.SetParent(parent, false);
            RectTransform rect = popupGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 160f);
            rect.sizeDelta = new Vector2(420f, 120f);
            TMP_Text label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            label.transform.SetParent(popupGo.transform, false);
            RectTransform labelRect = label.transform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.92f, 0.35f, 1f);
            popupGo.SetActive(false);
            return popupGo.GetComponent<LevelUpPopupView>();
        }

        private SkillUnlockPopupView FindSkillUnlockPopupInScene()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                SkillUnlockPopupView popup = canvas.GetComponentInChildren<SkillUnlockPopupView>(true);
                if (popup != null)
                    return popup;
            }

            SkillUnlockPopupView[] popups = FindObjectsByType<SkillUnlockPopupView>(FindObjectsInactive.Include);
            return popups != null && popups.Length > 0 ? popups[0] : null;
        }

        private SkillUnlockPopupView CreateSkillUnlockPopup()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            Transform parent = canvas != null ? canvas.transform : context.transform;
            GameObject popupGo = new GameObject("SkillUnlockPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(SkillUnlockPopupView));
            popupGo.transform.SetParent(parent, false);
            RectTransform rect = popupGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 84f);
            rect.sizeDelta = new Vector2(460f, 100f);
            TMP_Text label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            label.transform.SetParent(popupGo.transform, false);
            RectTransform labelRect = label.transform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.52f, 0.9f, 1f, 1f);
            popupGo.SetActive(false);
            return popupGo.GetComponent<SkillUnlockPopupView>();
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

            entryPanel.Bind(context.GoldDungeonService, context.EXPDungeonService);
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

        private void EnsureEXPDungeonResultModal()
        {
            EXPDungeonResultModal resultModal = context.EXPDungeonResultModal != null
                ? context.EXPDungeonResultModal
                : FindEXPDungeonResultModalInScene();
            if (resultModal == null)
                resultModal = CreateEXPDungeonResultModal();

            resultModal.Bind(context.EXPDungeonService, context.AdSimulation);
            if (context.StartupPopupQueue != null)
                context.StartupPopupQueue.Register(resultModal);
            context.SetEXPDungeonResultModal(resultModal);
        }

        private EXPDungeonResultModal FindEXPDungeonResultModalInScene()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                EXPDungeonResultModal modal = canvas.GetComponentInChildren<EXPDungeonResultModal>(true);
                if (modal != null)
                    return modal;
            }

            EXPDungeonResultModal[] modals = FindObjectsByType<EXPDungeonResultModal>(FindObjectsInactive.Include);
            return modals != null && modals.Length > 0 ? modals[0] : null;
        }

        private EXPDungeonResultModal CreateEXPDungeonResultModal()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            Transform parent = canvas != null ? canvas.transform : context.transform;
            GameObject modalGo = new GameObject("EXPDungeonResultModal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(EXPDungeonResultModal));
            modalGo.transform.SetParent(parent, false);
            RectTransform rect = modalGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            modalGo.transform.SetAsLastSibling();
            modalGo.SetActive(false);
            return modalGo.GetComponent<EXPDungeonResultModal>();
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

            RecomputeEquipmentStats(weapon != null ? weapon.statBonuses : (WizardGrower.Weapons.WeaponStats?)null);
        }

        private void OnArmorEquipped(ArmorDefinition armor)
        {
            RecomputeEquipmentStats(context != null && context.WeaponInventory != null && context.WeaponInventory.Equipped != null
                ? context.WeaponInventory.Equipped.statBonuses
                : (WizardGrower.Weapons.WeaponStats?)null);
        }

        private void RecomputeEquipmentStats(WizardGrower.Weapons.WeaponStats? weaponStats)
        {
            if (context == null || context.Wizard == null)
                return;

            ArmorStats armorStats = context.ArmorInventory != null ? context.ArmorInventory.CaptureEquippedStats() : default;
            context.Wizard.Stats.RecomputeWithEquipment(weaponStats, armorStats);
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
                EnsureCloudFunctionsClient();
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

        private void EnsureCloudFunctionsClient()
        {
            CloudFunctionsClient client = context.CloudFunctionsClient != null
                ? context.CloudFunctionsClient
                : context.GetComponent<CloudFunctionsClient>();
            if (client == null)
                client = context.gameObject.AddComponent<CloudFunctionsClient>();
            client.Initialize();
            context.SetCloudFunctionsClient(client);
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
