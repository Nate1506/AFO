using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kao
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        public float waterWalkSpeed = 2.6f;
        public float acceleration = 12f;
        public float rotationSpeed = 15f;

        [Header("Jump & Gravity")]
        public float jumpHeight = 1.8f;
        public float gravity = 22f;
        public float coyoteTime = 0.15f;
        public float jumpBufferTime = 0.15f;

        [Header("Camera & View")]
        public Transform cameraPivot;
        public Camera playerCamera;
        public float mouseSensitivity = 1.5f;
        public float minPitch = -70f;
        public float maxPitch = 75f;
        public float thirdPersonDistance = 3.2f;
        public float minDistance = 0.4f;
        public float maxDistance = 6.0f;
        public LayerMask cameraCollisionLayers = ~0; // All layers by default
        public bool isFirstPerson = false;

        [Header("Visuals & Equipment")]
        public GameObject visualRoot;
        public Light flashlight;
        public Transform safeZoneTarget;
        public float floodWaterLevel = 0.5f;

        // Private components & state
        private CharacterController controller;
        private Vector3 velocity;
        private Vector3 currentMoveVelocity;
        private float pitch = 15f;
        private float currentDistance;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private bool isGrounded;
        private bool isInWater;
        private bool isSprinting;
        private bool isCursorLocked = true;

        // Procedural animation
        private Vector3 visualInitialScale = Vector3.one;
        private Vector3 visualInitialPos = Vector3.zero;
        private float walkCycle = 0f;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentDistance = isFirstPerson ? 0f : thirdPersonDistance;

            if (visualRoot != null)
            {
                visualInitialScale = visualRoot.transform.localScale;
                visualInitialPos = visualRoot.transform.localPosition;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            // Exclude Player and Ignore Raycast from camera collision
            cameraCollisionLayers &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            if (gameObject.layer != 0)
            {
                cameraCollisionLayers &= ~(1 << gameObject.layer);
            }

            // Find flood water if not set
            var waterObj = GameObject.Find("FloodWater");
            if (waterObj != null)
            {
                floodWaterLevel = waterObj.transform.position.y + 0.3f;
            }

            // Find Temple / Safe Zone
            if (safeZoneTarget == null)
            {
                var temple = GameObject.Find("Temple_SafeZone");
                if (temple != null) safeZoneTarget = temple.transform;
            }
        }

        private void Start()
        {
            SetCursorLock(true);
        }

        private void Update()
        {
            HandleCursor();
            HandleInput();
            HandleMouseLook();
            HandleMovement();
            HandleJumpAndGravity();
            HandleCameraPosition();
            HandleProceduralAnimation();
        }

        private void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleCursor()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLock(!isCursorLocked);
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isCursorLocked)
            {
                SetCursorLock(true);
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetCursorLock(!isCursorLocked);
            }
            if (Input.GetMouseButtonDown(0) && !isCursorLocked)
            {
                SetCursorLock(true);
            }
#endif
        }

        private void HandleInput()
        {
            // Toggle Flashlight (F)
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                ToggleFlashlight();
            }
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                TogglePerspective();
            }
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    currentDistance = Mathf.Clamp(currentDistance - Mathf.Sign(scroll) * 0.6f, 0f, maxDistance);
                    isFirstPerson = currentDistance < 0.2f;
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleFlashlight();
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                TogglePerspective();
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistance = Mathf.Clamp(currentDistance - Mathf.Sign(scroll) * 0.6f, 0f, maxDistance);
                isFirstPerson = currentDistance < 0.2f;
            }
#endif
        }

        public void ToggleFlashlight()
        {
            if (flashlight != null)
            {
                flashlight.enabled = !flashlight.enabled;
            }
        }

        public void TogglePerspective()
        {
            isFirstPerson = !isFirstPerson;
            currentDistance = isFirstPerson ? 0f : thirdPersonDistance;
        }

        private void HandleMouseLook()
        {
            if (!isCursorLocked) return;

            Vector2 mouseDelta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mouseDelta = Mouse.current.delta.ReadValue() * (mouseSensitivity * 0.1f);
            }
#else
            mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity;
#endif

            // Horizontal rotation turns the character
            transform.Rotate(Vector3.up * mouseDelta.x);

            // Vertical pitch tilts camera pivot
            pitch = Mathf.Clamp(pitch - mouseDelta.y, minPitch, maxPitch);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            isGrounded = controller.isGrounded;
            isInWater = transform.position.y <= floodWaterLevel;

            Vector2 input = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                var kb = Keyboard.current;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;

                isSprinting = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) && input.y > 0.1f && !isInWater;
            }
