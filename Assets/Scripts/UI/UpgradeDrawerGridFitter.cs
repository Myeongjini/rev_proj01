using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class UpgradeDrawerGridFitter : MonoBehaviour
    {
        [SerializeField] private int columns = 2;
        [SerializeField] private float cellHeight = 176f;

        private GridLayoutGroup grid;
        private RectTransform rectTransform;
        private float lastWidth;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (rectTransform != null && Mathf.Abs(rectTransform.rect.width - lastWidth) > 0.5f)
                Apply();
        }

        private void Apply()
        {
            if (grid == null)
                grid = GetComponent<GridLayoutGroup>();
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (grid == null || rectTransform == null)
                return;

            int columnCount = Mathf.Max(1, columns);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columnCount;

            float usableWidth = rectTransform.rect.width - grid.padding.left - grid.padding.right - grid.spacing.x * (columnCount - 1);
            float cellWidth = Mathf.Max(220f, usableWidth / columnCount);
            grid.cellSize = new Vector2(cellWidth, cellHeight);
            lastWidth = rectTransform.rect.width;
        }
    }
}
