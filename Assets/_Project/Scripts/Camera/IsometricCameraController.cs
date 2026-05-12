using UnityEngine;
using UnityEngine.InputSystem;
using PawsAndCare.Input;

namespace PawsAndCare.Camera
{
    /// <summary>
    /// Drives the isometric camera rig: WASD/arrow keyboard pan, middle-mouse drag pan,
    /// scroll-wheel zoom, and Q/E 90° snap rotation. All motion is smoothed via SmoothDamp.
    ///
    /// Design: "target-and-smooth" pattern. Input updates target values
    /// (targetRigPosition / targetZoom / targetYRotation); each frame the actual transform
    /// eases toward them via SmoothDamp. This decouples "what the player wants" from
    /// "how it animates" — input handling stays simple while motion stays buttery.
    ///
    /// Note: this namespace shadows UnityEngine.Camera. Whenever you need the Camera
    /// component type, fully qualify it as UnityEngine.Camera (as below).
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        // Hierarchy references — assigned in inspector.
        // Rig holds world position (panning moves this).
        // Pivot holds the Y rotation (Q/E spins this).
        // MainCamera holds orthographic size (scrolling adjusts this) and a fixed local pitch.
        [SerializeField]
        private Transform cameraRig;
        [SerializeField]
        private Transform cameraPivot;
        [SerializeField]
        private UnityEngine.Camera mainCamera;

        // Pan tuning.
        // panSpeed is world units per second at the reference zoom (panZoomScalingRef).
        // At other zoom levels, pan speed scales linearly with orthographicSize so the
        // perceived screen-speed stays roughly constant.
        [SerializeField]
        private float panSpeed = 10f;
        [SerializeField]
        private float panZoomScalingRef = 10f;
        [SerializeField]
        private float panSmoothTime = 0.1f;

        // Zoom tuning. zoomSpeed default is small because the new Input System reports
        // raw mouse-scroll deltas (often ~120 per notch on Windows); tune in inspector.
        [SerializeField]
        private float zoomSpeed = 0.05f;
        [SerializeField]
        private float zoomMin = 3f;
        [SerializeField]
        private float zoomMax = 20f;
        [SerializeField]
        private float zoomSmoothTime = 0.1f;

        // Rotation tuning — slightly longer smooth time so a 90° turn reads visually.
        [SerializeField]
        private float rotationSmoothTime = 0.2f;

        public Transform CameraRig { get { return cameraRig; } }
        public Transform CameraPivot { get { return cameraPivot; } }
        public UnityEngine.Camera MainCamera { get { return mainCamera; } }

        // The generated Input System wrapper class (from CameraInput.inputactions).
        private CameraInputActions input;

        // Target state — what the player has asked for. SmoothDamp eases toward these.
        private Vector3 targetRigPosition;
        private float targetZoom;
        private float targetYRotation;

        // The current animated Y rotation (kept separately so we don't read back from
        // transform.eulerAngles, which can flip representations across the 0/360 wrap).
        private float currentYRotation;

        // SmoothDamp internal velocity buffers (it stores acceleration state in these refs).
        private Vector3 panVelocity;
        private float zoomVelocity;
        private float rotationVelocity;

        // Middle-mouse drag state.
        private bool isDragging;
        private Vector2 lastPointerPosition;

        /// <summary>
        /// Instantiates the generated InputActions wrapper. The wrapper holds all
        /// action maps + bindings defined in CameraInput.inputactions.
        /// </summary>
        private void Awake()
        {
            input = new CameraInputActions();
        }

        /// <summary>
        /// Activates the Camera action map and subscribes to discrete (event-driven)
        /// actions: rotation taps and drag start/stop. Continuous actions (Pan, Zoom,
        /// PointerPosition) are read each frame in Update, so they don't need event hooks.
        /// </summary>
        private void OnEnable()
        {
            input.Camera.Enable();
            input.Camera.RotateLeft.performed += OnRotateLeft;
            input.Camera.RotateRight.performed += OnRotateRight;
            input.Camera.Drag.started += OnDragStarted;
            input.Camera.Drag.canceled += OnDragCanceled;
        }

        /// <summary>
        /// Unsubscribes and disables the action map. Mirrors OnEnable exactly to avoid
        /// dangling handlers when the component is disabled or destroyed.
        /// </summary>
        private void OnDisable()
        {
            input.Camera.RotateLeft.performed -= OnRotateLeft;
            input.Camera.RotateRight.performed -= OnRotateRight;
            input.Camera.Drag.started -= OnDragStarted;
            input.Camera.Drag.canceled -= OnDragCanceled;
            input.Camera.Disable();
        }

        /// <summary>
        /// Snapshots the initial state into the target fields so the first frame's
        /// SmoothDamp call has nothing to ease toward (no surprise jolt at startup).
        /// Runs in Start (after all Awake calls) to guarantee the inspector references
        /// are wired up.
        /// </summary>
        private void Start()
        {
            targetRigPosition = cameraRig.position;
            targetZoom = mainCamera.orthographicSize;
            currentYRotation = cameraPivot.localEulerAngles.y;
            targetYRotation = currentYRotation;
        }

        /// <summary>
        /// Frame order: read input (which updates targets), then smooth current state
        /// toward targets. Keeping this split makes the "what player wants" vs "what
        /// the camera is doing now" distinction obvious.
        /// </summary>
        private void Update()
        {
            HandleKeyboardPan();
            HandleMouseDrag();
            HandleZoom();
            ApplySmoothing();
        }

