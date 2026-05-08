using System;
using System.Collections.Generic;

namespace WizardGrower.Weapons
{
    public class WeaponFusionService
    {
        public event Action<IReadOnlyList<WeaponFusionResult>> FusionCompleted;

        public bool CanFuseAny(WeaponInventory inventory, WeaponDatabase database)
        {
            if (inventory == null || database == null)
                return false;

            IReadOnlyList<WeaponDefinition> ordered = database.OrderedWeapons;
            for (int i = 0; i < ordered.Count; i++)
            {
                WeaponDefinition weapon = ordered[i];
                if (weapon == null || database.GetNext(weapon) == null)
                    continue;
                if (inventory.GetCount(weapon.weaponId) >= 3)
                    return true;
            }
            return false;
        }

        public IReadOnlyList<WeaponFusionResult> SynthesizeAll(WeaponInventory inventory, WeaponDatabase database)
        {
            List<WeaponFusionResult> results = new List<WeaponFusionResult>();
            if (inventory == null || database == null)
                return results;

            IReadOnlyList<WeaponDefinition> ordered = database.OrderedWeapons;
            for (int i = 0; i < ordered.Count; i++)
            {
                WeaponDefinition from = ordered[i];
                WeaponDefinition to = database.GetNext(from);
                if (from == null || to == null)
                    continue;

                int times = inventory.GetCount(from.weaponId) / 3;
                if (times <= 0)
                    continue;

                if (!inventory.TryConsume(from.weaponId, times * 3, true))
                    continue;

                inventory.Add(to.weaponId, times);
                results.Add(new WeaponFusionResult
                {
                    fromWeaponId = from.weaponId,
                    toWeaponId = to.weaponId,
                    times = times
                });
            }

            inventory.EnsureEquippedValid();

            FusionCompleted?.Invoke(results);
            return results;
        }
    }

    [Serializable]
    public struct WeaponFusionResult
    {
        public string fromWeaponId;
        public string toWeaponId;
        public int times;
    }
}
