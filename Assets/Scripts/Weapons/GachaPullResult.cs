using System;
using System.Collections.Generic;

namespace WizardGrower.Weapons
{
    public readonly struct GachaPullResult
    {
        public GachaPullResult(bool success, IReadOnlyList<WeaponDefinition> pulledList, string failureMessage)
        {
            Success = success;
            PulledList = pulledList ?? Array.Empty<WeaponDefinition>();
            FailureMessage = failureMessage ?? string.Empty;
        }

        public bool Success { get; }
        public IReadOnlyList<WeaponDefinition> PulledList { get; }
        public string FailureMessage { get; }
        public WeaponDefinition First => PulledList.Count > 0 ? PulledList[0] : null;

        public static GachaPullResult Ok(IReadOnlyList<WeaponDefinition> pulledList)
        {
            return new GachaPullResult(true, pulledList, string.Empty);
        }

        public static GachaPullResult Fail(string message)
        {
            return new GachaPullResult(false, Array.Empty<WeaponDefinition>(), message);
        }
    }
}
