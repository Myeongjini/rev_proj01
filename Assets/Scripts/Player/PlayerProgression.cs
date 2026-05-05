using UnityEngine;

namespace WizardGrower.Player
{
    public class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private int highestStageReached = 1;
        [SerializeField] private float bestCombatPower;

        public int HighestStageReached => highestStageReached;
        public float BestCombatPower => bestCombatPower;

        public void RecordStage(int stage)
        {
            highestStageReached = Mathf.Max(highestStageReached, stage);
        }

        public void RecordCombatPower(float combatPower)
        {
            bestCombatPower = Mathf.Max(bestCombatPower, combatPower);
        }
    }
}
