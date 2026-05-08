using UnityEngine;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public static class RarityVisuals
    {
        public static Color ColorFor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return new Color(0.30f, 0.90f, 0.78f, 1f);
                case Rarity.Rare: return new Color(0.35f, 0.58f, 1f, 1f);
                case Rarity.Epic: return new Color(0.72f, 0.35f, 1f, 1f);
                case Rarity.Legendary: return new Color(1f, 0.58f, 0.18f, 1f);
                case Rarity.Mythic: return new Color(1f, 0.22f, 0.34f, 1f);
                default: return new Color(0.80f, 0.82f, 0.86f, 1f);
            }
        }

        public static Color ColorFor(WeaponUpperGrade grade)
        {
            switch (grade)
            {
                case WeaponUpperGrade.Normal: return new Color(0.30f, 0.90f, 0.78f, 1f);
                case WeaponUpperGrade.Advanced: return new Color(0.35f, 0.58f, 1f, 1f);
                case WeaponUpperGrade.Epic: return new Color(0.72f, 0.35f, 1f, 1f);
                case WeaponUpperGrade.Unique: return new Color(1f, 0.58f, 0.18f, 1f);
                default: return new Color(0.80f, 0.82f, 0.86f, 1f);
            }
        }

        public static string LabelFor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return "Uncommon";
                case Rarity.Rare: return "Rare";
                case Rarity.Epic: return "Epic";
                case Rarity.Legendary: return "Legendary";
                case Rarity.Mythic: return "Mythic";
                default: return "Common";
            }
        }
    }
}
