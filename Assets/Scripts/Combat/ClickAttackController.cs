using UnityEngine;
using UnityEngine.EventSystems;
using WizardGrower.Enemies;
using WizardGrower.Player;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WizardGrower.Combat
{
    public class ClickAttackController : MonoBehaviour
    {
        [SerializeField] private PlayerWizard wizard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ProjectileFactory projectileFactory;
        [SerializeField] private PlayerMana mana;
        [SerializeField] private float manualManaCost = 5f;

        private CombatCalculator calculator;

        public void Initialize(PlayerWizard wizard, EnemySpawner enemySpawner, ProjectileFactory projectileFactory, PlayerMana mana, CombatCalculator calculator)
        {
            this.wizard = wizard;
            this.enemySpawner = enemySpawner;
            this.projectileFactory = projectileFactory;
            this.mana = mana;
            this.calculator = calculator;
        }

        public bool TryFireManual()
        {
            if (mana != null && !mana.TrySpend(manualManaCost))
                return false;

            IDamageable target = enemySpawner != null ? enemySpawner.CurrentEnemy : null;
            if (target == null || !target.IsAlive)
                return false;

            TryRepairCalculator();
            if (wizard == null || projectileFactory == null || calculator == null)
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
