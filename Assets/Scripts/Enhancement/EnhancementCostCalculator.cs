using System;
using UnityEngine;

namespace WizardGrower.Enhancement
{
    public static class EnhancementCostCalculator
    {
        public const int MaxLevel = 10;
        private const int BaseCost = 100;
        private const float Growth = 1.5f;
        private const float BonusPerLevel = 0.1f;

        public static int GetCost(int currentLevel)
        {
            int level = Mathf.Clamp(currentLevel, 0, MaxLevel);
            if (level >= MaxLevel)
                return 0;
            return Mathf.Max(1, Mathf.RoundToInt(BaseCost * Mathf.Pow(Growth, level)));
        }

        public static int ClampLevel(int level)
        {
            return Mathf.Clamp(level, 0, MaxLevel);
        }

        public static float GetStatMultiplier(int level)
        {
            return 1f + BonusPerLevel * ClampLevel(level);
        }

        public static bool CanEnhance(int currentLevel)
        {
            return currentLevel >= 0 && currentLevel < MaxLevel;
        }
    }
}
