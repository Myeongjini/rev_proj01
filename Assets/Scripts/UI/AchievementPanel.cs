using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Missions;

namespace WizardGrower.UI
{
    public class AchievementPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dailyTabButton;
        [SerializeField] private Button repeatTabButton;
        [SerializeField] private Transform rowContainer;
        [SerializeField] private MissionRowView rowPrefab;

        private MissionService service;
        private bool showingDaily = true;

        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();
            Close();
        }

        public void Bind(MissionService service)
        {
            if (this.service != null)
                this.service.StateChanged -= Refresh;
            this.service = service;
            if (this.service != null)
                this.service.StateChanged += Refresh;
            Refresh();
        }

        public void Toggle()
        {
            if (gameObject.activeSelf)
                Close();
            else
                Open();
        }

        public void Open()
        {
            ResolveReferences();
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            Refresh();
            OpenStateChanged?.Invoke(true);
        }

        public void Close()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
            OpenStateChanged?.Invoke(false);
        }

        private void Refresh()
        {
            if (rowContainer == null)
                return;

            for (int i = rowContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = rowContainer.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            if (service == null)
                return;

            if (showingDaily)
            {
                for (int i = 0; i < service.DailyStates.Count; i++)
                {
                    DailyMissionState state = service.DailyStates[i];
                    if (state == null || state.claimed)
                        continue;
                    MissionDefinition definition = service.GetDefinition(state.missionId);
                    if (definition == null)
                        continue;
                    AddRow(state.missionId, definition.FormatDescription(definition.initialTargetCount), state.progress, definition.initialTargetCount, service.IsComplete(state), id => service.ClaimDailyAsync(id));
                }
            }
            else
            {
                for (int i = 0; i < service.RepeatStates.Count; i++)
                {
                    RepeatMissionState state = service.RepeatStates[i];
                    MissionDefinition definition = service.GetDefinition(state.missionId);
                    if (definition == null)
                        continue;
                    AddRow(state.missionId, definition.FormatDescription(state.currentTargetN), state.runningCounter, state.currentTargetN, service.IsComplete(state), id => service.ClaimRepeatAsync(id));
                }
            }
        }

        private void AddRow(string id, string description, int progress, int target, bool complete, Func<string, Task<bool>> claim)
        {
            if (rowPrefab == null)
                return;

            MissionRowView row = Instantiate(rowPrefab, rowContainer);
            RectTransform rect = row.transform as RectTransform;
            if (rect != null)
                rect.sizeDelta = new Vector2(620f, 74f);
            row.Bind(id, description, progress, target, complete, claim);
        }

        private void ResolveReferences()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (closeButton == null)
                closeButton = FindButton("CloseButton");
            if (dailyTabButton == null)
                dailyTabButton = FindButton("DailyTab");
            if (repeatTabButton == null)
                repeatTabButton = FindButton("RepeatTab");
            if (rowContainer == null)
                rowContainer = FindChildTransform("Rows");
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
            if (dailyTabButton != null)
            {
                dailyTabButton.onClick.RemoveAllListeners();
                dailyTabButton.onClick.AddListener(() => { showingDaily = true; Refresh(); });
            }
            if (repeatTabButton != null)
            {
                repeatTabButton.onClick.RemoveAllListeners();
                repeatTabButton.onClick.AddListener(() => { showingDaily = false; Refresh(); });
            }
        }

        private Button FindButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == objectName)
                    return buttons[i];
            }
            return null;
        }

        private Transform FindChildTransform(string objectName)
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i] != null && rects[i].name == objectName)
                    return rects[i];
            }
            return null;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (service != null)
                service.StateChanged -= Refresh;
        }
    }
}
