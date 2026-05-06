using System;
using UnityEngine;
using WizardGrower.Enemies;
using WizardGrower.Player;

namespace WizardGrower.Combat
{
    public class ActiveSkillController : MonoBehaviour
    {
        [SerializeField] private PlayerWizard wizard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ProjectileFactory projectileFactory;
        [SerializeField] private PlayerMana mana;
        [SerializeField] private float manaCost = 40f;
        [SerializeField] private float cooldown = 8f;
        [SerializeField] private float damageMultiplier = 8f;

        private CombatCalculator calculator;
        private float cooldownRemaining;

        public event Action<float, float> CooldownChanged;
        public float CooldownRemaining => cooldownRemaining;
        public float Cooldown => cooldown;

        public void Initialize(PlayerWizard wizard, EnemySpawner enemySpawner, ProjectileFactory projectileFactory, PlayerMana mana, CombatCalculator calculator)
        {
            this.wizard = wizard;
            this.enemySpawner = enemySpawner;
            this.projectileFactory = projectileFactory;
            this.mana = mana;
            this.calculator = calculator;
        }

        private void Update()
        {
            if (cooldownRemaining <= 0f)
                return;

            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            CooldownChanged?.Invoke(cooldownRemaining, cooldown);
        }

        public bool TryCast()
        {
            TryRepairCalculator();
            if (cooldownRemaining > 0f || mana == null)
                return false;

            if (wizard == null || projectileFactory == null || calculator == null)
                return false;

            IDamageable target = enemySpawner != null ? enemySpawner.GetNearestEnemy(wizard.transform.position) : null;
            if (target == null || !target.IsAlive)
                return false;

            if (!mana.TrySpend(manaCost))
                return false;

            cooldownRemaining = cooldown;
            projectileFactory.FireSkill(wizard.CastPoint.position, target, calculator.Skill(wizard.gameObject, damageMultiplier));
            CooldownChanged?.Invoke(cooldownRemaining, cooldown);
            return true;
        }

        private void TryRepairCalculator()
        {
            if (calculator == null && wizard != null)
                calculator = new CombatCalculator(wizard.Stats);
        }
    }
}
