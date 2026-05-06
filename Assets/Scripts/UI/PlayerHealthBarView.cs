using TMPro;
using UnityEngine;
using WizardGrower.Player;

namespace WizardGrower.UI
{
    public class PlayerHealthBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private TMP_Text label;

        private PlayerStats stats;

        public void Bind(PlayerStats stats)
        {
            if (this.stats != null)
                this.stats.HealthChanged -= Refresh;

            this.stats = stats;

            if (this.stats != null)
                this.stats.HealthChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (stats != null)
                stats.HealthChanged -= Refresh;
        }

        private void Refresh()
        {
            if (stats == null)
                return;

            float max = Mathf.Max(1f, stats.MaxHealth);
            float normalized = Mathf.Clamp01(stats.CurrentHealth / max);

            if (fill != null)
                fill.anchorMax = new Vector2(normalized, fill.anchorMax.y);

            if (label != null)
                label.text = $"HP {Mathf.CeilToInt(stats.CurrentHealth)} / {Mathf.CeilToInt(max)}";
        }
    }
}
