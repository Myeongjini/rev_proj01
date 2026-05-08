using System.Collections.Generic;
using UnityEngine;

namespace WizardGrower.Skills
{
    [CreateAssetMenu(menuName = "Wizard Grower/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        public SkillDefinition[] skills;

        public IReadOnlyList<SkillDefinition> OrderedSkills => skills ?? System.Array.Empty<SkillDefinition>();

        public SkillDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id) || skills == null)
                return null;

            for (int i = 0; i < skills.Length; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill != null && skill.skillId == id)
                    return skill;
            }

            return null;
        }
    }
}
