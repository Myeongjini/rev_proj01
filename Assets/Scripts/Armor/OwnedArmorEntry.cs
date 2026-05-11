using System;

namespace WizardGrower.Armor
{
    [Serializable]
    public class OwnedArmorEntry
    {
        public string armorId;
        public int count;

        public OwnedArmorEntry()
        {
        }

        public OwnedArmorEntry(string armorId, int count)
        {
            this.armorId = armorId;
            this.count = count;
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
