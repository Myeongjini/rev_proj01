using System;
using System.Collections.Generic;

namespace WizardGrower.Accessory
{
    public class AccessoryFusionService
    {
        public event Action<IReadOnlyList<AccessoryFusionResult>> FusionCompleted;

        public bool CanFuseAny(AccessoryInventory inventory, AccessoryDatabase database)
        {
            if (inventory == null || database == null)
                return false;

            IReadOnlyList<AccessoryDefinition> ordered = database.OrderedAccessories;
            for (int i = 0; i < ordered.Count; i++)
            {
                AccessoryDefinition accessory = ordered[i];
                if (accessory == null || database.GetNext(accessory) == null)
                    continue;
                if (inventory.GetCount(accessory.accessoryId) >= 3)
                    return true;
            }
            return false;
        }

        public IReadOnlyList<AccessoryFusionResult> SynthesizeAll(AccessoryInventory inventory, AccessoryDatabase database)
        {
            List<AccessoryFusionResult> results = new List<AccessoryFusionResult>();
            if (inventory == null || database == null)
                return results;

            IReadOnlyList<AccessoryDefinition> ordered = database.OrderedAccessories;
            for (int i = 0; i < ordered.Count; i++)
            {
                AccessoryDefinition from = ordered[i];
                AccessoryDefinition to = database.GetNext(from);
                if (from == null || to == null)
                    continue;

                int times = inventory.GetCount(from.accessoryId) / 3;
                if (times <= 0 || !inventory.TryConsume(from.accessoryId, times * 3))
                    continue;

                inventory.Add(to.accessoryId, times);
                results.Add(new AccessoryFusionResult
                {
                    fromAccessoryId = from.accessoryId,
                    toAccessoryId = to.accessoryId,
                    times = times
                });
            }

            FusionCompleted?.Invoke(results);
            return results;
        }
    }

    [Serializable]
    public struct AccessoryFusionResult
    {
        public string fromAccessoryId;
        public string toAccessoryId;
        public int times;
    }
}
