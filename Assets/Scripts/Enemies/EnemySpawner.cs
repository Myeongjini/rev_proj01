using System;
using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Combat;

namespace WizardGrower.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private NormalEnemy normalEnemyPrefab;
        [SerializeField] private BossEnemy bossEnemyPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private int fieldEnemyCount = 5;
        [SerializeField] private Vector2 fieldSpawnExtents = new Vector2(2.2f, 2.4f);
        [SerializeField] private Vector2 minWanderBounds = new Vector2(-2.35f, -3.45f);
        [SerializeField] private Vector2 maxWanderBounds = new Vector2(2.35f, 3.15f);

        public event Action<EnemyBase> EnemySpawned;
        public event Action<EnemyBase, DamageInfo> EnemyDamaged;
        public event Action<EnemyBase> EnemyKilled;

        private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();

        public EnemyBase CurrentEnemy
        {
            get
            {
                for (int i = activeEnemies.Count - 1; i >= 0; i--)
                {
                    EnemyBase enemy = activeEnemies[i];
                    if (enemy == null || !enemy.IsAlive)
                    {
                        activeEnemies.RemoveAt(i);
                        continue;
                    }

                    return enemy;
                }

                return null;
            }
        }
        public IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;

        public EnemyBase SpawnNormal(float health, int reward, float armor)
        {
            ClearEnemies();
            return SpawnNormalSingle(health, reward, armor, GetFieldSpawnPosition());
        }

        public void SpawnNormalGroup(float health, int reward, float armor)
        {
            ClearEnemies();
            int count = Mathf.Max(1, fieldEnemyCount);
            for (int i = 0; i < count; i++)
                SpawnNormalSingle(health, reward, armor, GetFieldSpawnPosition());
        }

        public BossEnemy SpawnBoss(float health, int reward, float armor)
        {
            ClearEnemies();
            return Spawn(bossEnemyPrefab, health, reward, armor, spawnPoint.position, false) as BossEnemy;
        }

        public void ClearEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                    Destroy(activeEnemies[i].gameObject);
            }

            activeEnemies.Clear();
        }

        public EnemyBase GetNearestEnemy(Vector3 position)
        {
            EnemyBase nearest = null;
            float nearestSqr = float.MaxValue;

            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = activeEnemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    activeEnemies.RemoveAt(i);
                    continue;
                }

                float sqr = (enemy.HitTransform.position - position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearest = enemy;
                    nearestSqr = sqr;
                }
            }

            return nearest;
        }

        private EnemyBase SpawnNormalSingle(float health, int reward, float armor, Vector3 position)
        {
            EnemyBase enemy = Spawn(normalEnemyPrefab, health, reward, armor, position, true);
            return enemy;
        }

        private EnemyBase Spawn(EnemyBase prefab, float health, int reward, float armor, Vector3 position, bool canWander)
        {
            EnemyBase enemy = Instantiate(prefab, position, Quaternion.identity, transform);
            enemy.Initialize(health, reward, armor);
            enemy.Damaged += info => OnEnemyDamaged(enemy, info);
            enemy.Killed += damageable => OnEnemyKilled(enemy);
            activeEnemies.Add(enemy);

            if (canWander)
            {
                EnemyWanderController wander = enemy.GetComponent<EnemyWanderController>();
                if (wander == null)
                    wander = enemy.gameObject.AddComponent<EnemyWanderController>();
                wander.SetBounds(minWanderBounds, maxWanderBounds);
            }

            EnemySpawned?.Invoke(enemy);
            return enemy;
        }

        private void OnEnemyDamaged(EnemyBase enemy, DamageInfo info)
        {
            if (enemy != null)
                EnemyDamaged?.Invoke(enemy, info);
        }

        private void OnEnemyKilled(EnemyBase enemy)
        {
            if (enemy == null)
                return;

            activeEnemies.Remove(enemy);
            EnemyKilled?.Invoke(enemy);
        }

        private Vector3 GetFieldSpawnPosition()
        {
            Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;
            center.x += UnityEngine.Random.Range(-fieldSpawnExtents.x, fieldSpawnExtents.x);
            center.y += UnityEngine.Random.Range(-fieldSpawnExtents.y, fieldSpawnExtents.y);
            center.x = Mathf.Clamp(center.x, minWanderBounds.x, maxWanderBounds.x);
            center.y = Mathf.Clamp(center.y, minWanderBounds.y, maxWanderBounds.y);
            return center;
        }
    }
}
