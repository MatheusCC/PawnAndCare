using UnityEngine;
using UnityEngine.InputSystem;
using PawsAndCare.Core;
using PawsAndCare.Input;
using PawsAndCare.Interaction;
using PawsAndCare.Services;
using PawsAndCare.Workers;

namespace PawsAndCare.Player
{
    /// <summary>
    /// Scene-level player input router. Left-click on a Worker selects it; left-click on the floor
    /// commands the selected Worker to move there. Station clicks are owned by the interaction layer
    /// and reach this router as a StationSelectedEvent, which dispatches the worker to that station.
    /// All worker behavior runs through WorkerServiceRunner — this class only routes intent.
    /// </summary>
    public class AgentController : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.Camera mainCamera = null;

        private CameraInputActions input;
        private Worker selectedWorker;
        private WorkerServiceRunner selectedRunner;

        private void Awake()
        {
            // Instantiate the generated input wrapper and subscribe to the Click action.
            // Click fires once on press (Button type), matching the "issue an order" gesture.
            input = new CameraInputActions();
            input.Camera.Click.performed += OnClick;
            EventBus.Subscribe<StationSelectedEvent>(OnStationSelected);
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
            EventBus.Unsubscribe<StationSelectedEvent>(OnStationSelected);
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
                else if (hit.collider.GetComponentInParent<IInteractable>() == null && selectedRunner != null)
                {
                    // Floor (non-interactable) click with a worker selected → move order.
                    // Interactable clicks (stations) are owned by the interaction layer via
                    // StationSelectedEvent, so they must not also trigger a raw move here.
                    selectedRunner.RequestMove(hit.point);
                }
            }
        }

        // Selection caches the worker and its service runner once, so per-click routing never
        // re-runs GetComponent. Re-selecting the already-selected worker is a no-op.
        private void SelectWorker(Worker worker)
        {
            if (selectedWorker != worker)
            {
                if (selectedWorker != null)
                {
                    selectedWorker.SetSelectionIndicatorActive(false);
                }

                selectedWorker = worker;
                selectedRunner = worker.GetComponent<WorkerServiceRunner>();
                selectedWorker.SetSelectionIndicatorActive(true);

                if (selectedRunner == null)
                {
                    Debug.LogError("[AgentController] Selected worker has no WorkerServiceRunner — it cannot perform services.", this);
                }
            }
        }

        // A station was selected through the interaction layer. If a worker is selected,
        // dispatch it to walk to that station and perform its service.
        private void OnStationSelected(StationSelectedEvent eventData)
        {
            if (selectedRunner != null)
            {
                selectedRunner.AssignStation(eventData.Station);
            }
        }
    }
}
