using System;

namespace WizardGrower.Armor
{
    [Serializable]
    public class OwnedArmorEntry
    {
        public string armorId;
        public int count;
        public int enhancementLevel;

        public OwnedArmorEntry()
        {
        }

        public OwnedArmorEntry(string armorId, int count, int enhancementLevel = 0)
        {
            this.armorId = armorId;
            this.count = count;
            this.enhancementLevel = enhancementLevel;
        }
    }

    [Serializable]
    public class EquippedArmorEntry
    {
        public ArmorSlot slot;
        public string armorId;

        public EquippedArmorEntry()
        {
        }

        public EquippedArmorEntry(ArmorSlot slot, string armorId)
        {
            this.slot = slot;
            this.armorId = armorId;
        }
    }
}
