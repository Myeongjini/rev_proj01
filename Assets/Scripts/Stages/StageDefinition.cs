using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Stage Definition", fileName = "Stage")]
    public class StageDefinition : ScriptableObject
    {
        [Header("Identification")]
        public int stageNumber;
        public string displayLabel;

        [Header("Field Monster")]
        public float fieldMonsterHealth = 50f;
        public float fieldMonsterArmor = 0f;
        public int fieldMonsterReward = 10;
        public float fieldRespawnDelay = 0.5f;

        [Header("Boss")]
        public float bossHealth = 400f;
        public float bossArmor = 5f;
        public int bossReward = 100;
        public float bossTimeLimit = 20f;

        [Header("Optional Visuals")]
        public Sprite normalEnemyOverride;
        public Sprite bossEnemyOverride;
    }
}
