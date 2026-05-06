using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Chapter Database", fileName = "ChapterDatabase")]
    public class ChapterDatabase : ScriptableObject
    {
        public ChapterDefinition[] chapters;

        public ChapterDefinition GetChapter(int chapterNumber)
        {
            foreach (ChapterDefinition chapter in chapters)
            {
                if (chapter != null && chapter.chapterNumber == chapterNumber)
                    return chapter;
            }

            return null;
        }
    }
}
