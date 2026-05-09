using System.Collections.Generic;
using UnityEngine;

namespace WizardGrower.Missions
{
    [CreateAssetMenu(menuName = "Wizard Grower/Mission Database")]
    public class MissionDatabase : ScriptableObject
    {
        public MissionDefinition[] missions;

        public IReadOnlyList<MissionDefinition> OrderedMissions => missions ?? System.Array.Empty<MissionDefinition>();

        public MissionDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id) || missions == null)
                return null;

            for (int i = 0; i < missions.Length; i++)
            {
                MissionDefinition mission = missions[i];
                if (mission != null && mission.missionId == id)
                    return mission;
            }
            return null;
        }
    }
}
