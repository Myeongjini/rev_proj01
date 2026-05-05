using UnityEngine;

namespace WizardGrower.UI
{
    [RequireComponent(typeof(Camera))]
    public class MobileCameraFitter : MonoBehaviour
    {
        [SerializeField] private float minVisibleWidth = 5.6f;
        [SerializeField] private float minVisibleHeight = 9.2f;
        [SerializeField] private float maxOrthographicSize = 6.2f;
        [SerializeField] private SpriteRenderer fittedBackground;

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
            if (lastWidth != Screen.width || lastHeight != Screen.height)
                Apply();
        }

        public void SetBackground(SpriteRenderer background)
        {
            fittedBackground = background;
            Apply();
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

        private void FitBackground()
        {
            if (fittedBackground == null || fittedBackground.sprite == null)
                return;

            Vector2 spriteSize = fittedBackground.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float worldHeight = targetCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * targetCamera.aspect;
            float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
            fittedBackground.transform.localScale = new Vector3(scale, scale, 1f);
            fittedBackground.transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, 0f);
        }
    }
}
