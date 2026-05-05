using System;
using UnityEngine;

namespace WizardGrower.Utilities
{
    [Serializable]
    public class Timer
    {
        [SerializeField] private float duration;
        private float remaining;

        public float Remaining => remaining;
        public bool IsRunning => remaining > 0f;

        public Timer(float duration)
        {
            this.duration = duration;
        }

        public void Restart()
        {
            remaining = duration;
        }

        public bool Tick(float deltaTime)
        {
            if (remaining <= 0f)
                return false;

            remaining = Mathf.Max(0f, remaining - deltaTime);
            return remaining <= 0f;
        }
    }
}
