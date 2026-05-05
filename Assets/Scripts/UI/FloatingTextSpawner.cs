using UnityEngine;
using WizardGrower.Combat;

namespace WizardGrower.UI
{
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private DamageTextView damageTextPrefab;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera mainCamera;

        public void Spawn(Vector3 worldPosition, DamageInfo info)
        {
            if (damageTextPrefab == null || canvas == null)
                return;

            if (mainCamera == null)
                mainCamera = Camera.main;

            DamageTextView view = Instantiate(damageTextPrefab, canvas.transform);
            view.transform.position = mainCamera.WorldToScreenPoint(worldPosition + Vector3.up * 0.35f);
            view.Show(info.Amount, info.IsCritical);
        }
    }
}
