using System;

namespace WizardGrower.Missions
{
    [Serializable]
    public class RepeatMissionState
    {
        public string missionId;
        public int currentTargetN;
        public int runningCounter;

        public RepeatMissionState() { }

        public RepeatMissionState(string missionId, int target)
        {
            this.missionId = missionId;
            currentTargetN = target;
        }
    }
}
