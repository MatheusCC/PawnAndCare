using UnityEngine;
using UnityEngine.InputSystem;
using PawsAndCare.Core;
using PawsAndCare.Input;

namespace PawsAndCare.Interaction
{
    /// <summary>
    /// Manages interactive entity hover detection and selection. Per-frame raycasts detect hover state
    /// changes and fire OnHoverEnter/OnHoverExit callbacks on IInteractable. Clicks route to
    /// IInteractable.OnSelect() if applicable. Filters interactions based on the current InteractionMode.
    /// </summary>
    public class InteractionManager : Singleton<InteractionManager>
    {
        [SerializeField]
        private UnityEngine.Camera mainCamera = null;

        private CameraInputActions input = null;
        private InteractionMode currentMode = InteractionMode.NORMAL;
        private IInteractable hoveredInteractable = null;

        /// <summary>
        /// The current interaction mode. Controls whether hover/click routing to IInteractable is allowed.
        /// </summary>
        public InteractionMode CurrentMode
        {
            get { return currentMode; }
        }

        // InteractionManager coexists with AgentController — both subscribe to Click.performed. This is
        // intentional: AgentController handles Worker selection/movement, InteractionManager handles
        // IInteractable.OnSelect. Workers are not IInteractable (today), so the two paths don't conflict.
        // If a Worker ever needs to become IInteractable, the click-routing rules will need to be revisited.
        protected override void OnInitialize()
        {
            input = new CameraInputActions();
            input.Camera.Click.performed += OnClick;
            input.Camera.Enable();
        }

        // Override (not hide) Singleton.OnDestroy so the base class can still clear the static Instance.
        // Forgetting to call base.OnDestroy() would leave a dangling reference across play sessions when
        // domain reload is disabled — silently breaking the next session's singleton lookup.
        protected override void OnDestroy()
        {
            if (input != null)
            {
                input.Camera.Click.performed -= OnClick;
                input.Dispose();
            }

            base.OnDestroy();
        }

        private void Update()
        {
            UpdateHoverState();
        }

        // Per-frame hover detection: raycast from pointer, compare result to previous frame's hovered entity,
        // and fire enter/exit callbacks if the state changes. Keeps hover tracking independent of click timing.
        private void UpdateHoverState()
        {
            IInteractable newHoveredInteractable = GetHoveredInteractable();

            if (newHoveredInteractable != hoveredInteractable)
            {
                if (hoveredInteractable != null)
                {
                    hoveredInteractable.OnHoverExit();
                }

                hoveredInteractable = newHoveredInteractable;

                if (hoveredInteractable != null)
                {
                    hoveredInteractable.OnHoverEnter();
                }
            }
        }

        // Raycast from the current pointer position and return the first IInteractable that passes its own
        // CanInteract() check. Returns null if no interactable is under the pointer or if mode blocks interaction.
        private IInteractable GetHoveredInteractable()
        {
            IInteractable result = null;

            if (mainCamera != null && IsInteractionAllowedInCurrentMode())
            {
                Vector2 pointerPos = input.Camera.PointerPosition.ReadValue<Vector2>();
                Ray ray = mainCamera.ScreenPointToRay(pointerPos);

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                    if (interactable != null && interactable.CanInteract())
                    {
                        result = interactable;
                    }
                }
            }

            return result;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (mainCamera != null)
            {
                HandleClick();
            }
            else
            {
                Debug.LogWarning("[InteractionManager] Camera reference is missing — assign one in the inspector.", this);
            }
        }

        // Route clicks to the currently hovered IInteractable. Re-check CanInteract because the entity's
        // state can change between hover-set and click (e.g., a station becoming occupied mid-hover).
        private void HandleClick()
        {
            if (hoveredInteractable != null && hoveredInteractable.CanInteract())
            {
                hoveredInteractable.OnSelect();
            }
        }

        /// <summary>
        /// Sets the current interaction mode. Modes other than NORMAL block hover/click routing to interactables.
        /// </summary>
        public void SetInteractionMode(InteractionMode mode)
        {
            currentMode = mode;
        }

        // Only NORMAL mode allows IInteractable routing. ASSIGNING and BUILD_MODE will gate interactions later
        // (e.g., during build mode, hovering a station should not fire its highlight — the builder UI owns the pointer).
        private bool IsInteractionAllowedInCurrentMode()
        {
            bool allowed = false;

            if (currentMode == InteractionMode.NORMAL)
            {
                allowed = true;
            }

            return allowed;
        }
    }
}
