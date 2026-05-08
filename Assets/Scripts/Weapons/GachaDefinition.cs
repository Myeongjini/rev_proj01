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
        public RarityWeight[] weights;
        public int pityThreshold = 30;
        public Rarity pityFloor = Rarity.Rare;
    }
}
