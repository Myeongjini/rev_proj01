using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WizardGrower.Accessory;
using WizardGrower.Ads;
using WizardGrower.Armor;
using WizardGrower.Attendance;
using WizardGrower.Auth;
using WizardGrower.Cloud;
using WizardGrower.Combat;
using WizardGrower.Dungeons;
using WizardGrower.Drops;
using WizardGrower.Enhancement;
using WizardGrower.Enemies;
using WizardGrower.Login;
using WizardGrower.Missions;
using WizardGrower.Offline;
using WizardGrower.Player;
using WizardGrower.Ranking;
using WizardGrower.UI;

namespace WizardGrower.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameContext context;
        [SerializeField] private LevelUpPopupView levelUpPopupPrefab;
        [SerializeField] private SkillUnlockPopupView skillUnlockPopupPrefab;
        [SerializeField] private WizardGrower.UI.GoldDungeonEntryPanel goldDungeonEntryPanelPrefab;
        [SerializeField] private GoldDungeonResultModal goldDungeonResultModalPrefab;
        [SerializeField] private EXPDungeonResultModal expDungeonResultModalPrefab;
        [SerializeField] private EnhancementStoneDungeonResultModal enhancementStoneDungeonResultModalPrefab;
        [SerializeField] private EnhancementModal enhancementModalPrefab;
        [SerializeField] private OfflineRewardModal offlineRewardModalPrefab;

        private CombatCalculator calculator;
        private CombatPowerService combatPower;
        private WizardGrower.Weapons.WeaponFusionService weaponFusion;
        private ArmorFusionService armorFusion;
        private AccessoryFusionService accessoryFusion;

        private void Awake()
        {
            if (context == null)
                context = GetComponent<GameContext>();

            context.SaveService.TryLoad();
            calculator = new CombatCalculator(context.Wizard.Stats);
            combatPower = new CombatPowerService();
            context.SetCombatPowerService(combatPower);
            EnsureRankingService();
            EnsureCloudFunctionsClient();
            if (context.Wallet != null)
                context.Wallet.InitializeAuthority(context.CloudFunctionsClient);
            weaponFusion = new WizardGrower.Weapons.WeaponFusionService();
            context.SetWeaponFusionService(weaponFusion);
            armorFusion = new ArmorFusionService();
            accessoryFusion = new AccessoryFusionService();
            EnsureMissionServices();
            EnsureAttendanceServices();
            EnsureOfflineTimeTracker();
            EnsureAdSimulationService();
            EnsureGoldDungeonService();
            EnsurePlayerLevelServices();
            if (context.GoldDungeonService != null)
                context.GoldDungeonService.AttachPlayerLevel(context.PlayerLevelService);
            EnsureEXPDungeonService();
            EnsureEnhancementStoneDungeonService();

            context.Movement.Initialize(context.Wizard, context.EnemySpawner);
            if (context.WeaponInventory != null)
                context.WeaponInventory.Initialize(context.WeaponDatabase);
            EnsureArmorServices();
            EnsureAccessoryServices();
            EnsureEnhancementService();
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
            context.HUD.Initialize(context.StageManager, context.Wallet, context.Wizard, context.Mana, context.EnemySpawner, context.BossStage, context.UpgradeSystem, context.ActiveSkill, context.ClickAttack, context.Movement, context.ChatService, context.WeaponInventory, context.WeaponDatabase, context.GachaService, context.GachaDefinition, combatPower, weaponFusion, context.SkillCastOrchestrator, context.MissionService, context.AttendanceService, context.GoldDungeonEntryPanel, context.PlayerLevelService, context.PlayerExpBar, context.ArmorInventory, context.ArmorDatabase, armorFusion, context.AccessoryInventory, context.AccessoryDatabase, accessoryFusion, context.EnhancementService, context.EnhancementModal);
            context.StageManager.Initialize(context.ChapterDatabase, context.EnemySpawner, context.Wallet, context.BossStage, context.Progression);
            context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
            if (context.OfflineTime != null)
                context.OfflineTime.Initialize(context.SaveService, context.SaveBinder, context.MissionResetService);
            EnsureOfflineRewardService();
            EnsureStartupPopupServices();
            EnsureGoldDungeonResultModal();
            EnsureEXPDungeonResultModal();
            EnsureEnhancementStoneDungeonResultModal();
            EnsureEnhancementModal();
            if (context.WeaponVisual != null)
                context.WeaponVisual.Bind(context.Wizard, context.WeaponInventory, context.ProjectileFactory);
            if (context.WeaponInventory != null)
                context.WeaponInventory.EquippedChanged += OnWeaponEquipped;
            if (context.ArmorInventory != null)
                context.ArmorInventory.EquippedChanged += OnArmorEquipped;
            if (context.AccessoryInventory != null)
                context.AccessoryInventory.EquippedChanged += OnAccessoryEquipped;
            if (context.EnhancementService != null)
                context.EnhancementService.EnhancementChanged += OnEnhancementChanged;
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

            tracker.Initialize(context.EnemySpawner, inventory, database, dropTable, context.ArmorAcquiredPopup, context.AccessoryInventory, context.AccessoryDatabase, context.LootDropTable, context.AccessoryAcquiredPopup);
            context.SetArmorServices(database, inventory, armorFusion, tracker, dropTable, context.ArmorAcquiredPopup);
        }

        private void EnsureAccessoryServices()
        {
            AccessoryInventory inventory = context.AccessoryInventory != null
                ? context.AccessoryInventory
                : context.GetComponent<AccessoryInventory>();
            if (inventory == null)
                inventory = context.gameObject.AddComponent<AccessoryInventory>();

            AccessoryDatabase database = context.AccessoryDatabase;
            inventory.Initialize(database);

            LootDropTable dropTable = context.LootDropTable != null
                ? context.LootDropTable
                : ScriptableObject.CreateInstance<LootDropTable>();

            if (context.EliteSpawnTracker != null)
                context.EliteSpawnTracker.Initialize(context.EnemySpawner, context.ArmorInventory, context.ArmorDatabase, context.ArmorDropTable, context.ArmorAcquiredPopup, inventory, database, dropTable, context.AccessoryAcquiredPopup);

            context.SetAccessoryServices(database, inventory, accessoryFusion, dropTable, context.AccessoryAcquiredPopup);
        }

        private void EnsureEnhancementService()
        {
            EnhancementService service = context.EnhancementService != null
                ? context.EnhancementService
                : context.GetComponent<EnhancementService>();
            if (service == null)
                service = context.gameObject.AddComponent<EnhancementService>();

            service.Initialize(context.Wallet, context.WeaponInventory, context.ArmorInventory, context.AccessoryInventory, context.SaveService, context.CloudFunctionsClient);
            context.SetEnhancementService(service);
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

        private void EnsureEnhancementStoneDungeonService()
        {
            EnhancementStoneDungeonService service = context.EnhancementStoneDungeonService != null
                ? context.EnhancementStoneDungeonService
                : context.GetComponent<EnhancementStoneDungeonService>();
            if (service == null)
                service = context.gameObject.AddComponent<EnhancementStoneDungeonService>();
            service.Initialize(context.SaveService, context.MissionResetService, context.Wallet, context.PlayerLevelService, context.AdSimulation);
            context.SetEnhancementStoneDungeonService(service);
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
                popup = InstantiateUiPrefab(levelUpPopupPrefab, "LevelUpPopup");

            SkillUnlockPopupView skillUnlockPopup = context.SkillUnlockPopup != null
                ? context.SkillUnlockPopup
                : FindSkillUnlockPopupInScene();
            if (skillUnlockPopup == null)
                skillUnlockPopup = InstantiateUiPrefab(skillUnlockPopupPrefab, "SkillUnlockPopup");

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
                modal = InstantiateUiPrefab(offlineRewardModalPrefab, "OfflineRewardModal");
            if (modal == null)
                return;

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
                entryPanel = InstantiateUiPrefab(goldDungeonEntryPanelPrefab, "GoldDungeonEntryPanel");
            if (entryPanel == null)
                return;

            entryPanel.Bind(context.GoldDungeonService, context.EXPDungeonService, context.EnhancementStoneDungeonService);
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

        private void EnsureGoldDungeonResultModal()
        {
            GoldDungeonResultModal resultModal = context.GoldDungeonResultModal != null
                ? context.GoldDungeonResultModal
                : FindGoldDungeonResultModalInScene();
            if (resultModal == null)
                resultModal = InstantiateUiPrefab(goldDungeonResultModalPrefab, "GoldDungeonResultModal");
            if (resultModal == null)
                return;

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
                resultModal = InstantiateUiPrefab(expDungeonResultModalPrefab, "EXPDungeonResultModal");
            if (resultModal == null)
                return;

            resultModal.Bind(context.EXPDungeonService, context.AdSimulation);
            if (context.StartupPopupQueue != null)
                context.StartupPopupQueue.Register(resultModal);
            context.SetEXPDungeonResultModal(resultModal);
        }

        private void EnsureEnhancementStoneDungeonResultModal()
        {
            EnhancementStoneDungeonResultModal resultModal = context.EnhancementStoneDungeonResultModal != null
                ? context.EnhancementStoneDungeonResultModal
                : FindEnhancementStoneDungeonResultModalInScene();
            if (resultModal == null)
                resultModal = InstantiateUiPrefab(enhancementStoneDungeonResultModalPrefab, "EnhancementStoneDungeonResultModal");
            if (resultModal == null)
                return;

            resultModal.Bind(context.EnhancementStoneDungeonService, context.AdSimulation);
            if (context.StartupPopupQueue != null)
                context.StartupPopupQueue.Register(resultModal);
            context.SetEnhancementStoneDungeonResultModal(resultModal);
        }

        private EnhancementStoneDungeonResultModal FindEnhancementStoneDungeonResultModalInScene()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                EnhancementStoneDungeonResultModal modal = canvas.GetComponentInChildren<EnhancementStoneDungeonResultModal>(true);
                if (modal != null)
                    return modal;
            }

            EnhancementStoneDungeonResultModal[] modals = FindObjectsByType<EnhancementStoneDungeonResultModal>(FindObjectsInactive.Include);
            return modals != null && modals.Length > 0 ? modals[0] : null;
        }

        private void EnsureEnhancementModal()
        {
            EnhancementModal modal = context.EnhancementModal != null
                ? context.EnhancementModal
                : FindEnhancementModalInScene();
            if (modal == null)
                modal = InstantiateUiPrefab(enhancementModalPrefab, "EnhancementModal");
            if (modal == null)
                return;

            modal.Bind(context.EnhancementService, context.Wallet);
            context.SetEnhancementModal(modal);
        }

        private EnhancementModal FindEnhancementModalInScene()
        {
            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                EnhancementModal modal = canvas.GetComponentInChildren<EnhancementModal>(true);
                if (modal != null)
                    return modal;
            }

            EnhancementModal[] modals = FindObjectsByType<EnhancementModal>(FindObjectsInactive.Include);
            return modals != null && modals.Length > 0 ? modals[0] : null;
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

        private T InstantiateUiPrefab<T>(T prefab, string instanceName) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError($"[GameManager] {instanceName} prefab is not assigned. Assign the matching UI prefab in the inspector.");
                return null;
            }

            Canvas canvas = context.HUD != null ? context.HUD.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            Transform parent = canvas != null ? canvas.transform : context.transform;
            T instance = Instantiate(prefab, parent, false);
            instance.name = instanceName;
            instance.transform.SetAsLastSibling();
            instance.gameObject.SetActive(false);
            return instance;
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

        private void OnAccessoryEquipped(AccessoryDefinition accessory)
        {
            RecomputeEquipmentStats(context != null && context.WeaponInventory != null && context.WeaponInventory.Equipped != null
                ? context.WeaponInventory.Equipped.statBonuses
                : (WizardGrower.Weapons.WeaponStats?)null);
        }

        private void OnEnhancementChanged(EnhancementSlotKind slotKind, string itemId, int level)
        {
            RecomputeEquipmentStats(context != null && context.WeaponInventory != null && context.WeaponInventory.Equipped != null
                ? context.WeaponInventory.Equipped.statBonuses
                : (WizardGrower.Weapons.WeaponStats?)null);
            if (context != null && context.CombatPower != null)
                context.CombatPower.Recalculate(true);
        }

        private void RecomputeEquipmentStats(WizardGrower.Weapons.WeaponStats? weaponStats)
        {
            if (context == null || context.Wizard == null)
                return;

            ArmorStats armorStats = context.ArmorInventory != null ? context.ArmorInventory.CaptureEquippedStats() : default;
            AccessoryStats accessoryStats = context.AccessoryInventory != null ? context.AccessoryInventory.CaptureEquippedStats() : default;
            int weaponEnhancementLevel = context.WeaponInventory != null ? context.WeaponInventory.GetEnhancementLevel(context.WeaponInventory.EquippedWeaponId) : 0;
            context.Wizard.Stats.RecomputeWithEquipment(weaponStats, weaponEnhancementLevel, armorStats, accessoryStats);
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
                if (context.RankingService != null)
                {
                    context.RankingService.Initialize(context.AuthService, context.UserProfileService, combatPower);
                    await context.RankingService.PushMyCombatPowerScoreAsync(displayName);
                }
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

        private void EnsureRankingService()
        {
            RankingService service = context.RankingService != null
                ? context.RankingService
                : context.GetComponent<RankingService>();
            if (service == null)
                service = context.gameObject.AddComponent<RankingService>();

            service.Initialize(context.AuthService, context.UserProfileService, combatPower);
            context.SetRankingService(service);
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
                if (context.RankingService != null)
                {
                    context.RankingService.Initialize(context.AuthService, context.UserProfileService, combatPower);
                    await context.RankingService.PushMyCombatPowerScoreAsync();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Profile update failed: {ex.Message}");
            }
        }
    }
}
