using UnityEngine;

namespace WizardGrower.Utilities
{
    public static class MathUtil
    {
        public static float SafeRatio(float current, float max)
        {
            return max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }
    }
}
