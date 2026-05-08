using System;
using UnityEngine;
using WizardGrower.Player;

namespace WizardGrower.Skills
{
    public class SkillRuntime
    {
        private float cooldownRemaining;

        public SkillRuntime(SkillDefinition definition)
        {
            Definition = definition;
        }

        public SkillDefinition Definition { get; }
        public float CooldownRemaining => cooldownRemaining;
        public float CooldownDuration => Definition != null ? Mathf.Max(0.01f, Definition.cooldownSeconds) : 0.01f;

        public event Action<float> CooldownChanged;

        public bool IsReady(PlayerMana mana)
        {
            return Definition != null
                && cooldownRemaining <= 0f
                && mana != null
                && mana.Current >= Definition.manaCost;
        }

        public void Tick(float deltaTime)
        {
            if (cooldownRemaining <= 0f)
                return;

            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Mathf.Max(0f, deltaTime));
            CooldownChanged?.Invoke(cooldownRemaining);
        }

        public bool TrySpendAndStart(PlayerMana mana)
        {
            if (!IsReady(mana) || !mana.TrySpend(Definition.manaCost))
                return false;

            cooldownRemaining = CooldownDuration;
            CooldownChanged?.Invoke(cooldownRemaining);
            return true;
        }

        public void ResetCooldown()
        {
            cooldownRemaining = 0f;
            CooldownChanged?.Invoke(cooldownRemaining);
        }
    }
}
