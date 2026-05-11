using UnityEngine;

namespace WizardGrower.Enemies
{
    public class EliteMonsterController : MonoBehaviour
    {
        [SerializeField] private Color eliteTint = new Color(1f, 0.84f, 0f, 1f);

        public void ApplyEliteVisuals(GameObject glowPrefab = null)
        {
            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
                renderer.color = eliteTint;

            if (glowPrefab != null)
                Instantiate(glowPrefab, transform);
        }
    }
}
