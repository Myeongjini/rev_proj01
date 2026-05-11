using UnityEngine;

namespace WizardGrower.Skills
{
    public enum SkillTargetingMode
    {
        NearestEnemy,
        DashToNearestEnemy,
        SelfRadius
    }

    [CreateAssetMenu(menuName = "Wizard Grower/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId;
        public string displayName;
        public Sprite icon;
        public int unlockLevel = 1;
        public float manaCost;
        public float cooldownSeconds;
        public float damageCoefficient;
        public float aoeRadius;
        public SkillTargetingMode targeting;
        public ParticleSystem castVfxPrefab;
        public ParticleSystem impactVfxPrefab;
        [TextArea] public string flavorText;
    }
}
