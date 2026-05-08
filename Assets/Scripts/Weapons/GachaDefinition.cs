using UnityEngine;

namespace WizardGrower.Weapons
{
    [CreateAssetMenu(menuName = "Wizard Grower/Gacha Definition")]
    public class GachaDefinition : ScriptableObject
    {
        public string gachaId = "standard";
        public string displayName = "기본 뽑기";
        public int costSingle = 30;
        public int costTen = 270;
        public WeaponDatabase pool;
        public SummonLevelDefinition[] summonLevels;
        public int pityThreshold = 30;
        public WeaponUpperGrade pityFloor = WeaponUpperGrade.Normal;

        public SummonLevelDefinition GetLevelDefinition(int level)
        {
            if (summonLevels == null || summonLevels.Length == 0)
                return null;

            SummonLevelDefinition bestBelow = null;
            SummonLevelDefinition lowest = null;
            for (int i = 0; i < summonLevels.Length; i++)
            {
                SummonLevelDefinition entry = summonLevels[i];
                if (entry == null)
                    continue;
                if (entry.level == level)
                    return entry;
                if (lowest == null || entry.level < lowest.level)
                    lowest = entry;
                if (entry.level < level && (bestBelow == null || entry.level > bestBelow.level))
                    bestBelow = entry;
            }

            return bestBelow ?? lowest;
        }

        public SummonLevelDefinition GetNextLevelDefinition(int level)
        {
            if (summonLevels == null || summonLevels.Length == 0)
                return null;

            SummonLevelDefinition next = null;
            for (int i = 0; i < summonLevels.Length; i++)
            {
                SummonLevelDefinition entry = summonLevels[i];
                if (entry == null || entry.level <= level)
                    continue;
                if (next == null || entry.level < next.level)
                    next = entry;
            }

            return next;
        }
    }
}
