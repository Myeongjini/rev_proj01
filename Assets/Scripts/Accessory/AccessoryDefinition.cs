using UnityEngine;
using WizardGrower.Weapons;

namespace WizardGrower.Accessory
{
    [CreateAssetMenu(menuName = "Wizard Grower/Accessory Definition")]
    public class AccessoryDefinition : ScriptableObject
    {
        public string accessoryId;
        public string displayName;
        public AccessorySlot slot;
        public WeaponUpperGrade upperGrade;
        public WeaponLowerGrade lowerGrade;
        public int ladderIndex;
        public Sprite icon;
        public Color tintColor = Color.white;
        public AccessoryStats statBonuses;
        [TextArea] public string flavorText;
    }
}
