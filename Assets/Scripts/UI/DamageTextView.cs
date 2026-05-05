using TMPro;
using UnityEngine;

namespace WizardGrower.UI
{
    public class DamageTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float lifetime = 0.75f;
        [SerializeField] private float riseSpeed = 80f;

        private float timer;

        public void Show(float amount, bool critical)
        {
            timer = lifetime;
            if (label != null)
            {
                label.text = critical ? $"CRIT {Mathf.CeilToInt(amount)}" : Mathf.CeilToInt(amount).ToString();
                label.color = critical ? new Color(1f, 0.72f, 0.12f) : Color.white;
                label.fontSize = critical ? 34f : 24f;
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            if (label != null)
            {
                Color color = label.color;
                color.a = Mathf.Clamp01(timer / lifetime);
                label.color = color;
            }

            if (timer <= 0f)
                Destroy(gameObject);
        }
    }
}
