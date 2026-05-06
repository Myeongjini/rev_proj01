using System;
using UnityEngine;
using WizardGrower.Combat;

namespace WizardGrower.Enemies
{
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float currentHealth = 50f;
        [SerializeField] private float armor = 0f;
        [SerializeField] private int rewardGold = 10;
        [SerializeField] private Transform hitTransform;

        public event Action<DamageInfo> Damaged;
        public event Action<IDamageable> Killed;

        public Transform HitTransform => hitTransform != null ? hitTransform : transform;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float Armor => armor;
        public int RewardGold => rewardGold;
        public bool IsAlive => currentHealth > 0f;

        public virtual void Initialize(float health, int reward, float armor = 0f)
        {
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            this.armor = Mathf.Max(0f, armor);
            rewardGold = Mathf.Max(0, reward);
            gameObject.SetActive(true);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive)
                return;

            float effectiveArmor = Mathf.Max(0f, armor - info.ArmorPenetration);
            float dealt = Mathf.Max(1f, info.Amount - effectiveArmor);
            currentHealth = Mathf.Max(0f, currentHealth - dealt);
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
