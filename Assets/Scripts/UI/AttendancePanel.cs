using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Attendance;

namespace WizardGrower.UI
{
    public class AttendancePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform cellContainer;
        [SerializeField] private AttendanceDayCellView cellPrefab;

        private AttendanceService service;

        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            EnsureUi();
            Close();
        }

        public void Bind(AttendanceService service)
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
            if (cellContainer == null)
                return;

            for (int i = cellContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = cellContainer.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            if (service == null)
                return;

            for (int day = 1; day <= 10; day++)
            {
                int dayIndex = day;
                AttendanceDayCellView cell = cellPrefab != null
                    ? Instantiate(cellPrefab, cellContainer)
                    : new GameObject($"Day{dayIndex}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(AttendanceDayCellView)).GetComponent<AttendanceDayCellView>();
                if (cell.transform.parent != cellContainer)
                    cell.transform.SetParent(cellContainer, false);
                RectTransform rect = cell.transform as RectTransform;
                if (rect != null)
                    rect.sizeDelta = new Vector2(116f, 86f);
                cell.Bind(dayIndex, service.GetRewardForDay(dayIndex), service.GetCellState(dayIndex), () => service.TryClaimToday());
            }
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
            bg.color = new Color(0.045f, 0.045f, 0.06f, 0.96f);

            if (closeButton == null)
                closeButton = CreateButton("CloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -30f), new Vector2(48f, 44f));
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);

            TMP_Text title = transform.Find("Title") != null ? transform.Find("Title").GetComponent<TMP_Text>() : null;
            if (title == null)
            {
                GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleGo.transform.SetParent(transform, false);
                RectTransform titleRect = titleGo.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -32f);
                titleRect.sizeDelta = new Vector2(-90f, 48f);
                title = titleGo.GetComponent<TMP_Text>();
                title.text = "출석 보상";
                title.alignment = TextAlignmentOptions.Center;
                title.fontStyle = FontStyles.Bold;
                title.fontSize = 22f;
                title.color = Color.white;
            }

            if (cellContainer == null)
            {
                GameObject grid = new GameObject("Cells", typeof(RectTransform), typeof(GridLayoutGroup));
                grid.transform.SetParent(transform, false);
                RectTransform rect = grid.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -28f);
                rect.sizeDelta = new Vector2(620f, 188f);
                GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(116f, 86f);
                layout.spacing = new Vector2(10f, 10f);
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 5;
                cellContainer = grid.transform;
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
