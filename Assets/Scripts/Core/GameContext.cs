using UnityEngine;
using WizardGrower.Ads;
using WizardGrower.Armor;
using WizardGrower.Attendance;
using WizardGrower.Auth;
using WizardGrower.Chat;
using WizardGrower.Cloud;
using WizardGrower.Combat;
using WizardGrower.Dungeons;
using WizardGrower.Drops;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Missions;
using WizardGrower.Multiplayer;
using WizardGrower.Offline;
using WizardGrower.Player;
using WizardGrower.Save;
using WizardGrower.Skills;
using WizardGrower.Stages;
using WizardGrower.Upgrades;
using WizardGrower.UI;
using WizardGrower.Weapons;

namespace WizardGrower.Core
{
    public class GameContext : MonoBehaviour
    {
        [field: SerializeField] public PlayerWizard Wizard { get; private set; }
        [field: SerializeField] public PlayerMovementController Movement { get; private set; }
        [field: SerializeField] public PlayerMana Mana { get; private set; }
        [field: SerializeField] public PlayerProgression Progression { get; private set; }
        [field: SerializeField] public CurrencyWallet Wallet { get; private set; }
        [field: SerializeField] public EnemySpawner EnemySpawner { get; private set; }
        [field: SerializeField] public ProjectileFactory ProjectileFactory { get; private set; }
        [field: SerializeField] public AutoAttackController AutoAttack { get; private set; }
        [field: SerializeField] public ClickAttackController ClickAttack { get; private set; }
        [field: SerializeField] public ActiveSkillController ActiveSkill { get; private set; }
        [field: SerializeField] public ChapterDatabase ChapterDatabase { get; private set; }
        [field: SerializeField] public StageManager StageManager { get; private set; }
        [field: SerializeField] public BossStageController BossStage { get; private set; }
        [field: SerializeField] public UpgradeSystem UpgradeSystem { get; private set; }
        [field: SerializeField] public SaveService SaveService { get; private set; }
        [field: SerializeField] public SaveBinder SaveBinder { get; private set; }
        [field: SerializeField] public CloudSyncService CloudSyncService { get; private set; }
        [field: SerializeField] public CloudFunctionsClient CloudFunctionsClient { get; private set; }
        [field: SerializeField] public SyncCoordinator SyncCoordinator { get; private set; }
        [field: SerializeField] public PresenceService PresenceService { get; private set; }
        [field: SerializeField] public PresenceCoordinator PresenceCoordinator { get; private set; }
        [field: SerializeField] public RemotePlayerView RemoteWizardPrefab { get; private set; }
        [field: SerializeField] public ChatService ChatService { get; private set; }
        [field: SerializeField] public AuthService AuthService { get; private set; }
        [field: SerializeField] public UserProfileService UserProfileService { get; private set; }
        [field: SerializeField] public AuthConfig AuthConfig { get; private set; }
        [field: SerializeField] public HUDController HUD { get; private set; }
        [field: SerializeField] public FloatingTextSpawner FloatingText { get; private set; }
        [field: SerializeField] public WeaponDatabase WeaponDatabase { get; private set; }
        [field: SerializeField] public WeaponInventory WeaponInventory { get; private set; }
        [field: SerializeField] public WeaponVisualController WeaponVisual { get; private set; }
        [field: SerializeField] public ArmorDatabase ArmorDatabase { get; private set; }
        [field: SerializeField] public ArmorInventory ArmorInventory { get; private set; }
        [field: SerializeField] public ArmorDropTable ArmorDropTable { get; private set; }
        [field: SerializeField] public EliteSpawnTracker EliteSpawnTracker { get; private set; }
        [field: SerializeField] public ArmorAcquiredPopupView ArmorAcquiredPopup { get; private set; }
        [field: SerializeField] public GachaDefinition GachaDefinition { get; private set; }
        [field: SerializeField] public GachaService GachaService { get; private set; }
        [field: SerializeField] public SkillDatabase SkillDatabase { get; private set; }
        [field: SerializeField] public SkillCastOrchestrator SkillCastOrchestrator { get; private set; }
        [field: SerializeField] public MissionDatabase MissionDatabase { get; private set; }
        [field: SerializeField] public MissionService MissionService { get; private set; }
        [field: SerializeField] public MissionResetService MissionResetService { get; private set; }
        [field: SerializeField] public AttendanceConfig AttendanceConfig { get; private set; }
        [field: SerializeField] public OfflineTimeTracker OfflineTime { get; private set; }
        [field: SerializeField] public OfflineRewardService OfflineReward { get; private set; }
        [field: SerializeField] public OfflineRewardModal OfflineRewardModal { get; private set; }
        [field: SerializeField] public GameStartupPopupQueue StartupPopupQueue { get; private set; }
        [field: SerializeField] public AdSimulationService AdSimulation { get; private set; }
        [field: SerializeField] public GoldDungeonEntryPanel GoldDungeonEntryPanel { get; private set; }
        [field: SerializeField] public GoldDungeonResultModal GoldDungeonResultModal { get; private set; }
        [field: SerializeField] public GoldDungeonService GoldDungeonService { get; private set; }
        [field: SerializeField] public EXPDungeonService EXPDungeonService { get; private set; }
        [field: SerializeField] public EXPDungeonResultModal EXPDungeonResultModal { get; private set; }
        [field: SerializeField] public PlayerLevelService PlayerLevelService { get; private set; }
        [field: SerializeField] public LevelUpPopupView LevelUpPopup { get; private set; }
        [field: SerializeField] public PlayerExpBarView PlayerExpBar { get; private set; }
        [field: SerializeField] public SkillUnlockPopupView SkillUnlockPopup { get; private set; }
        [field: SerializeField] public CombatPowerPopupView CombatPowerPopup { get; private set; }
        public CombatPowerService CombatPower { get; private set; }
        public WeaponFusionService WeaponFusion { get; private set; }
        public ArmorFusionService ArmorFusion { get; private set; }
        public AttendanceService AttendanceService { get; private set; }

