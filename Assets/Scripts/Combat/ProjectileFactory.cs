using UnityEngine;
using WizardGrower.Weapons;

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

        private WeaponInventory weaponInventory;

        public void BindWeaponInventory(WeaponInventory inventory)
        {
            weaponInventory = inventory;
        }

        public void FireAuto(Vector3 origin, IDamageable target, DamageInfo info)
        {
            Sprite spriteOverride = weaponInventory != null && weaponInventory.Equipped != null ? weaponInventory.Equipped.projectileSprite : null;
            Fire(autoProjectilePrefab, origin, target, info, autoSpeed, spriteOverride);
        }

        public void FireManual(Vector3 origin, IDamageable target, DamageInfo info)
        {
            Fire(manualProjectilePrefab, origin, target, info, manualSpeed, null);
        }

        public void FireSkill(Vector3 origin, IDamageable target, DamageInfo info)
        {
            Fire(skillProjectilePrefab, origin, target, info, skillSpeed, null);
        }

        private void Fire(Projectile prefab, Vector3 origin, IDamageable target, DamageInfo info, float speed, Sprite spriteOverride)
        {
            if (prefab == null || target == null || !target.IsAlive)
                return;

            Projectile projectile = Instantiate(prefab, origin, Quaternion.identity, projectileRoot);
            projectile.ApplySpriteOverride(spriteOverride);
            projectile.Launch(target, info, speed);
        }
    }
}
