using System;

namespace WizardGrower.Player
{
    public class CombatPowerService
    {
        private PlayerStats stats;
        private PlayerMana mana;

        public float CurrentPower { get; private set; }

        public event Action<float, float> PowerIncreased;
        public event Action<float> PowerChanged;

        public void Initialize(PlayerStats stats, PlayerMana mana)
        {
            if (this.stats != null)
                this.stats.Changed -= OnStatsChanged;
            if (this.mana != null)
                this.mana.Changed -= OnManaChanged;

            this.stats = stats;
            this.mana = mana;

            if (this.stats != null)
                this.stats.Changed += OnStatsChanged;
            if (this.mana != null)
                this.mana.Changed += OnManaChanged;

            Recalculate(false);
        }

        public void Recalculate(bool showIncreaseFeedback)
        {
            if (stats == null)
                return;

            float previous = CurrentPower;
            CurrentPower = Calculate(stats, mana);
            stats.SetCombatPower(CurrentPower);
            PowerChanged?.Invoke(CurrentPower);

            float delta = CurrentPower - previous;
            if (showIncreaseFeedback && delta > 0.01f)
                PowerIncreased?.Invoke(CurrentPower, delta);
        }

        private void OnStatsChanged()
        {
            Recalculate(true);
        }

        private void OnManaChanged(float current, float max)
        {
            Recalculate(true);
        }

        private static float Calculate(PlayerStats stats, PlayerMana mana)
        {
            float maxMana = mana != null ? mana.Max : 0f;
            return stats.AttackDamage
                * (1f + stats.CriticalChance * (stats.CriticalMultiplier - 1f))
                * (1f / UnityEngine.Mathf.Max(0.05f, stats.AutoAttackInterval))
                + stats.ArmorPenetration * 2f
                + stats.MaxHealth * 0.1f
                + maxMana * 0.05f;
        }
    }
}
