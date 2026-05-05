using TMPro;
using UnityEngine;

namespace WizardGrower.UI
{
    public class BossTimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        public void Refresh(float current, float duration)
        {
            bool active = duration > 0f && current > 0f;
            gameObject.SetActive(active);
            if (label != null)
                label.text = active ? $"Boss {current:0.0}s" : string.Empty;
        }
    }
}
