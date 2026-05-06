using UnityEngine;

namespace WizardGrower.Enemies
{
    public class EnemyWanderController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 0.45f;
        [SerializeField] private float retargetInterval = 1.8f;

        private Vector2 minBounds = new Vector2(-2.35f, -3.45f);
        private Vector2 maxBounds = new Vector2(2.35f, 3.15f);
        private Vector3 destination;
        private float timer;

        private void OnEnable()
        {
            PickDestination();
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            PickDestination();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f || Vector3.Distance(transform.position, destination) <= 0.08f)
                PickDestination();

            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
        }

        private void PickDestination()
        {
            timer = retargetInterval + Random.Range(-0.45f, 0.65f);
            destination = new Vector3(
                Random.Range(minBounds.x, maxBounds.x),
                Random.Range(minBounds.y, maxBounds.y),
                transform.position.z);
        }
    }
}
