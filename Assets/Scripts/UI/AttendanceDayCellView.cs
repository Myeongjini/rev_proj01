using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Attendance;
using WizardGrower.UI.Common;

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
        private Func<Task<bool>> asyncClicked;
        private bool pending;

        private void Awake()
        {
            ResolveReferences();
            BindButton();
        }

        public void Bind(int dayIndex, AttendanceDayReward reward, AttendanceCellState state, Action clicked)
        {
            ResolveReferences();
            this.clicked = clicked;
            asyncClicked = null;
            if (dayLabel != null)
                dayLabel.text = $"Day {dayIndex}";
            if (rewardLabel != null)
                rewardLabel.text = $"gem {reward.amount}";
            if (markLabel != null)
                markLabel.text = state == AttendanceCellState.Claimed ? "✓" : state == AttendanceCellState.Locked ? "잠김" : "받기";
            if (button != null)
                button.interactable = state == AttendanceCellState.Claimable;
            if (background != null)
                background.color = state switch
                {
                    AttendanceCellState.Claimed => new Color(0.10f, 0.12f, 0.16f, 0.72f),
                    AttendanceCellState.Claimable => new Color(0.16f, 0.36f, 0.82f, 0.95f),
                    _ => new Color(0.18f, 0.18f, 0.20f, 0.78f)
                };
            BindButton();
        }

        public void Bind(int dayIndex, AttendanceDayReward reward, AttendanceCellState state, Func<Task<bool>> clicked)
        {
            Bind(dayIndex, reward, state, (Action)null);
            asyncClicked = clicked;
        }

        private void BindButton()
        {
            if (button == null)
                return;

            button.targetGraphic = background;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }

        private async void OnClicked()
        {
            if (pending)
                return;

            pending = true;
            if (button != null)
                button.interactable = false;
            if (markLabel != null)
                markLabel.text = "처리 중";

            bool success = true;
            if (asyncClicked != null)
                success = await asyncClicked();
            else
                clicked?.Invoke();

            pending = false;
            if (!success && button != null)
                button.interactable = true;
            if (!success && markLabel != null)
                markLabel.text = "실패";
            if (!success)
                ServerStatusToast.Show(ServerStatusToast.RewardFailed);
        }

        private void ResolveReferences()
        {
            if (background == null)
                background = GetComponent<Image>();
            if (button == null)
                button = GetComponent<Button>();

            TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null)
                    continue;
                if (dayLabel == null && labels[i].name == "DayLabel")
                    dayLabel = labels[i];
                else if (rewardLabel == null && labels[i].name == "RewardLabel")
                    rewardLabel = labels[i];
                else if (markLabel == null && labels[i].name == "MarkLabel")
                    markLabel = labels[i];
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
