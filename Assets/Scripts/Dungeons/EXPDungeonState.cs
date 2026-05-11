using System;

namespace WizardGrower.Dungeons
{
    [Serializable]
    public class EXPDungeonState
    {
        public long lastEntryDateUtcMs;
        public int todayEntryCount;
        public long bestScore;
    }
}
