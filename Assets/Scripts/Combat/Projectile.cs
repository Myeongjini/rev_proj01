using UnityEngine;

namespace WizardGrower.Combat
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float hitDistance = 0.18f;
        [SerializeField] private float lifetime = 4f;

        private IDamageable target;
        private Transform targetTransform;
        private DamageInfo damageInfo;
        private float lifeTimer;

        public void Launch(IDamageable target, DamageInfo damageInfo, float projectileSpeed)
        {
            this.target = target;
            targetTransform = target != null ? target.HitTransform : null;
            this.damageInfo = damageInfo;
            speed = projectileSpeed;
            lifeTimer = lifetime;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f || target == null || !target.IsAlive || targetTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPosition = targetTransform.position;
            if (Vector3.Distance(transform.position, targetPosition) <= hitDistance)
            {
                target.TakeDamage(damageInfo);
                Destroy(gameObject);
                return;
            }

            Vector3 direction = targetPosition - transform.position;
            if (direction.sqrMagnitude > 0.001f)
                transform.right = direction.normalized;

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) <= hitDistance)
            {
                target.TakeDamage(damageInfo);
                Destroy(gameObject);
            }
        }
    }
}
