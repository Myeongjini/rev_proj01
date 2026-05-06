using UnityEngine;

namespace WizardGrower.Player
{
    public class PlayerWizard : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats = new PlayerStats();
        [SerializeField] private Transform castPoint;

        public PlayerStats Stats => stats;
        public Transform CastPoint => castPoint != null ? castPoint : transform;

        public void ConfigureCastPoint(Transform point)
        {
            castPoint = point;
        }

        public void TakeBossHit(int amount)
        {
            stats.TakeHealth(amount);
        }
    }
}
