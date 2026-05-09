using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class MainUI01Bar : MonoBehaviour
    {
        public enum NavTab { Upgrade, Weapon, Summon, Skill, Reserved5 }

        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button weaponButton;
        [SerializeField] private Button summonButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button reserved4Button;
        [SerializeField] private Button reserved5Button;

        public event Action<NavTab> TabRequested;

        private void Awake()
        {
            if (skillButton == null)
                skillButton = reserved4Button;
            Bind();
            ConfigureSkill(skillButton);
            ConfigureReserved(reserved5Button);
            SetActiveTab(null);
        }

        public void Bind()
        {
            Wire(upgradeButton, NavTab.Upgrade);
            Wire(weaponButton, NavTab.Weapon);
            Wire(summonButton, NavTab.Summon);
            if (skillButton == null)
                skillButton = reserved4Button;
            ConfigureSkill(skillButton);
            Wire(skillButton, NavTab.Skill);
        }

        public void SetActiveTab(NavTab? activeTab)
        {
            SetButtonState(upgradeButton, activeTab.HasValue && activeTab.Value == NavTab.Upgrade);
            SetButtonState(weaponButton, activeTab.HasValue && activeTab.Value == NavTab.Weapon);
            SetButtonState(summonButton, activeTab.HasValue && activeTab.Value == NavTab.Summon);
            SetButtonState(skillButton != null ? skillButton : reserved4Button, activeTab.HasValue && activeTab.Value == NavTab.Skill);
        }

        private void Wire(Button button, NavTab tab)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => TabRequested?.Invoke(tab));
        }

        private static void ConfigureReserved(Button button)
        {
            if (button == null)
                return;

            button.interactable = false;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.28f, 0.28f, 0.30f, 0.92f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "준비중";
        }

        private static void ConfigureSkill(Button button)
        {
            if (button == null)
                return;

            button.interactable = true;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.10f, 0.12f, 0.16f, 0.92f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "스킬";
        }

        private static void SetButtonState(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = active ? new Color(0.32f, 0.58f, 1f, 0.96f) : new Color(0.10f, 0.12f, 0.16f, 0.92f);
        }
    }
}
