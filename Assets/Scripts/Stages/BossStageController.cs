using System;
using UnityEngine;

namespace WizardGrower.Stages
{
    public class BossStageController : MonoBehaviour
    {
        private float timeRemaining;
        private bool running;

        public event Action<float, float> TimerChanged;
        public event Action Failed;

        public bool IsRunning => running;
        public float TimeRemaining => timeRemaining;

        public void StartTimer(float duration)
        {
            timeRemaining = duration;
            running = true;
            TimerChanged?.Invoke(timeRemaining, duration);
        }

        public void StopTimer()
        {
            running = false;
            TimerChanged?.Invoke(0f, 0f);
        }

        private void Update()
        {
            if (!running)
                return;

            timeRemaining -= Time.deltaTime;
            TimerChanged?.Invoke(Mathf.Max(0f, timeRemaining), timeRemaining);

            if (timeRemaining <= 0f)
            {
                running = false;
                Failed?.Invoke();
            }
        }
    }
}
