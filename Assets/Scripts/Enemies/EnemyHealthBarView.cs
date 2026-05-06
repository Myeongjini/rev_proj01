using UnityEngine;
using WizardGrower.Combat;

namespace WizardGrower.Enemies
{
    public class EnemyHealthBarView : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.72f, 0f);
        [SerializeField] private Vector2 size = new Vector2(0.78f, 0.1f);

        private EnemyBase enemy;
        private Transform barRoot;
        private Transform fill;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            CreateBar();
        }

        private void OnEnable()
        {
            if (enemy != null)
                enemy.Damaged += OnDamaged;
            Refresh();
        }

        private void OnDisable()
        {
            if (enemy != null)
                enemy.Damaged -= OnDamaged;
        }

        private void LateUpdate()
        {
            if (barRoot != null)
                barRoot.position = transform.position + worldOffset;
        }

        private void OnDamaged(DamageInfo info)
        {
            Refresh();
        }

        private void CreateBar()
        {
            if (barRoot != null)
                return;

            barRoot = new GameObject("WorldHealthBar").transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = worldOffset;

            CreateSprite("Back", barRoot, new Color(0.08f, 0.08f, 0.08f, 0.85f), size, new Vector3(0f, 0f, 0.02f));
            fill = CreateSprite("Fill", barRoot, new Color(0.25f, 0.95f, 0.25f, 0.95f), size * 0.86f, new Vector3(-size.x * 0.07f, 0f, 0.01f));
        }

        private Transform CreateSprite(string name, Transform parent, Color color, Vector2 scale, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            renderer.color = color;
            renderer.sortingOrder = 40;
            return go.transform;
        }

        private void Refresh()
        {
            if (enemy == null || fill == null)
                return;

            float normalized = Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth);
            fill.localScale = new Vector3(size.x * 0.86f * normalized, size.y * 0.86f, 1f);
            fill.localPosition = new Vector3(-size.x * 0.43f * (1f - normalized) - size.x * 0.07f, 0f, 0.01f);
        }
    }
}
