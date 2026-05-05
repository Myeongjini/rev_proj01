using System;
using UnityEngine;

namespace WizardGrower.Enemies
{
    public class BossEnemy : EnemyBase
    {
        [SerializeField] private float attackInterval = 4f;
        [SerializeField] private int attackDamage = 8;

        private float timer;

        public event Action<int> BossAttacked;

        private void Update()
        {
            if (!IsAlive)
                return;

            timer += Time.deltaTime;
            if (timer < attackInterval)
                return;

            timer = 0f;
            BossAttacked?.Invoke(attackDamage);
        }
    }
}
