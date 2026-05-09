using System;
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
            EnsureUi();
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
            EnsureUi();
            gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
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
                    AddRow(state.missionId, definition.FormatDescription(definition.initialTargetCount), state.progress, definition.initialTargetCount, service.IsComplete(state), id => service.ClaimDaily(id));
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
                    AddRow(state.missionId, definition.FormatDescription(state.currentTargetN), state.runningCounter, state.currentTargetN, service.IsComplete(state), id => service.ClaimRepeat(id));
                }
            }
        }

        private void AddRow(string id, string description, int progress, int target, bool complete, Action<string> claim)
        {
            MissionRowView row = rowPrefab != null
                ? Instantiate(rowPrefab, rowContainer)
                : new GameObject("MissionRow", typeof(RectTransform), typeof(Image), typeof(MissionRowView)).GetComponent<MissionRowView>();
            if (row.transform.parent != rowContainer)
                row.transform.SetParent(rowContainer, false);
            RectTransform rect = row.transform as RectTransform;
            if (rect != null)
                rect.sizeDelta = new Vector2(620f, 74f);
            row.Bind(id, description, progress, target, complete, claim);
        }

        private void EnsureUi()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            Image bg = GetComponent<Image>();
            if (bg == null)
                bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);

            if (closeButton == null)
                closeButton = CreateButton("CloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -30f), new Vector2(48f, 44f));
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);

            if (dailyTabButton == null)
                dailyTabButton = CreateButton("DailyTab", "일일미션", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -34f), new Vector2(150f, 48f));
            if (repeatTabButton == null)
                repeatTabButton = CreateButton("RepeatTab", "반복미션", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(292f, -34f), new Vector2(150f, 48f));
            dailyTabButton.onClick.RemoveAllListeners();
            repeatTabButton.onClick.RemoveAllListeners();
            dailyTabButton.onClick.AddListener(() => { showingDaily = true; Refresh(); });
            repeatTabButton.onClick.AddListener(() => { showingDaily = false; Refresh(); });

            if (rowContainer == null)
            {
                GameObject rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
                rows.transform.SetParent(transform, false);
                RectTransform rect = rows.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -54f);
                rect.sizeDelta = new Vector2(660f, -82f);
                VerticalLayoutGroup layout = rows.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childControlHeight = false;
                layout.childControlWidth = false;
                layout.childAlignment = TextAnchor.UpperCenter;
                rowContainer = rows.transform;
            }
        }

        private Button CreateButton(string name, string text, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.14f, 0.22f, 0.36f, 0.96f);
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TMP_Text label = labelGo.GetComponent<TMP_Text>();
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 14f;
            label.color = Color.white;
            return go.GetComponent<Button>();
        }

        private void OnDestroy()
        {
            if (service != null)
                service.StateChanged -= Refresh;
        }
    }
}
