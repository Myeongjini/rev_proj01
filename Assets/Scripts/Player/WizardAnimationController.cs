using UnityEngine;

namespace WizardGrower.Player
{
    [RequireComponent(typeof(Animator))]
    public class WizardAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string movingParameter = "Moving";
        [SerializeField] private float movingThreshold = 0.0025f;

        private Vector3 lastPosition;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 delta = transform.position - lastPosition;
            if (animator != null)
                animator.SetBool(movingParameter, delta.sqrMagnitude > movingThreshold * movingThreshold);
            lastPosition = transform.position;
        }
    }
}
