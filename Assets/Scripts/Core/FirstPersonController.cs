using UnityEngine;
using UnityEngine.InputSystem;

namespace SayItAgain
{
    public enum MoveMode
    {
        /// <summary>Van: stands at the open tailgate and can only look around.</summary>
        LookOnly,
        /// <summary>Tent: walks inside a radius around the anchor.</summary>
        Confined,
        /// <summary>Village: free roam.</summary>
        Free,
    }

    /// <summary>First-person look and walk using the Input System. Movement rules depend on the role's MoveMode.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        public Camera viewCamera;
        public float walkSpeed = 3.6f;
        public float runSpeed = 5.6f;
        public float gravity = 20f;
        public float lookScale = 0.08f;
        public float headBobAmplitude = 0.04f;
        public float headBobFrequency = 9f;

        public MoveMode mode = MoveMode.Free;
        public Vector3 anchor;
        public float confineRadius = 4f;

        /// <summary>Set by menus, cards and pause to stop look/move while the cursor is free.</summary>
        public bool InputBlocked { get; set; }
        public bool IsMoving { get; private set; }
        /// <summary>Set by the world, e.g. wading through flood water.</summary>
        public float speedMultiplier = 1f;

        CharacterController controller;
        InputAction move, look, run;
        float pitch, verticalVelocity, bobTime, cameraBaseY;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            cameraBaseY = viewCamera.transform.localPosition.y;

            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            run = new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
        }

        void OnEnable()
        {
            move.Enable(); look.Enable(); run.Enable();
            SetCursorLocked(true);
        }

        void OnDisable()
        {
            move.Disable(); look.Disable(); run.Disable();
            SetCursorLocked(false);
        }

        void OnDestroy()
        {
            move.Dispose(); look.Dispose(); run.Dispose();
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            if (InputBlocked)
            {
                IsMoving = false;
                ApplyGravityOnly();
                return;
            }

            UpdateLook();
            UpdateMove();
            UpdateHeadBob();
        }

        void UpdateLook()
        {
            var delta = look.ReadValue<Vector2>();
            // Slower look while zoomed so binoculars stay steady.
            float zoomScale = viewCamera.fieldOfView / Mathf.Max(1f, ComfortSettings.FieldOfView);
            float scale = ComfortSettings.MouseSensitivity * lookScale * zoomScale;
            transform.Rotate(0f, delta.x * scale, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y * scale, -80f, 80f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void UpdateMove()
        {
            if (mode == MoveMode.LookOnly)
            {
                IsMoving = false;
                ApplyGravityOnly();
                return;
            }

            var input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            float speed = (run.IsPressed() ? runSpeed : walkSpeed) * speedMultiplier;
            var velocity = (transform.right * input.x + transform.forward * input.y) * speed;
            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity - gravity * Time.deltaTime;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
            IsMoving = input.sqrMagnitude > 0.01f;

            if (mode == MoveMode.Confined) ClampToAnchor();
        }

        void ApplyGravityOnly()
        {
            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity - gravity * Time.deltaTime;
            controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
        }

        void ClampToAnchor()
        {
            var offset = transform.position - anchor;
            offset.y = 0f;
            if (offset.magnitude <= confineRadius) return;
            var target = anchor + offset.normalized * confineRadius;
            controller.Move(new Vector3(target.x - transform.position.x, 0f, target.z - transform.position.z));
        }

        void UpdateHeadBob()
        {
            var pos = viewCamera.transform.localPosition;
            if (ComfortSettings.HeadBob && !ComfortSettings.ReduceMotion && IsMoving)
            {
                bobTime += Time.deltaTime * headBobFrequency;
                pos.y = cameraBaseY + Mathf.Sin(bobTime) * headBobAmplitude;
            }
            else
            {
                pos.y = Mathf.MoveTowards(pos.y, cameraBaseY, Time.deltaTime);
            }
            viewCamera.transform.localPosition = pos;
        }
    }
}
