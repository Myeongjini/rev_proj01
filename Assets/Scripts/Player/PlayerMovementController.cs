using System;
using UnityEngine;
using UnityEngine.EventSystems;
using WizardGrower.Enemies;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WizardGrower.Player
{
    public class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private PlayerWizard wizard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private float manualMoveSpeed = 3.2f;
        [SerializeField] private float autoMoveSpeed = 2.0f;
        [SerializeField] private float joystickDeadZone = 22f;
        [SerializeField] private float autoStopDistance = 2.25f;
        [SerializeField] private Vector2 minBounds = new Vector2(-2.35f, -3.45f);
        [SerializeField] private Vector2 maxBounds = new Vector2(2.35f, 3.15f);

        private bool autoModeEnabled = true;
        private bool manualPointerActive;
        private Vector2 pointerStart;
        private Vector2 pointerCurrent;

        public event Action<bool> AutoModeChanged;
        public event Action<bool, Vector2, Vector2> JoystickChanged;

        public bool IsManualMoving { get; private set; }
        public bool AutoModeEnabled => autoModeEnabled;

        public void Initialize(PlayerWizard wizard, EnemySpawner enemySpawner)
        {
            this.wizard = wizard;
            this.enemySpawner = enemySpawner;
        }

        private void Awake()
        {
            if (wizard == null)
                wizard = GetComponent<PlayerWizard>();
        }

        private void Update()
        {
            UpdateManualPointer();

            if (IsManualMoving)
            {
                Vector2 direction = (pointerCurrent - pointerStart).normalized;
                Move(direction, manualMoveSpeed);
                return;
            }

            if (autoModeEnabled)
                AutoMove();
        }

        public void ToggleAutoMode()
        {
            SetAutoMode(!autoModeEnabled);
        }

        public void SetAutoMode(bool enabled)
        {
            autoModeEnabled = enabled;
            AutoModeChanged?.Invoke(autoModeEnabled);
        }

        private void UpdateManualPointer()
        {
            bool pointerDown;
            bool pointerHeld;
            bool pointerUp;
            Vector2 pointerPosition;
            ReadPointer(out pointerDown, out pointerHeld, out pointerUp, out pointerPosition);

            if (pointerDown && !IsPointerOverUi())
            {
                manualPointerActive = true;
                pointerStart = pointerPosition;
                pointerCurrent = pointerPosition;
            }

            if (manualPointerActive && pointerHeld)
                pointerCurrent = pointerPosition;

            if (manualPointerActive && pointerUp)
            {
                manualPointerActive = false;
                IsManualMoving = false;
                JoystickChanged?.Invoke(false, pointerStart, pointerCurrent);
                return;
            }

            bool moving = manualPointerActive && Vector2.Distance(pointerStart, pointerCurrent) >= joystickDeadZone;
            if (moving != IsManualMoving)
                IsManualMoving = moving;

            JoystickChanged?.Invoke(manualPointerActive, pointerStart, pointerCurrent);
        }

        private void AutoMove()
        {
            if (wizard == null || enemySpawner == null)
                return;

            EnemyBase target = enemySpawner.GetNearestEnemy(wizard.transform.position);
            if (target == null)
                return;

            Vector3 enemyPosition = target.HitTransform.position;
            Vector3 toEnemy = enemyPosition - wizard.transform.position;
            if (toEnemy.magnitude <= autoStopDistance)
                return;

            Move(toEnemy.normalized, autoMoveSpeed);
        }

        private void Move(Vector2 direction, float speed)
        {
            if (wizard == null || direction.sqrMagnitude <= 0.0001f)
                return;

            Vector3 next = wizard.transform.position + (Vector3)(direction.normalized * speed * Time.deltaTime);
            next.x = Mathf.Clamp(next.x, minBounds.x, maxBounds.x);
            next.y = Mathf.Clamp(next.y, minBounds.y, maxBounds.y);
            wizard.transform.position = next;
        }

        private bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
                return false;

#if ENABLE_INPUT_SYSTEM
            return EventSystem.current.IsPointerOverGameObject();
#else
            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

            return EventSystem.current.IsPointerOverGameObject();
#endif
        }

        private static void ReadPointer(out bool down, out bool held, out bool up, out Vector2 position)
        {
            down = false;
            held = false;
            up = false;
            position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                var touch = Touchscreen.current.primaryTouch;
                down = touch.press.wasPressedThisFrame;
                held = touch.press.isPressed;
                up = touch.press.wasReleasedThisFrame;
                position = touch.position.ReadValue();
                return;
            }

            if (Mouse.current != null)
            {
                down = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                up = Mouse.current.leftButton.wasReleasedThisFrame;
                position = Mouse.current.position.ReadValue();
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                down = touch.phase == TouchPhase.Began;
                held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                position = touch.position;
                return;
            }

            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            up = Input.GetMouseButtonUp(0);
            position = Input.mousePosition;
#endif
        }
    }
}
