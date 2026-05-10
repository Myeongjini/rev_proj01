using System;
using UnityEngine;
using WizardGrower.Missions;

namespace WizardGrower.Attendance
{
    [CreateAssetMenu(menuName = "Wizard Grower/Attendance Config")]
    public class AttendanceConfig : ScriptableObject
    {
        public AttendanceDayReward[] dayRewards;

        public AttendanceDayReward GetReward(int dayIndex)
        {
            EnsureDefaultRewards();
            int index = Mathf.Clamp(dayIndex, 1, 10) - 1;
            return dayRewards[index];
        }

        public void EnsureDefaultRewards()
        {
            if (dayRewards != null && dayRewards.Length == 10)
                return;

            dayRewards = new AttendanceDayReward[10];
            for (int i = 0; i < dayRewards.Length; i++)
            {
                dayRewards[i] = new AttendanceDayReward
                {
                    kind = RewardKind.Gem,
                    amount = 100
                };
            }
        }

        public static AttendanceConfig CreateDefault()
        {
            AttendanceConfig config = CreateInstance<AttendanceConfig>();
            config.name = "RuntimeAttendanceConfig";
            config.EnsureDefaultRewards();
            return config;
        }
    }

    [Serializable]
    public struct AttendanceDayReward
    {
        public RewardKind kind;
        public int amount;
    }
}
