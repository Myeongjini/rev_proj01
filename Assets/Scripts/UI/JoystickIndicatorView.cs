using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class JoystickIndicatorView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform knob;
        [SerializeField] private float maxKnobDistance = 56f;

        public void Refresh(bool active, Vector2 start, Vector2 current)
        {
            if (root == null)
                root = transform as RectTransform;

            root.gameObject.SetActive(active);
            if (!active)
                return;

            root.position = start;
            if (knob != null)
                knob.anchoredPosition = Vector2.ClampMagnitude(current - start, maxKnobDistance);
        }
    }
}
