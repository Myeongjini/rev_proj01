using TMPro;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Missions;

namespace WizardGrower.UI
{
    public class MissionRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Slider slider;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimLabel;
        [SerializeField] private Image background;

        private string missionId;
        private System.Action<string> claimClicked;
        private System.Func<string, Task<bool>> asyncClaimClicked;
        private bool pending;

        private void Awake()
        {
            ResolveReferences();
            BindButton();
        }

        public void Bind(string missionId, string description, int progress, int target, bool complete, System.Action<string> claimClicked)
        {
            ResolveReferences();
            this.missionId = missionId;
            this.claimClicked = claimClicked;
            asyncClaimClicked = null;
            if (label != null)
                label.text = $"{description} [{progress}/{target}]";
            if (slider != null)
            {
                slider.minValue = 0;
                slider.maxValue = Mathf.Max(1, target);
                slider.value = Mathf.Clamp(progress, 0, Mathf.Max(1, target));
            }
            if (claimButton != null)
            {
                claimButton.interactable = complete;
                if (claimButton.image != null)
                    claimButton.image.color = complete ? new Color(0.18f, 0.38f, 0.85f, 1f) : new Color(0.28f, 0.28f, 0.30f, 0.85f);
            }
            if (claimLabel != null)
                claimLabel.text = "보상";
            if (background != null)
                background.color = new Color(0.08f, 0.09f, 0.13f, 0.95f);
            BindButton();
        }

        public void Bind(string missionId, string description, int progress, int target, bool complete, System.Func<string, Task<bool>> claimClicked)
        {
            Bind(missionId, description, progress, target, complete, (System.Action<string>)null);
            asyncClaimClicked = claimClicked;
        }

        private void BindButton()
        {
            if (claimButton == null)
                return;

            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }

        private async void OnClaimClicked()
        {
            if (pending)
                return;

            pending = true;
            if (claimButton != null)
                claimButton.interactable = false;
            if (claimLabel != null)
                claimLabel.text = "처리 중";

            bool success = true;
            if (asyncClaimClicked != null)
                success = await asyncClaimClicked(missionId);
            else
                claimClicked?.Invoke(missionId);

            pending = false;
            if (!success && claimButton != null)
                claimButton.interactable = true;
            if (!success && claimLabel != null)
                claimLabel.text = "실패";
        }

        private void ResolveReferences()
        {
            if (background == null)
                background = GetComponent<Image>();
            if (slider == null)
                slider = GetComponentInChildren<Slider>(true);
            if (claimButton == null)
                claimButton = GetComponentInChildren<Button>(true);
            TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null)
                    continue;
                if (label == null && labels[i].name == "Label" && labels[i].transform.parent == transform)
                    label = labels[i];
                else if (claimLabel == null && labels[i].transform.parent == (claimButton != null ? claimButton.transform : null))
                    claimLabel = labels[i];
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
