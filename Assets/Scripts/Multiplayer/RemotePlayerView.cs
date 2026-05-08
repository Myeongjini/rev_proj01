using TMPro;
using UnityEngine;

namespace WizardGrower.Multiplayer
{
    public class RemotePlayerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private float lerpDuration = 0.2f;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float lerpStartedAt;

        public string Uid { get; private set; }
        public long LastUpdateUnixMs { get; private set; }

        public void Initialize(string uid, string displayName, Vector2 initialPos)
        {
            Uid = uid;
            transform.position = initialPos;
            startPosition = transform.position;
            targetPosition = transform.position;
            lerpStartedAt = Time.time;
            SetName(displayName);
        }

        public void SetTarget(Vector2 newPos)
        {
            startPosition = transform.position;
            targetPosition = new Vector3(newPos.x, newPos.y, transform.position.z);
            lerpStartedAt = Time.time;
        }

        public void Touch(long unixMs)
        {
            LastUpdateUnixMs = unixMs;
        }

        public bool IsStale(long nowUnixMs, long thresholdMs = 30000)
        {
            return LastUpdateUnixMs > 0 && nowUnixMs - LastUpdateUnixMs > thresholdMs;
        }

        private void Awake()
        {
            if (body == null)
                body = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (lerpDuration <= 0f)
            {
                transform.position = targetPosition;
                return;
            }

            float t = Mathf.Clamp01((Time.time - lerpStartedAt) / lerpDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
        }

        private void SetName(string displayName)
        {
            if (nameLabel == null)
                return;

            nameLabel.text = string.IsNullOrWhiteSpace(displayName) ? "Guest" : displayName;
        }
    }
}
