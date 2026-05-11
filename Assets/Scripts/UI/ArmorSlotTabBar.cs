using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Armor;

namespace WizardGrower.UI
{
    public class ArmorSlotTabBar : MonoBehaviour
    {
        [SerializeField] private Button[] buttons;
        [SerializeField] private TMP_Text[] labels;

        private ArmorSlot currentSlot = ArmorSlot.Helmet;

        public event Action<ArmorSlot> SlotChanged;

        private void Awake()
        {
            WireButtons();
            Refresh();
        }

        public void Select(ArmorSlot slot)
        {
            if (currentSlot == slot)
                return;

            currentSlot = slot;
            Refresh();
            SlotChanged?.Invoke(currentSlot);
        }

        private void WireButtons()
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                if (buttons[i] == null)
                    continue;
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => Select((ArmorSlot)Mathf.Clamp(index, 0, 4)));
            }
        }

        private void Refresh()
        {
            if (labels != null)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i] != null)
                        labels[i].text = SlotKo((ArmorSlot)Mathf.Clamp(i, 0, 4));
                }
            }

            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;
                ColorBlock colors = buttons[i].colors;
                bool selected = i == (int)currentSlot;
                colors.normalColor = selected ? new Color(0.45f, 0.6f, 1f, 1f) : Color.white;
                colors.selectedColor = colors.normalColor;
                buttons[i].colors = colors;
            }
        }

        public static string SlotKo(ArmorSlot slot)
        {
            switch (slot)
            {
                case ArmorSlot.Helmet: return "모자";
                case ArmorSlot.Chest: return "상의";
                case ArmorSlot.Legs: return "하의";
                case ArmorSlot.Gloves: return "장갑";
                case ArmorSlot.Boots: return "신발";
                default: return slot.ToString();
            }
        }
    }
}
