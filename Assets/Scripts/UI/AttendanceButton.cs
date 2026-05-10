using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class AttendanceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private AttendancePanel panel;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => panel?.Toggle());
        }

        public void Bind(AttendancePanel panel)
        {
            this.panel = panel;
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => this.panel?.Toggle());
            }
        }
    }
}
