using System;
using System.Collections.Generic;

namespace WizardGrower.Armor
{
    public class ArmorFusionService
    {
        public event Action<IReadOnlyList<ArmorFusionResult>> FusionCompleted;

        public bool CanFuseAny(ArmorInventory inventory, ArmorDatabase database)
        {
            if (inventory == null || database == null)
                return false;

            IReadOnlyList<ArmorDefinition> ordered = database.OrderedArmors;
            for (int i = 0; i < ordered.Count; i++)
            {
                ArmorDefinition armor = ordered[i];
                if (armor == null || database.GetNext(armor) == null)
                    continue;
                if (inventory.GetCount(armor.armorId) >= 3)
                    return true;
            }
            return false;
        }

        public IReadOnlyList<ArmorFusionResult> SynthesizeAll(ArmorInventory inventory, ArmorDatabase database)
        {
            List<ArmorFusionResult> results = new List<ArmorFusionResult>();
            if (inventory == null || database == null)
                return results;

            IReadOnlyList<ArmorDefinition> ordered = database.OrderedArmors;
            for (int i = 0; i < ordered.Count; i++)
            {
                ArmorDefinition from = ordered[i];
                ArmorDefinition to = database.GetNext(from);
                if (from == null || to == null)
                    continue;

                int times = inventory.GetCount(from.armorId) / 3;
                if (times <= 0 || !inventory.TryConsume(from.armorId, times * 3))
                    continue;

                inventory.Add(to.armorId, times);
                results.Add(new ArmorFusionResult
                {
                    fromArmorId = from.armorId,
                    toArmorId = to.armorId,
                    times = times
                });
            }

            FusionCompleted?.Invoke(results);
            return results;
        }
    }

    [Serializable]
    public struct ArmorFusionResult
    {
        public string fromArmorId;
        public string toArmorId;
        public int times;
    }
}
