using System;

namespace WizardGrower.Missions
{
    [Serializable]
    public class DailyMissionState
    {
        public string missionId;
        public int progress;
        public bool claimed;
        public long lastResetUtcMs;

        public DailyMissionState() { }

        public DailyMissionState(string missionId, long resetUtcMs)
        {
            this.missionId = missionId;
            lastResetUtcMs = resetUtcMs;
        }
    }
}
