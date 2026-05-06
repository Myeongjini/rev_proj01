using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Combat;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Player;
using WizardGrower.Stages;
using WizardGrower.Upgrades;

namespace WizardGrower.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text attackLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private ManaBarView manaBar;
        [SerializeField] private HealthBarView healthBar;
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
        [SerializeField] private UpgradeButtonView[] upgradeButtons;
        [SerializeField] private Sprite[] upgradeIcons;

        private ActiveSkillController skillController;
        private ClickAttackController manualAttackController;
        private PlayerMovementController movementController;
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
            PlayerMovementController movementController)
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
            spawner.EnemySpawned += healthBar.Bind;
            spawner.EnemyDamaged += OnEnemyDamaged;
            bossStageController.TimerChanged += bossTimer.Refresh;
            upgradeSystem.UpgradePurchased += (_, _, _) => RefreshUpgradeButtons();
            skillButton.onClick.AddListener(() => skillController.TryCast());
            manualAttackButton.onClick.AddListener(() => manualAttackController.TryFireManual());
            autoToggleButton.onClick.AddListener(() => movementController.ToggleAutoMode());
            if (bossEntryButton != null)
                bossEntryButton.onClick.AddListener(() => stageManager.EnterBossRoom());
            movementController.AutoModeChanged += RefreshAutoToggle;
            if (joystickIndicator != null)
                movementController.JoystickChanged += joystickIndicator.Refresh;

            BindUpgradeButtons(upgradeSystem);
            RefreshAttack(wizard);
            goldLabel.text = $"Gold {wallet.Gold}";
            manaBar.Refresh(mana.Current, mana.Max);
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
            for (int i = 0; i < upgradeButtons.Length && i < system.Upgrades.Count; i++)
            {
                Sprite icon = i < upgradeIcons.Length ? upgradeIcons[i] : null;
                upgradeButtons[i].Bind(system, system.Upgrades[i], icon);
            }
        }

        private void RefreshUpgradeButtons()
        {
            foreach (UpgradeButtonView view in upgradeButtons)
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
