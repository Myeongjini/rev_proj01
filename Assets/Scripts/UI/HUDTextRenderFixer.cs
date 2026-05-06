using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class HUDTextRenderFixer : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private string[] asciiValueLabelNames =
        {
            "GoldLabel",
            "DPSLabel",
            "AttackLabel"
        };

        private void Awake()
        {
            FixCanvas();
            FixValueTexts();
        }

        private void OnEnable()
        {
            FixCanvas();
            FixValueTexts();
        }

        private void LateUpdate()
        {
            FixValueTexts();
            enabled = false;
        }

        private void FixCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 50);

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        private void FixValueTexts()
        {
            TMP_FontAsset fallbackFont = TMP_Settings.defaultFontAsset;
            if (fallbackFont == null)
                return;

            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (!ShouldUseFallbackFont(text))
                    continue;

                text.font = fallbackFont;
                text.fontSharedMaterial = fallbackFont.material;
                text.color = Color.white;
                text.alpha = 1f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.raycastTarget = false;
                text.ForceMeshUpdate(true);
            }
        }

        private bool ShouldUseFallbackFont(TMP_Text text)
        {
            if (text == null)
                return false;

            if (text.name == "Label" && text.transform.parent != null && text.transform.parent.name == "ManaBar")
                return true;

            foreach (string labelName in asciiValueLabelNames)
            {
                if (text.name == labelName)
                    return true;
            }

            return false;
        }
    }
}
