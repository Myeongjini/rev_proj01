using System;

namespace WizardGrower.Weapons
{
    [Serializable]
    public class OwnedWeaponEntry
    {
        public string weaponId;
        public int count;
        public int enhancementLevel;

        public OwnedWeaponEntry()
        {
        }

        public OwnedWeaponEntry(string weaponId, int count, int enhancementLevel = 0)
        {
            this.weaponId = weaponId;
            this.count = count;
            this.enhancementLevel = enhancementLevel;
        }
    }
}
