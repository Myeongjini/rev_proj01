using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class ManaBarView : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;

        public void Refresh(float current, float max)
        {
            if (slider != null)
                slider.value = max <= 0f ? 0f : current / max;
            if (label != null)
                label.text = $"Mana {Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
        }
    }
}
