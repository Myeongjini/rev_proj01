using UnityEngine;

namespace WizardGrower.Player
{
    public class PlayerWizard : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats = new PlayerStats();
        [SerializeField] private Transform castPoint;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;

        public PlayerStats Stats => stats;
        public Transform CastPoint => castPoint != null ? castPoint : transform;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        public void ConfigureCastPoint(Transform point)
        {
            castPoint = point;
        }

        public void TakeBossHit(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
        }
    }
}
