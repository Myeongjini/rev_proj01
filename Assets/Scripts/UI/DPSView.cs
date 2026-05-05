using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WizardGrower.Combat;

namespace WizardGrower.UI
{
    public class DPSView : MonoBehaviour
    {
        private struct Sample
        {
            public float time;
            public float damage;
        }

        [SerializeField] private TMP_Text label;
        [SerializeField] private float windowSeconds = 5f;
        private readonly Queue<Sample> samples = new Queue<Sample>();

        public void Record(DamageInfo info)
        {
            samples.Enqueue(new Sample { time = Time.time, damage = info.Amount });
        }

        private void Update()
        {
            while (samples.Count > 0 && Time.time - samples.Peek().time > windowSeconds)
                samples.Dequeue();

            float total = 0f;
            foreach (Sample sample in samples)
                total += sample.damage;

            if (label != null)
                label.text = $"DPS {total / windowSeconds:0.0}";
        }
    }
}
