using System;

namespace WizardGrower.Weapons
{
    [Serializable]
    public class SummonLevelDefinition
    {
        public int level = 1;
        public int pullsToNextLevel;
        public WeaponUpperGrade maxUpperGrade;
        public WeaponGradeWeight[] upperGradeWeights;
    }
}
