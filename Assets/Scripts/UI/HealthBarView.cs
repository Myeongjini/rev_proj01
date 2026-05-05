using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Combat;
using WizardGrower.Enemies;

namespace WizardGrower.UI
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

        private EnemyBase target;
        private Camera mainCamera;

        public void Bind(EnemyBase enemy)
        {
            if (target != null)
                target.Damaged -= OnDamaged;

            target = enemy;
            if (target != null)
                target.Damaged += OnDamaged;
            Refresh();
        }

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (target == null || mainCamera == null)
                return;

            transform.position = mainCamera.WorldToScreenPoint(target.transform.position + worldOffset);
        }

        private void OnDamaged(DamageInfo info)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (target == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (slider != null)
                slider.value = target.CurrentHealth / target.MaxHealth;
            if (label != null)
                label.text = $"{Mathf.CeilToInt(target.CurrentHealth)} / {Mathf.CeilToInt(target.MaxHealth)}";
        }
    }
}
