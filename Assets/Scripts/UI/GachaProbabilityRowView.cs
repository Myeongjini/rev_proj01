using TMPro;
using UnityEngine;

namespace WizardGrower.UI
{
    public class GachaProbabilityRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        public void Bind(string text)
        {
            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
        }
    }
}
