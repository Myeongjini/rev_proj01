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
            ResolveReferences();
            WireButtons();
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
                if (cellPrefab == null)
                    return;

                AttendanceDayCellView cell = Instantiate(cellPrefab, cellContainer);
                RectTransform rect = cell.transform as RectTransform;
                if (rect != null)
                    rect.sizeDelta = new Vector2(116f, 86f);
                cell.Bind(dayIndex, service.GetRewardForDay(dayIndex), service.GetCellState(dayIndex), () => service.TryClaimTodayAsync());
            }
        }

        private void ResolveReferences()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (closeButton == null)
                closeButton = FindButton("CloseButton");
            if (cellContainer == null)
                cellContainer = FindChildTransform("Cells");
        }

        private void WireButtons()
        {
            if (closeButton == null)
                return;

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
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
