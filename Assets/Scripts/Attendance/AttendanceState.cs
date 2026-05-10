using System;

namespace WizardGrower.Attendance
{
    [Serializable]
    public class AttendanceState
    {
        public int currentDayIndex = 1;
        public long lastClaimedUtcMs;
        public int totalCheckIns;

        public AttendanceState Clone()
        {
            return new AttendanceState
            {
                currentDayIndex = currentDayIndex,
                lastClaimedUtcMs = lastClaimedUtcMs,
                totalCheckIns = totalCheckIns
            };
        }
    }
}
