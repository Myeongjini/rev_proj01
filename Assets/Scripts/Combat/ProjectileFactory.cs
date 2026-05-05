using UnityEngine;

namespace WizardGrower.Combat
{
    public class ProjectileFactory : MonoBehaviour
    {
        [SerializeField] private Projectile autoProjectilePrefab;
        [SerializeField] private Projectile manualProjectilePrefab;
        [SerializeField] private Projectile skillProjectilePrefab;
        [SerializeField] private Transform projectileRoot;
        [SerializeField] private float autoSpeed = 8f;
        [SerializeField] private float manualSpeed = 10f;
        [SerializeField] private float skillSpeed = 12f;

        public void FireAuto(Vector3 origin, IDamageable target, DamageInfo info)
        {
            Fire(autoProjectilePrefab, origin, target, info, autoSpeed);
        }

        public void FireManual(Vector3 origin, IDamageable target, DamageInfo info)
        {
            Fire(manualProjectilePrefab, origin, target, info, manualSpeed);
        }

        public void FireSkill(Vector3 origin, IDamageable target, DamageInfo info)
        {
            Fire(skillProjectilePrefab, origin, target, info, skillSpeed);
        }

        private void Fire(Projectile prefab, Vector3 origin, IDamageable target, DamageInfo info, float speed)
        {
            if (prefab == null || target == null || !target.IsAlive)
                return;

            Projectile projectile = Instantiate(prefab, origin, Quaternion.identity, projectileRoot);
            projectile.Launch(target, info, speed);
        }
    }
}
