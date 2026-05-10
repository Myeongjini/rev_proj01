using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Attendance;

namespace WizardGrower.UI
{
    public class AttendanceDayCellView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text dayLabel;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private TMP_Text markLabel;

        private Action clicked;

        private void Awake()
        {
            EnsureUi();
        }

        public void Bind(int dayIndex, AttendanceDayReward reward, AttendanceCellState state, Action clicked)
        {
            EnsureUi();
            this.clicked = clicked;
            dayLabel.text = $"Day {dayIndex}";
            rewardLabel.text = $"gem {reward.amount}";
            markLabel.text = state == AttendanceCellState.Claimed ? "✓" : state == AttendanceCellState.Locked ? "잠김" : "받기";
            button.interactable = state == AttendanceCellState.Claimable;
            background.color = state switch
            {
                AttendanceCellState.Claimed => new Color(0.10f, 0.12f, 0.16f, 0.72f),
                AttendanceCellState.Claimable => new Color(0.16f, 0.36f, 0.82f, 0.95f),
                _ => new Color(0.18f, 0.18f, 0.20f, 0.78f)
            };
        }

        private void EnsureUi()
        {
            if (background == null)
                background = GetComponent<Image>();
            if (background == null)
                background = gameObject.AddComponent<Image>();
            if (button == null)
                button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            if (dayLabel == null)
                dayLabel = CreateText("DayLabel", new Vector2(0f, 0.62f), new Vector2(1f, 1f), 16f, FontStyles.Bold);
            if (rewardLabel == null)
                rewardLabel = CreateText("RewardLabel", new Vector2(0f, 0.30f), new Vector2(1f, 0.64f), 14f, FontStyles.Normal);
            if (markLabel == null)
                markLabel = CreateText("MarkLabel", new Vector2(0f, 0f), new Vector2(1f, 0.30f), 13f, FontStyles.Normal);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked?.Invoke());
        }

        private TMP_Text CreateText(string name, Vector2 min, Vector2 max, float size, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            return text;
        }
    }
}
