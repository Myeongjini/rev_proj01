using UnityEngine;

namespace WizardGrower.Economy
{
    public class RewardService
    {
        public int NormalReward(int stage, int baseReward)
        {
            return Mathf.RoundToInt(baseReward * Mathf.Pow(1.18f, Mathf.Max(0, stage - 1)));
        }

        public int BossReward(int normalReward)
        {
            return normalReward * 10;
        }
    }
}
