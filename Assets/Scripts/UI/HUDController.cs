using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Accessory;
using WizardGrower.Attendance;
using WizardGrower.Armor;
using WizardGrower.Chat;
using WizardGrower.Combat;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Missions;
using WizardGrower.Player;
using WizardGrower.Stages;
using WizardGrower.Skills;
using WizardGrower.Upgrades;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text gemLabel;
        [SerializeField] private TMP_Text enhancementStoneLabel;
        [SerializeField] private TMP_Text attackLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private ManaBarView manaBar;
        [SerializeField] private PlayerHealthBarView playerHealthBar;
        [SerializeField] private BossTimerView bossTimer;
        [SerializeField] private DPSView dpsView;
        [SerializeField] private JoystickIndicatorView joystickIndicator;
        [SerializeField] private Button skillButton;
        [SerializeField] private TMP_Text skillButtonLabel;
        [SerializeField] private Button manualAttackButton;
        [SerializeField] private TMP_Text manualAttackButtonLabel;
        [SerializeField] private Button autoToggleButton;
        [SerializeField] private TMP_Text autoToggleButtonLabel;
        [SerializeField] private Button bossEntryButton;
        [SerializeField] private TMP_Text bossEntryButtonLabel;
        [SerializeField] private Button chatToggleButton;
        [SerializeField] private ChatPanel chatPanel;
        [SerializeField] private WeaponInventoryPanel weaponInventoryPanel;
        [SerializeField] private GachaPanel gachaPanel;
        [SerializeField] private MainUI01Bar mainUI01Bar;
        [SerializeField] private MainUI01Coordinator mainUI01Coordinator;
        [SerializeField] private GoldDungeonEntryPanel goldDungeonEntryPanel;
        [SerializeField] private SkillBarView skillBarView;
        [SerializeField] private SkillTabPanel skillTabPanel;
        [SerializeField] private PlayerExpBarView playerExpBarView;
        [SerializeField] private AchievementButton achievementButton;
        [SerializeField] private AchievementPanel achievementPanel;
        [SerializeField] private AchievementPanel achievementPanelPrefab;
        [SerializeField] private AttendanceButton attendanceButton;
        [SerializeField] private AttendancePanel attendancePanel;
        [SerializeField] private AttendancePanel attendancePanelPrefab;
        [SerializeField] private CombatPowerPopupView combatPowerPopup;
        [SerializeField] private UpgradeDrawerView upgradeDrawer;
        [SerializeField] private Transform upgradeButtonContainer;
        [SerializeField] private UpgradeButtonView upgradeButtonPrefab;
        [SerializeField] private Sprite[] upgradeIcons;

        private ActiveSkillController skillController;
        private ClickAttackController manualAttackController;
        private PlayerMovementController movementController;
        private CombatPowerService combatPowerService;
        private PlayerWizard boundWizard;
        private CurrencyWallet boundWallet;
        private readonly System.Collections.Generic.List<UpgradeButtonView> upgradeButtonViews = new System.Collections.Generic.List<UpgradeButtonView>();
        private float feedbackTimer;

        public void Initialize(
            StageManager stageManager,
            CurrencyWallet wallet,
            PlayerWizard wizard,
            PlayerMana mana,
            EnemySpawner spawner,
            BossStageController bossStageController,
            UpgradeSystem upgradeSystem,
            ActiveSkillController skillController,
            ClickAttackController manualAttackController,
            PlayerMovementController movementController,
            ChatService chatService = null,
            WeaponInventory weaponInventory = null,
            WeaponDatabase weaponDatabase = null,
            GachaService gachaService = null,
            GachaDefinition gachaDefinition = null,
            CombatPowerService combatPowerService = null,
            WeaponFusionService weaponFusionService = null,
            SkillCastOrchestrator skillCastOrchestrator = null,
            MissionService missionService = null,
            AttendanceService attendanceService = null,
            GoldDungeonEntryPanel goldDungeonEntryPanel = null,
            PlayerLevelService playerLevelService = null,
            PlayerExpBarView playerExpBarView = null,
            ArmorInventory armorInventory = null,
            ArmorDatabase armorDatabase = null,
            ArmorFusionService armorFusionService = null,
            AccessoryInventory accessoryInventory = null,
            AccessoryDatabase accessoryDatabase = null,
            AccessoryFusionService accessoryFusionService = null,
            WizardGrower.Enhancement.EnhancementService enhancementService = null,
            EnhancementModal enhancementModal = null)
        {
            this.skillController = skillController;
            this.manualAttackController = manualAttackController;
            this.movementController = movementController;
            this.combatPowerService = combatPowerService;
            boundWallet = wallet;
            if (goldDungeonEntryPanel != null)
                this.goldDungeonEntryPanel = goldDungeonEntryPanel;
            if (playerExpBarView != null)
                this.playerExpBarView = playerExpBarView;
            boundWizard = wizard;

            wallet.GoldChanged += gold =>
            {
                goldLabel.text = $"Gold {gold}";
                RefreshUpgradeButtons();
            };
            if (gemLabel != null)
                wallet.GemsChanged += gems => gemLabel.text = $"다이아 {gems}";
            if (enhancementStoneLabel != null)
                wallet.EnhancementStoneChanged += stones => enhancementStoneLabel.text = $"강화석 {stones}";
            wizard.Stats.Changed += () => RefreshAttack(wizard);
            if (this.combatPowerService != null)
            {
                this.combatPowerService.PowerChanged += _ => RefreshAttack(wizard);
                this.combatPowerService.PowerIncreased += (_, _) => RefreshAttack(wizard);
            }
            if (combatPowerPopup != null)
                combatPowerPopup.Bind(this.combatPowerService);
            mana.Changed += manaBar.Refresh;
            stageManager.StateChanged += OnStateChanged;
            stageManager.BossEntryAvailabilityChanged += OnBossEntryAvailabilityChanged;
            stageManager.Feedback += ShowFeedback;
            spawner.EnemyDamaged += OnEnemyDamaged;
            bossStageController.TimerChanged += bossTimer.Refresh;
            upgradeSystem.UpgradePurchased += (_, _, _) => RefreshUpgradeButtons();
            skillButton.onClick.AddListener(() => skillController.TryCast());
            manualAttackButton.onClick.AddListener(() => manualAttackController.TryFireManual());
            autoToggleButton.onClick.AddListener(() => movementController.ToggleAutoMode());
            if (bossEntryButton != null)
                bossEntryButton.onClick.AddListener(() => stageManager.EnterBossRoom());
            if (chatToggleButton != null && chatPanel != null)
                chatToggleButton.onClick.AddListener(ToggleChatPanel);
            movementController.AutoModeChanged += RefreshAutoToggle;
            if (joystickIndicator != null)
                movementController.JoystickChanged += joystickIndicator.Refresh;
            if (chatPanel != null)
                chatPanel.Initialize(chatService, stageManager);
            if (weaponInventoryPanel != null)
            {
                weaponInventoryPanel.Initialize(weaponInventory, weaponDatabase, weaponFusionService);
                weaponInventoryPanel.InitializeArmor(armorInventory, armorDatabase, armorFusionService);
                weaponInventoryPanel.InitializeAccessory(accessoryInventory, accessoryDatabase, accessoryFusionService);
                weaponInventoryPanel.InitializeEnhancement(enhancementService, enhancementModal);
            }
            if (gachaPanel != null)
                gachaPanel.Initialize(gachaService, gachaDefinition);
            if (skillBarView != null)
                skillBarView.Bind(skillCastOrchestrator, mana);
            if (skillTabPanel != null)
                skillTabPanel.Bind(skillCastOrchestrator);
            EnsureAchievementUi(missionService);
            if (achievementPanel != null)
                achievementPanel.Bind(missionService);
            if (achievementButton != null)
                achievementButton.Bind(achievementPanel);
            EnsureAttendanceUi(attendanceService);
            if (attendancePanel != null)
                attendancePanel.Bind(attendanceService);
            if (attendanceButton != null)
                attendanceButton.Bind(attendancePanel);
            EnsurePlayerExpUi(playerLevelService);
            if (this.playerExpBarView != null)
                this.playerExpBarView.Bind(playerLevelService);
            BindSecondaryPanelCoordinator();
            if (mainUI01Coordinator != null)
                mainUI01Coordinator.Initialize(mainUI01Bar, upgradeDrawer, weaponInventoryPanel, gachaPanel, skillTabPanel, this.goldDungeonEntryPanel);

            BindUpgradeButtons(upgradeSystem);
            RefreshAttack(wizard);
            goldLabel.text = $"Gold {wallet.Gold}";
            if (gemLabel != null)
                gemLabel.text = $"다이아 {wallet.Gems}";
            if (enhancementStoneLabel != null)
                enhancementStoneLabel.text = $"강화석 {wallet.EnhancementStone}";
            manaBar.Refresh(mana.Current, mana.Max);
            if (playerHealthBar != null)
                playerHealthBar.Bind(wizard.Stats);
            RefreshAutoToggle(movementController.AutoModeEnabled);
        }

        private void Update()
        {
            if (skillButtonLabel != null && skillController != null)
            {
                skillButtonLabel.text = skillController.CooldownRemaining > 0f
                    ? $"Skill\n{skillController.CooldownRemaining:0.0}s"
                    : "Skill";
            }

            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f && feedbackLabel != null)
                    feedbackLabel.text = string.Empty;
            }
        }

        private void RefreshAutoToggle(bool enabled)
        {
            if (autoToggleButtonLabel != null)
                autoToggleButtonLabel.text = enabled ? "Auto On" : "Auto Off";
        }

        private void OnStateChanged(ChapterDefinition chapter, StageDefinition stage, StageMode mode)
        {
            if (stageLabel == null || chapter == null || stage == null)
                return;

            string suffix = mode == StageMode.BossRoom ? " BOSS" : "";
            stageLabel.text = $"{chapter.displayName} {chapter.chapterNumber}-{stage.stageNumber}{suffix}";
        }

        private void OnBossEntryAvailabilityChanged(bool available)
        {
            if (bossEntryButton != null)
                bossEntryButton.interactable = available;

            if (bossEntryButtonLabel != null)
                bossEntryButtonLabel.text = "보스 입장";
        }

        private void OnEnemyDamaged(EnemyBase enemy, DamageInfo info)
        {
            dpsView.Record(info);
        }

        private void RefreshAttack(PlayerWizard wizard)
        {
            if (attackLabel == null)
                return;
            float power = combatPowerService != null ? combatPowerService.CurrentPower : wizard.Stats.CombatPower;
            attackLabel.text = $"전투력 {power:0}";
        }

        public void BindCombatPower(CombatPowerService service)
        {
            if (combatPowerService != null)
            {
                combatPowerService.PowerChanged -= OnCombatPowerChanged;
                combatPowerService.PowerIncreased -= OnCombatPowerIncreased;
            }

            combatPowerService = service;
            if (combatPowerService != null)
            {
                combatPowerService.PowerChanged += OnCombatPowerChanged;
                combatPowerService.PowerIncreased += OnCombatPowerIncreased;
            }

            if (combatPowerPopup != null)
                combatPowerPopup.Bind(combatPowerService);
            if (boundWizard != null)
                RefreshAttack(boundWizard);
        }

        private void OnCombatPowerChanged(float _)
        {
            if (boundWizard != null)
                RefreshAttack(boundWizard);
        }

        private void OnCombatPowerIncreased(float current, float delta)
        {
            if (boundWizard != null)
                RefreshAttack(boundWizard);
        }

        private void BindUpgradeButtons(UpgradeSystem system)
        {
            upgradeButtonViews.Clear();
            if (upgradeButtonContainer == null || upgradeButtonPrefab == null)
                return;

            for (int i = upgradeButtonContainer.childCount - 1; i >= 0; i--)
                Destroy(upgradeButtonContainer.GetChild(i).gameObject);

            for (int i = 0; i < system.Upgrades.Count; i++)
            {
                Sprite icon = i < upgradeIcons.Length ? upgradeIcons[i] : null;
                UpgradeButtonView view = Instantiate(upgradeButtonPrefab, upgradeButtonContainer);
                view.Bind(system, system.Upgrades[i], icon, boundWallet);
                upgradeButtonViews.Add(view);
            }
        }

        private void RefreshUpgradeButtons()
        {
            foreach (UpgradeButtonView view in upgradeButtonViews)
                view.Refresh();
        }

        private void EnsureAchievementUi(MissionService missionService)
        {
            if (missionService == null || achievementButton != null && achievementPanel != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.transform : transform;

            if (achievementPanel == null)
            {
                if (achievementPanelPrefab != null)
                {
                    achievementPanel = Instantiate(achievementPanelPrefab, root, false);
                    achievementPanel.name = "AchievementPanel";
                }
            }
        }

        private void EnsureAttendanceUi(AttendanceService attendanceService)
        {
            if (attendanceService == null || attendanceButton != null && attendancePanel != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.transform : transform;

            if (attendancePanel == null)
            {
                if (attendancePanelPrefab != null)
                {
                    attendancePanel = Instantiate(attendancePanelPrefab, root, false);
                    attendancePanel.name = "AttendancePanel";
                }
            }
        }

        private void EnsurePlayerExpUi(PlayerLevelService playerLevelService)
        {
            if (playerLevelService == null || playerExpBarView != null)
                return;

        }

        private void BindSecondaryPanelCoordinator()
        {
            if (achievementPanel != null)
                achievementPanel.OpenStateChanged += open =>
                {
                    if (!open)
                        return;
                    attendancePanel?.Close();
                    chatPanel?.SetVisible(false);
                };

            if (attendancePanel != null)
                attendancePanel.OpenStateChanged += open =>
                {
                    if (!open)
                        return;
                    achievementPanel?.Close();
                    chatPanel?.SetVisible(false);
                };
        }

        private void ToggleChatPanel()
        {
            if (chatPanel == null)
                return;

            bool show = !chatPanel.IsVisible;
            chatPanel.SetVisible(show);
            if (show)
            {
                achievementPanel?.Close();
                attendancePanel?.Close();
            }
        }

        private void ShowFeedback(string message)
        {
            if (feedbackLabel == null)
                return;

            feedbackTimer = 1.5f;
            feedbackLabel.text = message;
        }
    }
}
