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
        [SerializeField] private int fieldEnemyCount = 7;
        [SerializeField] private Vector2 fieldSpawnExtents = new Vector2(5.3f, 2.8f);
        [SerializeField] private Vector2 minWanderBounds = new Vector2(-5.8f, -3.25f);
        [SerializeField] private Vector2 maxWanderBounds = new Vector2(5.8f, 3.25f);
        [SerializeField] private float minSpawnSpacing = 1.15f;
        [SerializeField] private GameObject eliteGlowPrefab;

        public event Action<EnemyBase> EnemySpawned;
        public event Action<EnemyBase, DamageInfo> EnemyDamaged;
        public event Action<EnemyBase> EnemyKilled;

        private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();

        public IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;

        public EnemyBase SpawnNormal(float health, int reward, float armor)
        {
            ClearEnemies();
            return SpawnNormalSingle(health, reward, armor, GetFieldSpawnPosition());
        }

        public EnemyBase SpawnNormalReplacement(float health, int reward, float armor)
        {
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

        public EnemyBase SpawnEliteNear(Vector3 nearPosition, float baseHealth, int baseReward, float armor)
        {
            Vector3 position = nearPosition + new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(-1.2f, 1.2f), 0f);
            position.x = Mathf.Clamp(position.x, minWanderBounds.x, maxWanderBounds.x);
            position.y = Mathf.Clamp(position.y, minWanderBounds.y, maxWanderBounds.y);
            EnemyBase enemy = Spawn(normalEnemyPrefab, Mathf.Max(1f, baseHealth) * 5f, Mathf.Max(0, baseReward) * 5, armor, position, true);
            if (enemy != null)
            {
                enemy.MarkElite();
                EliteMonsterController elite = enemy.GetComponent<EliteMonsterController>();
                if (elite == null)
                    elite = enemy.gameObject.AddComponent<EliteMonsterController>();
                elite.ApplyEliteVisuals(eliteGlowPrefab);
            }
            return enemy;
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

            if (enemy.GetComponent<EnemyHealthBarView>() == null)
                enemy.gameObject.AddComponent<EnemyHealthBarView>();

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
            Vector3 candidate = center;

            for (int attempt = 0; attempt < 24; attempt++)
            {
                candidate = center;
                candidate.x += UnityEngine.Random.Range(-fieldSpawnExtents.x, fieldSpawnExtents.x);
                candidate.y += UnityEngine.Random.Range(-fieldSpawnExtents.y, fieldSpawnExtents.y);
                candidate.x = Mathf.Clamp(candidate.x, minWanderBounds.x, maxWanderBounds.x);
                candidate.y = Mathf.Clamp(candidate.y, minWanderBounds.y, maxWanderBounds.y);

                if (HasEnoughSpacing(candidate))
                    return candidate;
            }

            return candidate;
        }

        private bool HasEnoughSpacing(Vector3 position)
        {
            float minSqr = minSpawnSpacing * minSpawnSpacing;
            foreach (EnemyBase enemy in activeEnemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                if ((enemy.transform.position - position).sqrMagnitude < minSqr)
                    return false;
            }

            return true;
        }
    }
}
