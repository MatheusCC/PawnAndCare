using UnityEngine;
using UnityEngine.InputSystem;
using PawsAndCare.Input;
using PawsAndCare.Workers;

namespace PawsAndCare.Player
{
    // Scene-level player input router. Left-click on a Worker selects it (visual only).
    // ServiceDispatcher drives workers to stations, so this class issues no move or
    // station-assign orders — it only tracks the current selection.
    public class AgentController : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.Camera mainCamera = null;

        private CameraInputActions input;
        private Worker selectedWorker;

        private void Awake()
        {
            // Instantiate the generated input wrapper and subscribe to the Click action.
            // Click fires once on press (Button type), matching the "select" gesture.
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
                    SelectWorker(clickedWorker);
                }
            }
        }

        // Selection caches the worker, so re-selecting the already-selected worker is a no-op.
        private void SelectWorker(Worker worker)
        {
            if (selectedWorker != worker)
            {
                if (selectedWorker != null)
                {
                    selectedWorker.SetSelectionIndicatorActive(false);
                }

                selectedWorker = worker;
                selectedWorker.SetSelectionIndicatorActive(true);
            }
        }
    }
}
