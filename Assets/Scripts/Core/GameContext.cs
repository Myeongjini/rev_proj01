using UnityEngine;
using WizardGrower.Auth;
using WizardGrower.Chat;
using WizardGrower.Combat;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Multiplayer;
using WizardGrower.Player;
using WizardGrower.Save;
using WizardGrower.Stages;
using WizardGrower.Upgrades;
using WizardGrower.UI;

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
