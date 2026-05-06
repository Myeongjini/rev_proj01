using UnityEngine;

namespace WizardGrower.UI
{
    [RequireComponent(typeof(Camera))]
    public class MobileCameraFitter : MonoBehaviour
    {
        [SerializeField] private float minVisibleWidth = 7.2f;
        [SerializeField] private float minVisibleHeight = 12.4f;
        [SerializeField] private float maxOrthographicSize = 8.0f;
        [SerializeField] private SpriteRenderer fittedBackground;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);
        [SerializeField] private Vector2 mapCenter = Vector2.zero;
        [SerializeField] private Vector2 mapSize = new Vector2(28f, 18f);

        private Camera targetCamera;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            Apply();
        }

        private void LateUpdate()
        {
            FollowTarget();

            if (lastWidth != Screen.width || lastHeight != Screen.height)
                Apply();
        }

        public void SetBackground(SpriteRenderer background)
        {
            fittedBackground = background;
            Apply();
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            FollowTarget();
        }

        private void Apply()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();

            float aspect = Mathf.Max(0.1f, targetCamera.aspect);
            float sizeForHeight = minVisibleHeight * 0.5f;
            float sizeForWidth = minVisibleWidth / (2f * aspect);
            targetCamera.orthographicSize = Mathf.Min(maxOrthographicSize, Mathf.Max(sizeForHeight, sizeForWidth));

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            FitBackground();
        }

        private void FollowTarget()
        {
            if (followTarget == null)
                return;

            transform.position = new Vector3(
                followTarget.position.x + followOffset.x,
                followTarget.position.y + followOffset.y,
                followOffset.z);
        }

        private void FitBackground()
        {
            if (fittedBackground == null || fittedBackground.sprite == null)
                return;

            Vector2 spriteSize = fittedBackground.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float worldHeight = Mathf.Max(mapSize.y, targetCamera.orthographicSize * 2f);
            float worldWidth = Mathf.Max(mapSize.x, worldHeight * targetCamera.aspect);
            float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
            fittedBackground.transform.localScale = new Vector3(scale, scale, 1f);
            fittedBackground.transform.position = new Vector3(mapCenter.x, mapCenter.y, 0f);
        }
    }
}
