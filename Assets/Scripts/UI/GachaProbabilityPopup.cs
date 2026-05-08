using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class GachaProbabilityPopup : MonoBehaviour
    {
        [SerializeField] private Transform rowContainer;
        [SerializeField] private GachaProbabilityRowView rowPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button outsideClickArea;

        private readonly List<GachaProbabilityRowView> rows = new List<GachaProbabilityRowView>();
        private GachaService gachaService;
        private bool visible;

        private void Awake()
        {
            WireButtons();
            Hide();
        }

        public void Bind(GachaService gachaService)
        {
            if (this.gachaService != null)
                this.gachaService.SummonLevelChanged -= OnSummonLevelChanged;

            this.gachaService = gachaService;

            if (this.gachaService != null)
                this.gachaService.SummonLevelChanged += OnSummonLevelChanged;
        }

        public void Show(int summonLevel)
        {
            visible = true;
            gameObject.SetActive(true);
            RefreshRows();
        }

        public void Hide()
        {
            visible = false;
            gameObject.SetActive(false);
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
            if (outsideClickArea != null)
            {
                outsideClickArea.onClick.RemoveListener(Hide);
                outsideClickArea.onClick.AddListener(Hide);
            }
        }

        private void RefreshRows()
        {
            if (rowContainer == null || rowPrefab == null || gachaService == null)
                return;

            rows.Clear();

            IReadOnlyList<WeaponGradeWeight> weights = gachaService.GetCurrentUpperGradeWeightsNormalized();
            for (int i = 0; i < weights.Count; i++)
            {
                WeaponGradeWeight weight = weights[i];
                GachaProbabilityRowView row = GetOrCreateRow(i);
                row.Bind($"{WeaponGradeLabels.UpperKo(weight.upperGrade)}: {weight.weight * 100f:0.0}%");
                row.gameObject.SetActive(true);
                rows.Add(row);
            }

            for (int i = weights.Count; i < rowContainer.childCount; i++)
                rowContainer.GetChild(i).gameObject.SetActive(false);
        }

        private GachaProbabilityRowView GetOrCreateRow(int index)
        {
            if (index < rowContainer.childCount)
            {
                GachaProbabilityRowView existing = rowContainer.GetChild(index).GetComponent<GachaProbabilityRowView>();
                if (existing != null)
                    return existing;
            }

            return Instantiate(rowPrefab, rowContainer);
        }

        private void OnSummonLevelChanged(int _)
        {
            if (visible)
                RefreshRows();
        }

        private void OnDestroy()
        {
            if (gachaService != null)
                gachaService.SummonLevelChanged -= OnSummonLevelChanged;
        }
    }
}
