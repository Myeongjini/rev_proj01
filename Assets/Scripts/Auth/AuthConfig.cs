using UnityEngine;

namespace WizardGrower.Auth
{
    [CreateAssetMenu(menuName = "Wizard Grower/Auth Config", fileName = "AuthConfig")]
    public class AuthConfig : ScriptableObject
    {
        public string bundleId;
        public string googleWebClientId;
    }
}