#else
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            isSprinting = Input.GetKey(KeyCode.LeftShift) && input.y > 0.1f && !isInWater;
#endif
            input = input.normalized;

            // Target speed calculation
            float targetSpeed = walkSpeed;
            if (isInWater)
            {
                targetSpeed = waterWalkSpeed;
            }
            else if (isSprinting)
            {
                targetSpeed = sprintSpeed;
            }

            Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
            Vector3 targetVelocity = moveDirection * targetSpeed;

            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, acceleration * Time.deltaTime);

            Vector3 move = currentMoveVelocity;
            move.y = velocity.y;

            controller.Move(move * Time.deltaTime);
        }

        private void HandleJumpAndGravity()
        {
            // Coyote time
            if (isGrounded)
            {
                coyoteCounter = coyoteTime;
                if (velocity.y < 0f)
                {
                    velocity.y = -2f; // Slight downward push to keep grounded
                }
            }
            else
            {
                coyoteCounter -= Time.deltaTime;
            }

            // Jump buffer
            bool jumpPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
            {
                jumpPressed = true;
            }
#endif

            if (jumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            // Execute Jump
            if (jumpBufferCounter > 0f && coyoteCounter > 0f)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
                coyoteCounter = 0f;
                jumpBufferCounter = 0f;

                // Jump stretch effect
                if (visualRoot != null)
                {
                    visualRoot.transform.localScale = new Vector3(visualInitialScale.x * 0.85f, visualInitialScale.y * 1.25f, visualInitialScale.z * 0.85f);
                }
            }

            // Apply gravity
            velocity.y -= gravity * Time.deltaTime;

            // Clamp max fall speed
            if (velocity.y < -30f)
            {
                velocity.y = -30f;
            }
        }

        private void HandleCameraPosition()
        {
            if (playerCamera == null || cameraPivot == null) return;

            // Visibility of visual model in first-person
            if (visualRoot != null)
            {
                bool showModel = !isFirstPerson;
                if (visualRoot.activeSelf != showModel)
                {
                    visualRoot.SetActive(showModel);
                }
            }

            if (isFirstPerson)
            {
                playerCamera.transform.position = cameraPivot.position;
                playerCamera.transform.rotation = cameraPivot.rotation;
            }
            else
            {
                Vector3 targetCamPos = cameraPivot.position - cameraPivot.forward * currentDistance + cameraPivot.up * 0.2f;

                // SphereCast to avoid clipping inside terrain, walls, rocks
                Ray ray = new Ray(cameraPivot.position, -cameraPivot.forward);
                if (Physics.SphereCast(ray, 0.25f, out RaycastHit hit, currentDistance, cameraCollisionLayers, QueryTriggerInteraction.Ignore))
                {
                    float safeDist = Mathf.Max(0.2f, hit.distance - 0.1f);
                    targetCamPos = cameraPivot.position - cameraPivot.forward * safeDist + cameraPivot.up * 0.2f;
                }

                playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetCamPos, 25f * Time.deltaTime);
                playerCamera.transform.rotation = cameraPivot.rotation;
            }
        }

        private void HandleProceduralAnimation()
        {
            if (visualRoot == null) return;

            // Recover squash/stretch scale smoothly
            visualRoot.transform.localScale = Vector3.Lerp(visualRoot.transform.localScale, visualInitialScale, 8f * Time.deltaTime);

            // Walk bobbing and tilt
            float horizontalSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
            if (isGrounded && horizontalSpeed > 0.2f)
            {
                float freq = isSprinting ? 12f : 8f;
                walkCycle += Time.deltaTime * freq;

                float bobOffset = Mathf.Sin(walkCycle) * 0.05f;
                float tiltAngle = Mathf.Sin(walkCycle * 0.5f) * 3f;

                visualRoot.transform.localPosition = visualInitialPos + new Vector3(0f, Mathf.Abs(bobOffset), 0f);
                visualRoot.transform.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);
            }
            else
            {
                walkCycle = 0f;
                visualRoot.transform.localPosition = Vector3.Lerp(visualRoot.transform.localPosition, visualInitialPos, 10f * Time.deltaTime);
                visualRoot.transform.localRotation = Quaternion.Lerp(visualRoot.transform.localRotation, Quaternion.identity, 10f * Time.deltaTime);
            }
        }

        private void OnGUI()
        {
            // Clean, stylish in-game HUD for controls and mission info
            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Bold;

            // Background box for controls helper (bottom-left)
            float boxWidth = 320f;
            float boxHeight = 160f;
            Rect helperRect = new Rect(20, Screen.height - boxHeight - 20, boxWidth, boxHeight);
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(helperRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(helperRect);
            GUILayout.Space(8);
            GUILayout.Label(" <color=#FFD700><b>🎮 ระบบควบคุมตัวละคร (CONTROLS)</b></color>");
            GUILayout.Label(" • <b>[W][A][S][D]</b> เคลื่อนที่ / เดิน");
            GUILayout.Label(" • <b>[Left Shift]</b> วิ่งเร็ว (Sprint)");
            GUILayout.Label(" • <b>[Spacebar]</b> กระโดด (Jump)");
            GUILayout.Label(" • <b>[V / Scroll]</b> สลับมุมมอง (" + (isFirstPerson ? "1st Person" : "3rd Person") + ")");
            GUILayout.Label(" • <b>[F]</b> ไฟฉาย (" + (flashlight != null && flashlight.enabled ? "<color=#00FF7F>เปิด</color>" : "<color=#FF6347>ปิด</color>") + ")");
            GUILayout.Label(" • <b>[Esc]</b> ปลดล็อคเมาส์ (" + (isCursorLocked ? "ล็อคอยู่" : "ปลดล็อค") + ")");
            GUILayout.EndArea();

            // Status bar (top-center)
            float statusWidth = 380f;
            float statusHeight = 70f;
            Rect statusRect = new Rect((Screen.width - statusWidth) * 0.5f, 20, statusWidth, statusHeight);
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(statusRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(statusRect);
            GUILayout.Space(6);
            GUILayout.Label(" <color=#00E5FF><b>🚨 ภารกิจอพยพ: สู่จุดปลอดภัยวัดบนเขา</b></color>");

            string statusMsg = isInWater ? "<color=#FF4500>⚠️ ลุยน้ำท่วม (เคลื่อนที่ช้าลง)</color>" : "<color=#7CFC00>✅ บนพื้นที่แห้ง</color>";
            float dist = safeZoneTarget != null ? Vector3.Distance(transform.position, safeZoneTarget.position) : 0f;

            GUILayout.Label($" • สถานะ: {statusMsg} | ความสูง: {transform.position.y:F1} ม.");
            GUILayout.Label($" • ระยะทางถึงวัดปลอดภัย: <b>{dist:F0} เมตร</b>");
            GUILayout.EndArea();
        }
    }
}
