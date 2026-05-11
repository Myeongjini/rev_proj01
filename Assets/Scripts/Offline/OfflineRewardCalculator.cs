using System;
using UnityEngine;
using WizardGrower.Player;
using WizardGrower.Stages;

namespace WizardGrower.Offline
{
    public static class OfflineRewardCalculator
    {
        private const float OfflineEfficiency = 0.5f;

        public static long CalculateGold(OfflineWindow window, ChapterDefinition chapter, StageDefinition stage, PlayerStats stats)
        {
            if (window.elapsedSeconds <= 0 || stage == null)
                return 0;

            float rewardPerKill = Mathf.Max(0, stage.fieldMonsterReward);
            float attackInterval = stats != null ? Mathf.Max(0.05f, stats.AutoAttackInterval) : 1f;
            double killsPerSecond = (1d / attackInterval) * OfflineEfficiency;
            double gold = rewardPerKill * killsPerSecond * window.elapsedSeconds;
            if (gold <= 0d)
                return 0;
            if (gold >= long.MaxValue)
                return long.MaxValue;
            return (long)Math.Floor(gold);
        }

        public static long CalculateExp(OfflineWindow window, ChapterDefinition chapter, StageDefinition stage, PlayerStats stats)
        {
            if (window.elapsedSeconds <= 0)
                return 0;

            const float expPerKill = 10f;
            float attackInterval = stats != null ? Mathf.Max(0.05f, stats.AutoAttackInterval) : 1f;
            double killsPerSecond = (1d / attackInterval) * OfflineEfficiency;
            double exp = expPerKill * killsPerSecond * window.elapsedSeconds;
            if (exp <= 0d)
                return 0;
            if (exp >= long.MaxValue)
                return long.MaxValue;
            return (long)Math.Floor(exp);
        }
    }
}
