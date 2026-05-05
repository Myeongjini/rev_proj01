using System;
using UnityEngine;

namespace WizardGrower.Combat
{
    public interface IDamageable
    {
        event Action<DamageInfo> Damaged;
        event Action<IDamageable> Killed;
        Transform HitTransform { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }
}
