using System;
using UnityEngine;

namespace WizardGrower.Missions
{
    public class MissionResetService : MonoBehaviour
    {
        private const long KstOffsetMs = 9L * 60L * 60L * 1000L;

        public event Action<long> DailyResetTriggered;

        public long CurrentServerUtcMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public bool IsLaterKstDay(long previousUtcMs, long currentUtcMs)
        {
            if (previousUtcMs <= 0)
                return true;

            DateTime previous = DateTimeOffset.FromUnixTimeMilliseconds(previousUtcMs + KstOffsetMs).UtcDateTime.Date;
            DateTime current = DateTimeOffset.FromUnixTimeMilliseconds(currentUtcMs + KstOffsetMs).UtcDateTime.Date;
            return current > previous;
        }

        public void CheckDailyReset(long lastResetUtcMs)
        {
            long now = CurrentServerUtcMs;
            if (IsLaterKstDay(lastResetUtcMs, now))
                DailyResetTriggered?.Invoke(now);
        }
    }
}
