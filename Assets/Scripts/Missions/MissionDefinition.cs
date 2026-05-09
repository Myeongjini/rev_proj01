using UnityEngine;

namespace WizardGrower.Missions
{
    public enum MissionType { Daily, Repeat }
    public enum MissionTracker { KillMonsters, ClearBoss, EarnGold, GachaPull, SynthesizeWeapon }
    public enum RewardKind { Gem }

    [CreateAssetMenu(menuName = "Wizard Grower/Mission Definition")]
    public class MissionDefinition : ScriptableObject
    {
        public string missionId;
        public string descriptionKo;
        public MissionType type;
        public MissionTracker tracker;
        public int initialTargetCount;
        public int repeatDelta;
        public RewardKind rewardKind;
        public int rewardAmount;

        public string FormatDescription(int target)
        {
            return string.Format(string.IsNullOrEmpty(descriptionKo) ? missionId : descriptionKo, target);
        }
    }
}
