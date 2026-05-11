using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Player;

namespace WizardGrower.UI
{
    public class PlayerExpBarView : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;

        private PlayerLevelService service;

        public void Bind(PlayerLevelService service)
        {
            EnsureUi();
            if (this.service != null)
            {
                this.service.LevelChanged -= OnLevelChanged;
                this.service.ExpChanged -= Refresh;
            }

            this.service = service;

            if (this.service != null)
            {
                this.service.LevelChanged += OnLevelChanged;
                this.service.ExpChanged += Refresh;
                Refresh(this.service.CurrentExp, this.service.ExpToNext);
            }
        }

        private void OnDestroy()
        {
            if (service != null)
            {
                service.LevelChanged -= OnLevelChanged;
                service.ExpChanged -= Refresh;
            }
        }

        private void OnLevelChanged(int _)
        {
            if (service != null)
                Refresh(service.CurrentExp, service.ExpToNext);
        }

        private void Refresh(int currentExp, int expToNext)
        {
            EnsureUi();
            if (slider != null)
                slider.value = expToNext < 0 ? 1f : Mathf.Clamp01(currentExp / Mathf.Max(1f, currentExp + expToNext));

            if (label == null)
                return;

            if (service == null)
                label.text = "Lv.1 EXP 0 / 100";
            else if (expToNext < 0)
                label.text = $"Lv.{service.CurrentLevel} MAX";
            else
                label.text = $"Lv.{service.CurrentLevel} EXP {currentExp} / {currentExp + expToNext}";
        }

        private void EnsureUi()
        {
            if (slider == null)
                slider = GetComponentInChildren<Slider>(true);
            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);

            if (slider == null)
            {
                GameObject sliderGo = new GameObject("ExpSlider", typeof(RectTransform), typeof(Slider));
                sliderGo.transform.SetParent(transform, false);
                RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
                sliderRect.anchorMin = Vector2.zero;
                sliderRect.anchorMax = Vector2.one;
                sliderRect.offsetMin = Vector2.zero;
                sliderRect.offsetMax = Vector2.zero;
                slider = sliderGo.GetComponent<Slider>();
                slider.interactable = false;

                GameObject backgroundGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                backgroundGo.transform.SetParent(sliderGo.transform, false);
                RectTransform bgRect = backgroundGo.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                backgroundGo.GetComponent<Image>().color = new Color(0.15f, 0.10f, 0.22f, 0.86f);

                GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
                fillAreaGo.transform.SetParent(sliderGo.transform, false);
                RectTransform fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.offsetMin = new Vector2(2f, 2f);
                fillAreaRect.offsetMax = new Vector2(-2f, -2f);

                GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(fillAreaGo.transform, false);
                RectTransform fillRect = fillGo.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fillGo.GetComponent<Image>().color = new Color(0.78f, 0.44f, 1f, 0.95f);
                slider.fillRect = fillRect;
                slider.targetGraphic = fillGo.GetComponent<Image>();
            }

            if (label == null)
            {
                GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(transform, false);
                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label = labelGo.GetComponent<TMP_Text>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 13f;
                label.fontStyle = FontStyles.Bold;
                label.color = Color.white;
            }
        }
    }
}
