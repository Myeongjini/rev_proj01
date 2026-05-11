using System;

namespace WizardGrower.Dungeons
{
    [Serializable]
    public class GoldDungeonState
    {
        public long lastEntryDateUtcMs;
        public int todayEntryCount;
        public long bestScore;
    }
}
