using UnityEngine;

namespace WizardGrower.Data
{
    [CreateAssetMenu(menuName = "Wizard Grower/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId = "arcane_burst";
        public string displayName = "Arcane Burst";
        public float damageMultiplier = 8f;
        public float manaCost = 40f;
        public float cooldown = 8f;
    }
}
