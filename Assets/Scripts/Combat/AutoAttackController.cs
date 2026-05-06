using UnityEngine;
using WizardGrower.Enemies;
using WizardGrower.Player;

namespace WizardGrower.Combat
{
    public class AutoAttackController : MonoBehaviour
    {
        [SerializeField] private PlayerWizard wizard;
        [SerializeField] private PlayerMovementController movement;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ProjectileFactory projectileFactory;

        private CombatCalculator calculator;
        private float timer;

        public float LastFireTime { get; private set; } = -1f;
        public int UpdateTicks { get; private set; }

        public void Initialize(PlayerWizard wizard, PlayerMovementController movement, EnemySpawner enemySpawner, ProjectileFactory projectileFactory, CombatCalculator calculator)
        {
            this.wizard = wizard;
            this.movement = movement;
            this.enemySpawner = enemySpawner;
            this.projectileFactory = projectileFactory;
            this.calculator = calculator;
            timer = wizard != null ? wizard.Stats.AutoAttackInterval : 0f;
        }

        private void Update()
        {
            UpdateTicks++;
            if (movement != null && (!movement.AutoModeEnabled || movement.IsManualMoving))
                return;

            if (wizard == null || projectileFactory == null || calculator == null)
            {
                TryRepairCalculator();
                if (wizard == null || projectileFactory == null || calculator == null)
                    return;
            }

            timer += Time.deltaTime;
            if (timer < wizard.Stats.AutoAttackInterval)
                return;

            timer = 0f;
            TryFireNow();
        }

        public bool TryFireNow()
        {
            TryRepairCalculator();
            if (movement != null && (!movement.AutoModeEnabled || movement.IsManualMoving))
                return false;

            if (wizard == null || projectileFactory == null || calculator == null)
                return false;

            IDamageable target = enemySpawner != null ? enemySpawner.CurrentEnemy : null;
            if (target == null || !target.IsAlive)
                return false;

            LastFireTime = Time.time;
            projectileFactory.FireAuto(wizard.CastPoint.position, target, calculator.Auto(wizard.gameObject));
            return true;
        }

        private void TryRepairCalculator()
        {
            if (calculator == null && wizard != null)
                calculator = new CombatCalculator(wizard.Stats);
        }
    }
}
