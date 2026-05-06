using UnityEngine;
using WizardGrower.Enemies;
using WizardGrower.Player;

namespace WizardGrower.Combat
{
    public class ClickAttackController : MonoBehaviour
    {
        [SerializeField] private PlayerWizard wizard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ProjectileFactory projectileFactory;

        private CombatCalculator calculator;
        public void Initialize(PlayerWizard wizard, EnemySpawner enemySpawner, ProjectileFactory projectileFactory, CombatCalculator calculator)
        {
            this.wizard = wizard;
            this.enemySpawner = enemySpawner;
            this.projectileFactory = projectileFactory;
            this.calculator = calculator;
        }

        public bool TryFireManual()
        {
            TryRepairCalculator();
            if (wizard == null || projectileFactory == null || calculator == null)
                return false;

            IDamageable target = enemySpawner != null ? enemySpawner.GetNearestEnemy(wizard.transform.position) : null;
            if (target == null || !target.IsAlive)
                return false;

            projectileFactory.FireManual(wizard.CastPoint.position, target, calculator.Manual(wizard.gameObject));
            return true;
        }

        private void TryRepairCalculator()
        {
            if (calculator == null && wizard != null)
                calculator = new CombatCalculator(wizard.Stats);
        }
    }
}
