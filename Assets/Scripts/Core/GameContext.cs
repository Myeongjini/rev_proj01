using UnityEngine;
using WizardGrower.Ads;
using WizardGrower.Attendance;
using WizardGrower.Auth;
using WizardGrower.Chat;
using WizardGrower.Combat;
using WizardGrower.Dungeons;
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
        [field: SerializeField] public GoldDungeonService GoldDungeonService { get; private set; }
        [field: SerializeField] public CombatPowerPopupView CombatPowerPopup { get; private set; }
        public CombatPowerService CombatPower { get; private set; }
        public WeaponFusionService WeaponFusion { get; private set; }
        public AttendanceService AttendanceService { get; private set; }

        public void SetCombatPowerService(CombatPowerService combatPower)
        {
            CombatPower = combatPower;
        }

        public void SetWeaponFusionService(WeaponFusionService weaponFusion)
        {
            WeaponFusion = weaponFusion;
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

        public void SetGoldDungeonService(GoldDungeonService service)
        {
            if (service != null)
                GoldDungeonService = service;
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
