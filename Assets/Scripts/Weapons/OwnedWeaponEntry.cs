using System;

namespace WizardGrower.Weapons
{
    [Serializable]
    public class OwnedWeaponEntry
    {
        public string weaponId;
        public int count;

        public OwnedWeaponEntry()
        {
        }

        public OwnedWeaponEntry(string weaponId, int count)
        {
            this.weaponId = weaponId;
            this.count = count;
        }
    }
}
