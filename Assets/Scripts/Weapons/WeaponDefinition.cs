using UnityEngine;

namespace WizardGrower.Weapons
{
    [CreateAssetMenu(menuName = "Wizard Grower/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string weaponId;
        public string displayName;
        public WeaponUpperGrade upperGrade;
        public WeaponLowerGrade lowerGrade;
        public int ladderIndex;
        public Rarity rarity;
        public Sprite icon;
        public Color tintColor = Color.white;
        public Sprite accessoryGlyph;
        public Sprite projectileSprite;
        public WeaponStats statBonuses;
        [TextArea] public string flavorText;
    }
}
