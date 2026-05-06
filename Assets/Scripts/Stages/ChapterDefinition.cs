using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Chapter Definition", fileName = "Chapter")]
    public class ChapterDefinition : ScriptableObject
    {
        [Header("Identification")]
        public int chapterNumber;
        public string displayName;
        public string themeDescription;

        [Header("Stages (8개 권장)")]
        public StageDefinition[] stages = new StageDefinition[8];

        [Header("Theme Visuals")]
        public Sprite backgroundSprite;
        public Color ambientTint = Color.white;
    }
}
