using System;

namespace WizardGrower.Stages
{
    [Serializable]
    public class StageDefinition
    {
        public int killsPerStage = 3;
        public int bossInterval = 5;
        public float bossTimeLimit = 20f;
    }
}
