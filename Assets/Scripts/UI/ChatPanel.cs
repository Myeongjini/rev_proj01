using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Chat;
using WizardGrower.Stages;

namespace WizardGrower.UI
{
    public class ChatPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button worldTabButton;
        [SerializeField] private Button stageTabButton;
        [SerializeField] private Button sendButton;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform messageContainer;
        [SerializeField] private ChatMessageView messagePrefab;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private float sendCooldownSeconds = 2f;
        [SerializeField] private int messageLimit = 50;

        private ChatService chatService;
        private StageManager stageManager;
        private IDisposable subscription;
        private readonly List<ChatMessageView> messageViews = new List<ChatMessageView>();
        private readonly HashSet<string> seenMessages = new HashSet<string>();
        private ChatChannel currentChannel = ChatChannel.World;
        private string currentStageKey = "1_1";
        private float nextSendTime;
        private float feedbackTimer;
        private bool initialized;
        private bool visible;

        public bool IsVisible => visible;

        public void Initialize(ChatService chatService, StageManager stageManager)
        {
            this.chatService = chatService;
            this.stageManager = stageManager;
            initialized = true;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (worldTabButton != null)
                worldTabButton.onClick.AddListener(() => SwitchChannel(ChatChannel.World));
            if (stageTabButton != null)
                stageTabButton.onClick.AddListener(() => SwitchChannel(ChatChannel.Stage));
            if (sendButton != null)
                sendButton.onClick.AddListener(SendCurrent);
            if (inputField != null)
            {
                inputField.characterLimit = 200;
                inputField.lineType = TMP_InputField.LineType.SingleLine;
                inputField.onValueChanged.AddListener(_ => RefreshSendButton());
                inputField.onSubmit.AddListener(_ => SendCurrent());
            }

            if (stageManager != null)
            {
                currentStageKey = BuildStageKey(stageManager.CurrentChapterNumber, stageManager.CurrentStageNumber);
                stageManager.StateChanged += OnStageChanged;
            }

            SetVisible(false);
            RefreshSendButton();
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
            if (stageManager != null)
                stageManager.StateChanged -= OnStageChanged;
        }

        private void Update()
        {
            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f && feedbackLabel != null)
                    feedbackLabel.text = string.Empty;
            }

            RefreshSendButton();
        }

        public void Toggle()
        {
            SetVisible(!visible);
        }

        public void SetVisible(bool show)
        {
            visible = show;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.interactable = show;
                canvasGroup.blocksRaycasts = show;
            }
            else
            {
                gameObject.SetActive(show);
            }

            if (show)
                Resubscribe();
            else
                subscription?.Dispose();
        }

        public void SwitchChannel(ChatChannel channel)
        {
            if (currentChannel == channel)
                return;

            currentChannel = channel;
            ClearMessages();
            Resubscribe();
        }

        private async void SendCurrent()
        {
            if (!initialized || chatService == null)
                return;

            string text = Sanitize(inputField != null ? inputField.text : string.Empty);
            if (string.IsNullOrEmpty(text))
                return;
            if (Time.unscaledTime < nextSendTime)
            {
                ShowFeedback("잠시만 기다려주세요");
                return;
            }

            try
            {
                nextSendTime = Time.unscaledTime + sendCooldownSeconds;
                RefreshSendButton();
                await chatService.SendAsync(currentChannel, currentStageKey, text);
                // 로컬 즉시 추가 제거: 서버 OnChildAdded echo로 단일 경로 출력 (중복 방지).
                if (inputField != null)
                    inputField.text = string.Empty;
                RefreshSendButton();
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.GetBaseException().Message);
            }
        }

        private void Resubscribe()
        {
            subscription?.Dispose();
            subscription = null;
            if (!visible || chatService == null || !chatService.IsInitialized)
                return;

            try
            {
                subscription = chatService.SubscribeChannel(currentChannel, currentStageKey, messageLimit, AddMessage);
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.GetBaseException().Message);
            }
        }

        private void AddMessage(ChatMessage message)
        {
            if (messageContainer == null || messagePrefab == null || string.IsNullOrEmpty(message.Text))
                return;

            string key = string.Format("{0}|{1}|{2}", message.Uid, message.Ts, message.Text);
            if (!seenMessages.Add(key))
                return;

            ChatMessageView view = Instantiate(messagePrefab, messageContainer);
            view.Bind(message);
            messageViews.Add(view);

            while (messageViews.Count > messageLimit)
            {
                ChatMessageView old = messageViews[0];
                messageViews.RemoveAt(0);
                if (old != null)
                    Destroy(old.gameObject);
            }

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private void ClearMessages()
        {
            seenMessages.Clear();
            for (int i = 0; i < messageViews.Count; i++)
            {
                if (messageViews[i] != null)
                    Destroy(messageViews[i].gameObject);
            }

            messageViews.Clear();
        }

        private void OnStageChanged(ChapterDefinition chapter, StageDefinition stage, StageMode mode)
        {
            string nextStageKey = BuildStageKey(stageManager.CurrentChapterNumber, stageManager.CurrentStageNumber);
            if (currentStageKey == nextStageKey)
                return;

            currentStageKey = nextStageKey;
            if (currentChannel == ChatChannel.Stage)
            {
                ClearMessages();
                Resubscribe();
            }
        }

        private void RefreshSendButton()
        {
            if (sendButton == null)
                return;

            bool hasText = !string.IsNullOrEmpty(Sanitize(inputField != null ? inputField.text : string.Empty));
            sendButton.interactable = initialized && chatService != null && chatService.IsInitialized && hasText && Time.unscaledTime >= nextSendTime;
        }

        private void ShowFeedback(string message)
        {
            if (feedbackLabel == null)
                return;

            feedbackTimer = 1.5f;
            feedbackLabel.text = message;
        }

        private static string BuildStageKey(int chapterNumber, int stageNumber)
        {
            return ChatService.BuildStageKey(chapterNumber, stageNumber);
        }

        private static string Sanitize(string text)
        {
            return (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
