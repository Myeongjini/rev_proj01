using System;
using UnityEngine;
using WizardGrower.Combat;

namespace WizardGrower.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private NormalEnemy normalEnemyPrefab;
        [SerializeField] private BossEnemy bossEnemyPrefab;
        [SerializeField] private Transform spawnPoint;

        public event Action<EnemyBase> EnemySpawned;
        public event Action<EnemyBase, DamageInfo> EnemyDamaged;
        public event Action<EnemyBase> EnemyKilled;

        public EnemyBase CurrentEnemy { get; private set; }

        public EnemyBase SpawnNormal(float health, int reward)
        {
            return Spawn(normalEnemyPrefab, health, reward);
        }

        public BossEnemy SpawnBoss(float health, int reward)
        {
            return Spawn(bossEnemyPrefab, health, reward) as BossEnemy;
        }

        private EnemyBase Spawn(EnemyBase prefab, float health, int reward)
        {
            if (CurrentEnemy != null)
                Destroy(CurrentEnemy.gameObject);

            CurrentEnemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity, transform);
            CurrentEnemy.Initialize(health, reward);
            CurrentEnemy.Damaged += OnEnemyDamaged;
            CurrentEnemy.Killed += OnEnemyKilled;
            EnemySpawned?.Invoke(CurrentEnemy);
            return CurrentEnemy;
        }

        private void OnEnemyDamaged(DamageInfo info)
        {
            if (CurrentEnemy != null)
                EnemyDamaged?.Invoke(CurrentEnemy, info);
        }

        private void OnEnemyKilled(IDamageable damageable)
        {
            if (CurrentEnemy == null)
                return;

            EnemyBase killed = CurrentEnemy;
            CurrentEnemy = null;
            EnemyKilled?.Invoke(killed);
        }
    }
}
