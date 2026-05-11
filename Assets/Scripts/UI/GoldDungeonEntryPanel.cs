using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class GoldDungeonEntryPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text remainingLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private Button enterButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private string dungeonSceneName = "GoldDungeonScene";

        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            EnsureUi();
            Close();
        }

        public void Bind()
        {
            EnsureUi();
            Refresh(3, 3);
        }

        public void Open()
        {
            EnsureUi();
            gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            Refresh(3, 3);
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

        public void Refresh(int remainingEntries, int dailyLimit)
        {
            EnsureUi();
            if (remainingLabel != null)
                remainingLabel.text = $"잔여 입장 {Mathf.Max(0, remainingEntries)}/{Mathf.Max(1, dailyLimit)}";
            if (feedbackLabel != null)
                feedbackLabel.text = "난이도 Lv1";
            if (enterButton != null)
                enterButton.interactable = remainingEntries > 0;
        }

        private void Enter()
        {
            SceneManager.LoadSceneAsync(dungeonSceneName, LoadSceneMode.Single);
        }

        private void EnsureUi()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            Image overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.55f);

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(transform, false);
                RectTransform panelRect = panelGo.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(620f, 430f);
                panelRect.anchoredPosition = Vector2.zero;
                panelGo.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
                panel = panelGo.transform;
            }

            if (titleLabel == null)
                titleLabel = CreateText(panel, "Title", "골드던전", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -52f), new Vector2(-120f, 58f), 30f, FontStyles.Bold);
            if (remainingLabel == null)
                remainingLabel = CreateText(panel, "Remaining", "잔여 입장 3/3", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 74f), new Vector2(-90f, 48f), 22f, FontStyles.Bold);
            if (feedbackLabel == null)
                feedbackLabel = CreateText(panel, "Feedback", "난이도 Lv1", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 24f), new Vector2(-90f, 44f), 18f, FontStyles.Normal);
            if (enterButton == null)
                enterButton = CreateButton(panel, "EnterButton", "입장", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 70f), new Vector2(200f, 60f), new Color(0.12f, 0.36f, 0.92f, 1f));
            if (cancelButton == null)
                cancelButton = CreateButton(panel, "CancelButton", "취소", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120f, 70f), new Vector2(200f, 60f), new Color(0.20f, 0.22f, 0.26f, 1f));

            enterButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(Enter);
            cancelButton.onClick.AddListener(Close);
        }

        private TMP_Text CreateText(Transform parent, string name, string text, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, float fontSize, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            TMP_Text label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = Color.white;
            return label;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            CreateText(go.transform, "Label", text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18f, FontStyles.Bold);
            return go.GetComponent<Button>();
        }
    }
}
