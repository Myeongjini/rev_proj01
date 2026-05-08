using TMPro;
using UnityEngine;
using WizardGrower.Chat;

namespace WizardGrower.UI
{
    public class ChatMessageView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        public void Bind(ChatMessage message)
        {
            if (label == null)
                label = GetComponentInChildren<TMP_Text>();

            if (label == null)
                return;

            string sender = string.IsNullOrWhiteSpace(message.DisplayName) ? "Guest" : message.DisplayName;
            label.text = string.Format("{0}: {1}", sender, message.Text);
        }
    }
}
