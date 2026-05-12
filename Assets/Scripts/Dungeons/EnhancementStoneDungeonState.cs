using System;

namespace WizardGrower.Dungeons
{
    [Serializable]
    public class EnhancementStoneDungeonState
    {
        public long lastEntryDateUtcMs;
        public int todayEntryCount;
        public long bestScore;
    }
}
