using System;

namespace WizardGrower.Accessory
{
    [Serializable]
    public class OwnedAccessoryEntry
    {
        public string accessoryId;
        public int count;
        public int enhancementLevel;

        public OwnedAccessoryEntry()
        {
        }

        public OwnedAccessoryEntry(string accessoryId, int count, int enhancementLevel = 0)
        {
            this.accessoryId = accessoryId;
            this.count = count;
            this.enhancementLevel = enhancementLevel;
        }
    }

    [Serializable]
    public class EquippedAccessoryEntry
    {
        public AccessorySlot slot;
        public string accessoryId;

        public EquippedAccessoryEntry()
        {
        }

        public EquippedAccessoryEntry(AccessorySlot slot, string accessoryId)
        {
            this.slot = slot;
            this.accessoryId = accessoryId;
        }
    }
}
