using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Save;

namespace WizardGrower.UI
{
    public enum ConflictChoice
    {
        UseLocal,
        UseRemote,
        CancelLogin
    }

    public class SaveConflictPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text localSummaryLabel;
        [SerializeField] private TMP_Text remoteSummaryLabel;
        [SerializeField] private Button useLocalButton;
        [SerializeField] private Button useRemoteButton;
        [SerializeField] private Button cancelLoginButton;

        private TaskCompletionSource<ConflictChoice> completion;

        private void Awake()
        {
            if (useLocalButton != null)
                useLocalButton.onClick.AddListener(() => Resolve(ConflictChoice.UseLocal));
            if (useRemoteButton != null)
                useRemoteButton.onClick.AddListener(() => Resolve(ConflictChoice.UseRemote));
            if (cancelLoginButton != null)
                cancelLoginButton.onClick.AddListener(() => Resolve(ConflictChoice.CancelLogin));
            gameObject.SetActive(false);
        }

        public Task<ConflictChoice> ShowAsync(SaveData local, SaveDataDocument remote)
        {
            completion = new TaskCompletionSource<ConflictChoice>();
            if (localSummaryLabel != null)
                localSummaryLabel.text = BuildLocalSummary(local);
            if (remoteSummaryLabel != null)
                remoteSummaryLabel.text = BuildRemoteSummary(remote);
            gameObject.SetActive(true);
            return completion.Task;
        }

        private void Resolve(ConflictChoice choice)
        {
            gameObject.SetActive(false);
            completion?.TrySetResult(choice);
            completion = null;
        }

        private static string BuildLocalSummary(SaveData data)
        {
            if (data == null)
                return "Local\nNo data";

            return $"Local\nGold {data.gold}\nStage {data.currentChapter}-{data.currentStage}\nUpdated {FormatTime(data.updatedAtUnixMs)}";
        }

        private static string BuildRemoteSummary(SaveDataDocument data)
        {
            if (data == null)
                return "Remote\nNo data";

            return $"Remote\nGold {data.Gold}\nStage {data.CurrentChapter}-{data.CurrentStage}\nUpdated {FormatTime(data.UpdatedAtUnixMs)}";
        }

        private static string FormatTime(long unixMs)
        {
            if (unixMs <= 0)
                return "Unknown";

            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
