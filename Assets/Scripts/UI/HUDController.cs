using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Chat;
using WizardGrower.Combat;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Player;
using WizardGrower.Stages;
using WizardGrower.Upgrades;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private TMP_Text goldLabel;
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
        [SerializeField] private Button weaponInventoryToggleButton;
        [SerializeField] private WeaponInventoryPanel weaponInventoryPanel;
        [SerializeField] private UpgradeDrawerView upgradeDrawer;
        [SerializeField] private Transform upgradeButtonContainer;
        [SerializeField] private UpgradeButtonView upgradeButtonPrefab;
        [SerializeField] private Sprite[] upgradeIcons;

        private ActiveSkillController skillController;
        private ClickAttackController manualAttackController;
        private PlayerMovementController movementController;
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
            WeaponDatabase weaponDatabase = null)
        {
            this.skillController = skillController;
            this.manualAttackController = manualAttackController;
            this.movementController = movementController;

            wallet.GoldChanged += gold => goldLabel.text = $"Gold {gold}";
            wizard.Stats.Changed += () => RefreshAttack(wizard);
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
                chatToggleButton.onClick.AddListener(chatPanel.Toggle);
            if (weaponInventoryToggleButton != null && weaponInventoryPanel != null)
                weaponInventoryToggleButton.onClick.AddListener(weaponInventoryPanel.Toggle);
            movementController.AutoModeChanged += RefreshAutoToggle;
            if (joystickIndicator != null)
                movementController.JoystickChanged += joystickIndicator.Refresh;
            if (chatPanel != null)
                chatPanel.Initialize(chatService, stageManager);
            if (weaponInventoryPanel != null)
                weaponInventoryPanel.Initialize(weaponInventory, weaponDatabase);

            BindUpgradeButtons(upgradeSystem);
            RefreshAttack(wizard);
            goldLabel.text = $"Gold {wallet.Gold}";
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
            attackLabel.text = $"ATK {wizard.Stats.AutoAttackDamage:0}  CP {wizard.Stats.CombatPower:0}";
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
                view.Bind(system, system.Upgrades[i], icon);
                upgradeButtonViews.Add(view);
            }
        }

        private void RefreshUpgradeButtons()
        {
            foreach (UpgradeButtonView view in upgradeButtonViews)
                view.Refresh();
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
