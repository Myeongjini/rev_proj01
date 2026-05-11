using UnityEngine;
using WizardGrower.Weapons;

namespace WizardGrower.Armor
{
    [CreateAssetMenu(menuName = "Wizard Grower/Armor Definition")]
    public class ArmorDefinition : ScriptableObject
    {
        public string armorId;
        public string displayName;
        public ArmorSlot slot;
        public WeaponUpperGrade upperGrade;
        public WeaponLowerGrade lowerGrade;
        public int ladderIndex;
        public Sprite icon;
        public Color tintColor = Color.white;
        public ArmorStats statBonuses;
        [TextArea] public string flavorText;
    }
}