        public void SetCombatPowerService(CombatPowerService combatPower)
        {
            CombatPower = combatPower;
        }

        public void SetWeaponFusionService(WeaponFusionService weaponFusion)
        {
            WeaponFusion = weaponFusion;
        }

        public void SetArmorServices(ArmorDatabase database, ArmorInventory inventory, ArmorFusionService fusion, EliteSpawnTracker eliteSpawnTracker = null, ArmorDropTable dropTable = null, ArmorAcquiredPopupView popup = null)
        {
            if (database != null)
                ArmorDatabase = database;
            if (inventory != null)
                ArmorInventory = inventory;
            if (fusion != null)
                ArmorFusion = fusion;
            if (eliteSpawnTracker != null)
                EliteSpawnTracker = eliteSpawnTracker;
            if (dropTable != null)
                ArmorDropTable = dropTable;
            if (popup != null)
                ArmorAcquiredPopup = popup;
        }

        public void SetMissionServices(MissionDatabase missionDatabase, MissionService missionService, MissionResetService missionResetService)
        {
            if (missionDatabase != null)
                MissionDatabase = missionDatabase;
            if (missionService != null)
                MissionService = missionService;
            if (missionResetService != null)
                MissionResetService = missionResetService;
        }

        public void SetAttendanceServices(AttendanceConfig attendanceConfig, AttendanceService attendanceService)
        {
            if (attendanceConfig != null)
                AttendanceConfig = attendanceConfig;
            if (attendanceService != null)
                AttendanceService = attendanceService;
        }

        public void SetOfflineServices(OfflineTimeTracker offlineTime, OfflineRewardService offlineReward = null)
        {
            if (offlineTime != null)
                OfflineTime = offlineTime;
            if (offlineReward != null)
                OfflineReward = offlineReward;
        }

        public void SetStartupPopupServices(GameStartupPopupQueue popupQueue, OfflineRewardModal offlineRewardModal, AdSimulationService adSimulation)
        {
            if (popupQueue != null)
                StartupPopupQueue = popupQueue;
            if (offlineRewardModal != null)
                OfflineRewardModal = offlineRewardModal;
            if (adSimulation != null)
                AdSimulation = adSimulation;
        }

        public void SetGoldDungeonEntryPanel(GoldDungeonEntryPanel entryPanel)
        {
            if (entryPanel != null)
                GoldDungeonEntryPanel = entryPanel;
        }

        public void SetGoldDungeonResultModal(GoldDungeonResultModal resultModal)
        {
            if (resultModal != null)
                GoldDungeonResultModal = resultModal;
        }

        public void SetGoldDungeonService(GoldDungeonService service)
        {
            if (service != null)
                GoldDungeonService = service;
        }

        public void SetEXPDungeonService(EXPDungeonService service)
        {
            if (service != null)
                EXPDungeonService = service;
        }

        public void SetEXPDungeonResultModal(EXPDungeonResultModal resultModal)
        {
            if (resultModal != null)
                EXPDungeonResultModal = resultModal;
        }

        public void SetPlayerLevelServices(PlayerLevelService service, LevelUpPopupView popup = null, PlayerExpBarView expBar = null, SkillUnlockPopupView skillUnlockPopup = null)
        {
            if (service != null)
                PlayerLevelService = service;
            if (popup != null)
                LevelUpPopup = popup;
            if (expBar != null)
                PlayerExpBar = expBar;
            if (skillUnlockPopup != null)
                SkillUnlockPopup = skillUnlockPopup;
        }

        public void SetCloudFunctionsClient(CloudFunctionsClient client)
        {
            if (client != null)
                CloudFunctionsClient = client;
        }

        public void SetAuthenticationServices(AuthService authService, UserProfileService userProfileService, AuthConfig authConfig, CloudSyncService cloudSyncService = null)
        {
            AuthService = authService;
            UserProfileService = userProfileService;
            AuthConfig = authConfig;
            if (cloudSyncService != null)
                CloudSyncService = cloudSyncService;
        }
    }
}
