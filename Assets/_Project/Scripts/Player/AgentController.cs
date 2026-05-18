using UnityEngine;
using UnityEngine.InputSystem;
using PawsAndCare.Input;
using PawsAndCare.Workers;

namespace PawsAndCare.Player
{
    /// <summary>
    /// Scene-level player input router. Left-click on a Worker selects it; left-click elsewhere
    /// commands the currently selected Worker to move to the hit point.
    /// </summary>
    public class AgentController : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.Camera mainCamera = null;

        private CameraInputActions input = null;
        private Worker selectedWorker = null;

        private void Awake()
        {
            // Instantiate the generated input wrapper and subscribe to the Click action.
            // Click fires once on press (Button type), matching the "issue an order" gesture.
            input = new CameraInputActions();
            input.Camera.Click.performed += OnClick;
        }

        private void OnEnable()
        {
            input.Camera.Enable();
        }

        private void OnDisable()
        {
            input.Camera.Disable();
        }

        private void OnDestroy()
        {
            // Unhook and dispose to release native input handles. Without this, leaving Play mode
            // with domain reload disabled leaks subscriptions across sessions.
            input.Camera.Click.performed -= OnClick;
            input.Dispose();
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (mainCamera != null)
            {
                HandleClick();
            }
            else
            {
                Debug.LogWarning("[AgentController] Camera reference is missing — assign one in the inspector.", this);
            }
        }

        private void HandleClick()
        {
            // Read pointer position from the same input asset so we don't mix raw Mouse.current
            // reads with InputAction subscriptions (different timing/code paths).
            Vector2 pointerPos = input.Camera.PointerPosition.ReadValue<Vector2>();
            Ray ray = mainCamera.ScreenPointToRay(pointerPos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                // GetComponentInParent so child colliders/visuals still resolve to the root Worker.
                Worker clickedWorker = hit.collider.GetComponentInParent<Worker>();

                if (clickedWorker != null)
                {
                    // Click landed on a Worker → make it the controlled agent.
                    // Clear the previous selection's indicator first; the new one gets its own indicator.
                    // Skip the toggle when clicking the already-selected worker (no-op selection).
                    if (selectedWorker != clickedWorker)
                    {
                        if (selectedWorker != null)
                        {
                            selectedWorker.SetSelectionIndicatorActive(false);
                        }

                        selectedWorker = clickedWorker;
                        selectedWorker.SetSelectionIndicatorActive(true);
                    }
                }
                else if (selectedWorker != null)
                {
                    // Click landed on the ground (or anything non-Worker) with a Worker
                    // already selected → route the click as a move command.
                    selectedWorker.MoveTo(hit.point);
                }
            }
        }
    }
}
