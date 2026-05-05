using System;
using UnityEngine;

namespace WizardGrower.Player
{
    public class PlayerMana : MonoBehaviour
    {
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float regenerationPerSecond = 5f;
        [SerializeField] private float currentMana = 100f;

        public event Action<float, float> Changed;

        public float Current => currentMana;
        public float Max => maxMana;
        public float RegenerationPerSecond => regenerationPerSecond;

        private void Awake()
        {
            currentMana = Mathf.Clamp(currentMana, 0f, maxMana);
        }

        private void Update()
        {
            if (currentMana >= maxMana)
                return;

            currentMana = Mathf.Min(maxMana, currentMana + regenerationPerSecond * Time.deltaTime);
            Changed?.Invoke(currentMana, maxMana);
        }

        public bool TrySpend(float amount)
        {
            if (currentMana < amount)
                return false;

            currentMana -= amount;
            Changed?.Invoke(currentMana, maxMana);
            return true;
        }

        public void IncreaseMax(float amount)
        {
            maxMana += amount;
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            Changed?.Invoke(currentMana, maxMana);
        }

        public void IncreaseRegeneration(float amount)
        {
            regenerationPerSecond += amount;
            Changed?.Invoke(currentMana, maxMana);
        }
    }
}
