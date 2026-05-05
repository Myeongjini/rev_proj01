using UnityEngine;

namespace WizardGrower.Enemies
{
    public class EnemyScalingService
    {
        private readonly float baseHealth;
        private readonly float healthScale;
        private readonly int baseReward;

        public EnemyScalingService(float baseHealth = 50f, float healthScale = 1.25f, int baseReward = 10)
        {
            this.baseHealth = baseHealth;
            this.healthScale = healthScale;
            this.baseReward = baseReward;
        }

        public float GetNormalHealth(int stage)
        {
            return baseHealth * Mathf.Pow(healthScale, Mathf.Max(0, stage - 1));
        }

        public float GetBossHealth(int stage)
        {
            return GetNormalHealth(stage) * 8f;
        }

        public int GetReward(int stage)
        {
            return Mathf.RoundToInt(baseReward * Mathf.Pow(1.18f, Mathf.Max(0, stage - 1)));
        }
    }
}
