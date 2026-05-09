using System;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class AchievementSubTabBar : MonoBehaviour
    {
        [SerializeField] private Button dailyButton;
        [SerializeField] private Button repeatButton;

        public event Action<bool> TabChanged;

        public void Bind()
        {
            if (dailyButton != null)
            {
                dailyButton.onClick.RemoveAllListeners();
                dailyButton.onClick.AddListener(() => TabChanged?.Invoke(true));
            }
            if (repeatButton != null)
            {
                repeatButton.onClick.RemoveAllListeners();
                repeatButton.onClick.AddListener(() => TabChanged?.Invoke(false));
            }
        }
    }
}