        /// <summary>
        /// Reads the WASD/arrows 2D vector and adds a delta to targetRigPosition.
        /// The input is camera-relative (W = "forward in view"), so we rotate it by
        /// the pivot's target Y rotation before applying — that way W always moves
        /// the rig in the direction the camera is facing, even mid-rotation animation.
        /// Pan magnitude scales with zoom so movement feels consistent on-screen at
        /// any zoom level.
        /// </summary>
        private void HandleKeyboardPan()
        {
            Vector2 panInput = input.Camera.Pan.ReadValue<Vector2>();

            if (panInput.sqrMagnitude > 0.0001f)
            {
                // Build a local-space direction. Input.x = right/left, input.y = forward/back.
                Vector3 localDirection = new Vector3(panInput.x, 0f, panInput.y);

                // Rotate by the pivot's TARGET rotation (not current) so panning direction
                // stays stable during the rotation tween. Using current would jitter.
                Quaternion pivotRotation = Quaternion.Euler(0f, targetYRotation, 0f);
                Vector3 worldDirection = pivotRotation * localDirection;

                // Scale pan speed with zoom: zoomed out = larger world steps per second.
                // This keeps "pixels moved per second" roughly constant on-screen.
                float zoomScale = mainCamera.orthographicSize / panZoomScalingRef;
                targetRigPosition += worldDirection * panSpeed * zoomScale * Time.deltaTime;
            }
        }

        /// <summary>
        /// While middle-mouse is held, translates the rig so the world "drags" with the
        /// cursor. We compute the per-frame pixel delta of the cursor, convert it to
        /// world units using the orthographic size, negate (camera moves opposite to
        /// cursor to make the world appear to follow), and rotate into world space.
        /// </summary>
        private void HandleMouseDrag()
        {
            if (isDragging)
            {
                Vector2 pointerPos = input.Camera.PointerPosition.ReadValue<Vector2>();
                Vector2 screenDelta = pointerPos - lastPointerPosition;
                lastPointerPosition = pointerPos;

                if (screenDelta.sqrMagnitude > 0.0001f)
                {
                    // For an orthographic camera, the full vertical world height is
                    // (2 * orthographicSize). Divide by screen height to get world
                    // units per pixel. Horizontal works out to the same ratio because
                    // Unity orthographic uses the screen aspect ratio symmetrically.
                    float worldPerPixel = (mainCamera.orthographicSize * 2f) / Screen.height;

                    // Negate: cursor moves right → camera moves left so world appears
                    // dragged right. Build delta in local (camera-relative) space first,
                    // then rotate to world space by the pivot's Y rotation.
                    Vector3 localDelta = new Vector3(-screenDelta.x * worldPerPixel, 0f, -screenDelta.y * worldPerPixel);
                    Quaternion pivotRotation = Quaternion.Euler(0f, targetYRotation, 0f);
                    Vector3 worldDelta = pivotRotation * localDelta;
                    targetRigPosition += worldDelta;
                }
            }
        }

        /// <summary>
        /// Adjusts targetZoom by the scroll input. Subtraction inverts the sign so
        /// scrolling UP (positive) zooms IN (smaller ortho size). Clamps to [min, max]
        /// so we never invert or zoom out to infinity.
        /// </summary>
        private void HandleZoom()
        {
            float scrollInput = input.Camera.Zoom.ReadValue<float>();

            if (Mathf.Abs(scrollInput) > 0.0001f)
            {
                targetZoom = Mathf.Clamp(targetZoom - scrollInput * zoomSpeed, zoomMin, zoomMax);
            }
        }

        /// <summary>
        /// Eases the actual transform values toward their targets each frame.
        /// SmoothDamp is acceleration-based — it accelerates toward the target and
        /// decelerates as it nears, giving a natural-feeling motion curve. The 'velocity'
        /// fields are reference parameters SmoothDamp uses to store its internal state
        /// between calls; never touch them directly.
        /// Rotation uses SmoothDampAngle (a wrap-aware float damp) and writes into
        /// localRotation so the cameraRig's own rotation, if any, stays independent.
        /// </summary>
        private void ApplySmoothing()
        {
            cameraRig.position = Vector3.SmoothDamp(cameraRig.position, targetRigPosition, 
                                                    ref panVelocity, panSmoothTime);

            mainCamera.orthographicSize = Mathf.SmoothDamp(mainCamera.orthographicSize, targetZoom,
                                                            ref zoomVelocity, zoomSmoothTime);

            // SmoothDampAngle handles the 360°/0° wrap correctly so going from 350° to
            // 20° eases through 360°/0° (the short way), not the long way around.
            currentYRotation = Mathf.SmoothDampAngle(currentYRotation, targetYRotation, 
                                                        ref rotationVelocity, rotationSmoothTime);
            cameraPivot.localRotation = Quaternion.Euler(0f, currentYRotation, 0f);
        }

        /// <summary>
        /// Q pressed → world rotates left from the player's view (camera spins
        /// clockwise around Y, which makes the world appear to swing left).
        /// </summary>
        private void OnRotateLeft(InputAction.CallbackContext ctx)
        {
            targetYRotation += 90f;
        }

        /// <summary>
        /// E pressed → world rotates right from the player's view.
        /// </summary>
        private void OnRotateRight(InputAction.CallbackContext ctx)
        {
            targetYRotation -= 90f;
        }

        /// <summary>
        /// Middle-mouse pressed → snapshot the cursor position so the next frame's
        /// HandleMouseDrag has a delta baseline to work against.
        /// </summary>
        private void OnDragStarted(InputAction.CallbackContext ctx)
        {
            lastPointerPosition = input.Camera.PointerPosition.ReadValue<Vector2>();
            isDragging = true;
        }

        /// <summary>
        /// Middle-mouse released → stop applying drag deltas in HandleMouseDrag.
        /// </summary>
        private void OnDragCanceled(InputAction.CallbackContext ctx)
        {
            isDragging = false;
        }
    }
}