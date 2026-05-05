using System;
using UnityEngine;
using WizardGrower.Combat;

namespace WizardGrower.Enemies
{
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float currentHealth = 50f;
        [SerializeField] private int rewardGold = 10;
        [SerializeField] private Transform hitTransform;

        public event Action<DamageInfo> Damaged;
        public event Action<IDamageable> Killed;

        public Transform HitTransform => hitTransform != null ? hitTransform : transform;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public int RewardGold => rewardGold;
        public bool IsAlive => currentHealth > 0f;

        public virtual void Initialize(float health, int reward)
        {
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            rewardGold = Mathf.Max(0, reward);
            gameObject.SetActive(true);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - info.Amount);
            Damaged?.Invoke(info);

            if (currentHealth <= 0f)
                Die();
        }

        protected virtual void Die()
        {
            Killed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
