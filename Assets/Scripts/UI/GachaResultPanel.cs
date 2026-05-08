using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class GachaResultPanel : MonoBehaviour
    {
        private struct CardLayout
        {
            public int Columns;
            public Vector2 CellSize;
            public Vector2 Spacing;
            public RectOffset Padding;
            public float InnerSpacing;
            public float IconSize;
            public float NameFontSize;
            public float RarityFontSize;
            public float NameHeight;
            public float RarityHeight;
        }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_FontAsset textFont;

        private Coroutine revealRoutine;

        private void Awake()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        public void Show(IReadOnlyList<WeaponDefinition> weapons)
        {
            if (group == null || cardContainer == null)
                return;

            ClearCards();
            List<WeaponDefinition> sorted = new List<WeaponDefinition>();
            if (weapons != null)
                sorted.AddRange(weapons);
            sorted.Sort((a, b) =>
            {
                int aIndex = a != null ? a.ladderIndex : -1;
                int bIndex = b != null ? b.ladderIndex : -1;
                return bIndex.CompareTo(aIndex);
            });

            CardLayout layout = ConfigureLayout(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                GameObject card = CreateCard(sorted[i], layout);
                card.SetActive(false);
            }
            ForceLayout();

            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
            gameObject.SetActive(true);

            if (revealRoutine != null)
                StopCoroutine(revealRoutine);
            revealRoutine = StartCoroutine(RevealCards());
        }

        public void Close()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator RevealCards()
        {
            for (int i = 0; i < cardContainer.childCount; i++)
            {
                cardContainer.GetChild(i).gameObject.SetActive(true);
                ForceLayout();
                yield return new WaitForSecondsRealtime(0.06f);
            }
        }

        private GameObject CreateCard(WeaponDefinition weapon, CardLayout layoutInfo)
        {
            GameObject root = new GameObject("GachaResultCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
            root.transform.SetParent(cardContainer, false);
            Image frame = root.GetComponent<Image>();
            frame.color = weapon != null ? RarityVisuals.ColorFor(weapon.upperGrade) : Color.white;
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = layoutInfo.Padding;
            layout.spacing = layoutInfo.InnerSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = layoutInfo.CellSize;

            Image icon = CreateImage("Icon", root.transform, weapon != null ? weapon.icon : null);
            SetPreferredSize(icon.gameObject, layoutInfo.IconSize, layoutInfo.IconSize);
            TMP_Text name = CreateText("Name", root.transform, weapon != null ? weapon.displayName : "-", layoutInfo.NameFontSize, FontStyles.Bold);
            SetPreferredSize(name.gameObject, layoutInfo.CellSize.x - layoutInfo.Padding.horizontal, layoutInfo.NameHeight);
            TMP_Text rarity = CreateText("Rarity", root.transform, weapon != null ? WeaponGradeLabels.Display(weapon.upperGrade, weapon.lowerGrade) : "-", layoutInfo.RarityFontSize, FontStyles.Normal);
            SetPreferredSize(rarity.gameObject, layoutInfo.CellSize.x - layoutInfo.Padding.horizontal, layoutInfo.RarityHeight);
            rarity.color = new Color(0.08f, 0.07f, 0.05f, 1f);
            name.color = Color.black;
            return root;
        }

        private Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            return image;
        }

        private TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(7f, size - 4f);
            label.fontSizeMax = size;
            if (textFont != null)
                label.font = textFont;
            return label;
        }

        private CardLayout ConfigureLayout(int cardCount)
        {
            CardLayout layout = GetLayout(cardCount);
            GridLayoutGroup grid = cardContainer != null ? cardContainer.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = layout.Columns;
                grid.cellSize = layout.CellSize;
                grid.spacing = layout.Spacing;
                grid.childAlignment = TextAnchor.UpperCenter;
            }

            RectTransform rect = cardContainer as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(640f, 370f);
                rect.anchoredPosition = new Vector2(0f, -18f);
            }

            return layout;
        }

        private static CardLayout GetLayout(int cardCount)
        {
            if (cardCount > 10)
            {
                return new CardLayout
                {
                    Columns = 6,
                    CellSize = new Vector2(100f, 66f),
                    Spacing = new Vector2(8f, 8f),
                    Padding = new RectOffset(4, 4, 4, 4),
                    InnerSpacing = 1f,
                    IconSize = 26f,
                    NameFontSize = 10f,
                    RarityFontSize = 9f,
                    NameHeight = 14f,
                    RarityHeight = 12f
                };
            }

            return new CardLayout
            {
                Columns = 5,
                CellSize = new Vector2(112f, 166f),
                Spacing = new Vector2(12f, 12f),
                Padding = new RectOffset(8, 8, 8, 8),
                InnerSpacing = 4f,
                IconSize = 76f,
                NameFontSize = 17f,
                RarityFontSize = 14f,
                NameHeight = 40f,
                RarityHeight = 26f
            };
        }

        private static void SetPreferredSize(GameObject target, float width, float height)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null)
                element = target.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
        }

        private void ForceLayout()
        {
            RectTransform rect = cardContainer as RectTransform;
            if (rect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void ClearCards()
        {
            if (cardContainer == null)
                return;

            for (int i = cardContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = cardContainer.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }
}
