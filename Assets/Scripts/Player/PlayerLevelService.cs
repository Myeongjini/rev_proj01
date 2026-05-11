using System;
using UnityEngine;
using WizardGrower.Enemies;

namespace WizardGrower.Player
{
    public class PlayerLevelService : MonoBehaviour
    {
        [SerializeField] private int levelCap = 50;
        [SerializeField] private int baseExpToNext = 100;
        [SerializeField] private float expCurveBase = 1.15f;
        [SerializeField] private int normalEnemyExp = 10;
        [SerializeField] private int bossEnemyExp = 50;
        [SerializeField] private int statPerLevelAttack = 5;
        [SerializeField] private int statPerLevelMaxHealth = 20;

        private PlayerStats stats;
        private EnemySpawner spawner;
        private CombatPowerService combatPower;
        private int currentLevel = 1;
        private int currentExp;

        public int CurrentLevel => currentLevel;
        public int CurrentExp => currentExp;
        public int ExpToNext => currentLevel >= LevelCap ? -1 : Mathf.Max(0, GetRequiredExpForLevel(currentLevel) - currentExp);
        public int LevelCap => Mathf.Max(1, levelCap);

        public event Action<int> LevelChanged;
        public event Action<int, int> ExpChanged;
        public event Action<int, int, int> LeveledUp;
        public event Action StateChanged;

        public void Initialize(PlayerStats stats, EnemySpawner spawner, CombatPowerService combatPower)
        {
            if (this.spawner != null)
                this.spawner.EnemyKilled -= OnEnemyKilled;

            this.stats = stats;
            this.spawner = spawner;
            this.combatPower = combatPower;

            currentLevel = Mathf.Clamp(currentLevel <= 0 ? 1 : currentLevel, 1, LevelCap);
            currentExp = currentLevel >= LevelCap ? 0 : Mathf.Max(0, Mathf.Min(currentExp, GetRequiredExpForLevel(currentLevel) - 1));

            if (this.spawner != null)
                this.spawner.EnemyKilled += OnEnemyKilled;

            LevelChanged?.Invoke(currentLevel);
            ExpChanged?.Invoke(currentExp, ExpToNext);
        }

        public void LoadState(int level, int exp)
        {
            currentLevel = Mathf.Clamp(level <= 0 ? 1 : level, 1, LevelCap);
            currentExp = currentLevel >= LevelCap ? 0 : Mathf.Max(0, Mathf.Min(exp, GetRequiredExpForLevel(currentLevel) - 1));
            LevelChanged?.Invoke(currentLevel);
            ExpChanged?.Invoke(currentExp, ExpToNext);
        }

        public void GrantExp(int amount)
        {
            if (amount <= 0 || currentLevel >= LevelCap)
                return;

            currentExp += amount;
            bool leveled = false;

            while (currentLevel < LevelCap)
            {
                int required = GetRequiredExpForLevel(currentLevel);
                if (currentExp < required)
                    break;

                currentExp -= required;
                currentLevel++;
                ApplyLevelUpStats();
                leveled = true;
            }

            if (currentLevel >= LevelCap)
                currentExp = 0;

            if (!leveled)
                ExpChanged?.Invoke(currentExp, ExpToNext);

            StateChanged?.Invoke();
        }

        public int GetRequiredExpForLevel(int level)
        {
            int safeLevel = Mathf.Clamp(level, 1, LevelCap);
            return Mathf.Max(1, (int)Math.Round(baseExpToNext * Math.Pow(expCurveBase, safeLevel - 1)));
        }

        private void ApplyLevelUpStats()
        {
            if (stats != null)
            {
                stats.AddAttackDamage(statPerLevelAttack);
                stats.AddMaxHealth(statPerLevelMaxHealth);
                stats.Heal(float.MaxValue);
            }

            combatPower?.Recalculate(true);
            LevelChanged?.Invoke(currentLevel);
            ExpChanged?.Invoke(currentExp, ExpToNext);
            LeveledUp?.Invoke(currentLevel, statPerLevelAttack, statPerLevelMaxHealth);
        }

        private void OnEnemyKilled(EnemyBase enemy)
        {
            GrantExp(enemy is BossEnemy ? bossEnemyExp : normalEnemyExp);
        }

        private void OnDestroy()
        {
            if (spawner != null)
                spawner.EnemyKilled -= OnEnemyKilled;
        }
    }
}
