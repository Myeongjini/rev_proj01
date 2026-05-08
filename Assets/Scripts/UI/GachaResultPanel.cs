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

            for (int i = 0; i < sorted.Count; i++)
            {
                GameObject card = CreateCard(sorted[i]);
                card.SetActive(false);
            }

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
                yield return new WaitForSecondsRealtime(0.06f);
            }
        }

        private GameObject CreateCard(WeaponDefinition weapon)
        {
            GameObject root = new GameObject("GachaResultCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
            root.transform.SetParent(cardContainer, false);
            Image frame = root.GetComponent<Image>();
            frame.color = weapon != null ? RarityVisuals.ColorFor(weapon.upperGrade) : Color.white;
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 190f);

            Image icon = CreateImage("Icon", root.transform, weapon != null ? weapon.icon : null);
            icon.rectTransform.sizeDelta = new Vector2(88f, 88f);
            TMP_Text name = CreateText("Name", root.transform, weapon != null ? weapon.displayName : "-", 18f, FontStyles.Bold);
            TMP_Text rarity = CreateText("Rarity", root.transform, weapon != null ? WeaponGradeLabels.Display(weapon.upperGrade, weapon.lowerGrade) : "-", 15f, FontStyles.Normal);
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
            if (textFont != null)
                label.font = textFont;
            return label;
        }

        private void ClearCards()
        {
            if (cardContainer == null)
                return;

            for (int i = cardContainer.childCount - 1; i >= 0; i--)
                Destroy(cardContainer.GetChild(i).gameObject);
        }
    }
}
