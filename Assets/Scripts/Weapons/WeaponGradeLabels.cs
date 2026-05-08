namespace WizardGrower.Weapons
{
    public static class WeaponGradeLabels
    {
        public static string UpperKo(WeaponUpperGrade grade)
        {
            switch (grade)
            {
                case WeaponUpperGrade.Normal: return "노멀";
                case WeaponUpperGrade.Advanced: return "고급";
                case WeaponUpperGrade.Epic: return "에픽";
                case WeaponUpperGrade.Unique: return "유니크";
                default: return "일반";
            }
        }

        public static string LowerKo(WeaponLowerGrade grade)
        {
            switch (grade)
            {
                case WeaponLowerGrade.Intermediate: return "중급";
                case WeaponLowerGrade.Upper: return "상급";
                case WeaponLowerGrade.Supreme: return "최상급";
                default: return "초급";
            }
        }

        public static string Display(WeaponUpperGrade upper, WeaponLowerGrade lower)
        {
            return $"{UpperKo(upper)} {LowerKo(lower)}";
        }
    }
}
