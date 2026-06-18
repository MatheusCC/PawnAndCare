using UnityEngine;
using UnityEngine.UI;
using PawsAndCare.Core;
using PawsAndCare.Interaction;

namespace PawsAndCare.Services
{
    /// <summary>
    /// A station where a Worker performs a service. Configuration (service type, name,
    /// duration, price) is sourced from the assigned ServiceData. Tracks runtime
    /// occupancy and physical condition. Self-registers with StationManager.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ServiceStation : MonoBehaviour, IInteractable
    {
        [SerializeField]
        [Tooltip("The service this station performs. Drives ServiceType matching, duration, price, and display name.")]
        private ServiceData serviceData = null;

        [SerializeField]
        [Tooltip("World-space anchor where the Worker should stand to perform the service.")]
        private Transform workerAnchor = null;

        [SerializeField]
        [Tooltip("World-space anchor where the customer pet should stand/sit to receive the service.")]
        private Transform customerAnchor = null;

        [SerializeField]
        private bool isOccupied = false;

        [SerializeField]
        [Range(0.0f, 100.0f)]
        [Tooltip("Physical condition of the station (0–100). Degrades with use; affects service quality.")]
        private float condition = 100.0f;

        [SerializeField]
        [Tooltip("IInteractable type discriminator. Defaults to STATION for ServiceStation.")]
        private InteractableType interactableType = InteractableType.STATION;

        [SerializeField]
        [Tooltip("Outline material applied as an additional sub-material on hover. Cleared on hover exit.")]
        private Material outlineMaterial = null;

        [SerializeField]
        [Tooltip("Optional. Root object of the world-space service progress bar (e.g. the Canvas). Toggled with the service.")]
        private GameObject progressBarRoot = null;

        [SerializeField]
        [Tooltip("Optional. Filled Image whose fillAmount goes 0 (empty) to 1 (full) as the service progresses. Set its Image Type to Filled.")]
        private Image progressFill = null;

        private MeshRenderer targetRenderer = null;

        // Two cached material arrays so hover/exit just swaps a reference instead of allocating each time.
        // defaultMaterials = the renderer's authored materials at boot.
        // highlightedMaterials = defaultMaterials with outlineMaterial appended at the end.
        private Material[] defaultMaterials = null;
        private Material[] highlightedMaterials = null;

        public ServiceData Data
        {
            get { return serviceData; }
        }

        public Transform WorkerAnchor
        {
            get { return workerAnchor; }
        }

        public Transform CustomerAnchor
        {
            get { return customerAnchor; }
        }

        public bool IsOccupied
        {
            get { return isOccupied; }
        }

        public float Condition
        {
            get { return condition; }
        }

        public InteractableType Type
        {
            get { return interactableType; }
        }

        // Build the cached material arrays once at boot. defaultMaterials snapshots the renderer's
        // authored materials; highlightedMaterials is the same list with outlineMaterial appended.
        // Hover toggling becomes a single sharedMaterials assignment instead of per-frame array work.
        private void Awake()
        {
            targetRenderer = GetComponent<MeshRenderer>();

            if (targetRenderer != null)
            {
                BuildHighlightMaterials();
            }
            else
            {
                Debug.LogWarning("[ServiceStation] No MeshRenderer found on this GameObject — hover highlight will not work.", this);
            }
        }

        // Snapshots the renderer's authored materials (defaultMaterials) and the same list with
        // outlineMaterial appended (highlightedMaterials), so hover toggling is a single
        // sharedMaterials assignment instead of per-frame array work.
        private void BuildHighlightMaterials()
        {
            if (outlineMaterial != null)
            {
                defaultMaterials = targetRenderer.sharedMaterials;

                highlightedMaterials = new Material[defaultMaterials.Length + 1];

                for (int i = 0; i < defaultMaterials.Length; i++)
                {
                    highlightedMaterials[i] = defaultMaterials[i];
                }

                highlightedMaterials[defaultMaterials.Length] = outlineMaterial;
            }
            else
            {
                Debug.LogWarning("[ServiceStation] outlineMaterial is missing — hover highlight will not work. Assign it in the inspector.", this);
            }
        }

        // Start (not Awake): StationManager is a Singleton initialised in its own Awake. Using Start
        // here means every singleton's Awake has already run, so StationManager.Instance is non-null
        // regardless of which order Unity wakes the GameObjects.
        private void Start()
        {
            if (workerAnchor == null)
            {
                Debug.LogWarning("[ServiceStation] workerAnchor is missing — Workers will have no stand position. Assign one in the inspector.", this);
            }

            if (customerAnchor == null)
            {
                Debug.LogWarning("[ServiceStation] customerAnchor is missing — pets will have no stand position. Assign one in the inspector.", this);
            }

            if (serviceData != null)
            {
                if (StationManager.Instance != null)
                {
                    StationManager.Instance.Register(this);
                }
                else
                {
                    Debug.LogWarning("[ServiceStation] StationManager.Instance is null at Start — station will not be registered.", this);
                }
            }
            else
            {
                Debug.LogError("[ServiceStation] ServiceData is missing — assign one in the inspector. Station will not be registered.", this);
            }
        }

        private void OnDestroy()
        {
            if (StationManager.Instance != null)
            {
                StationManager.Instance.Unregister(this);
            }
        }

        /// <summary>
        /// Returns true if this station can perform the given service type.
        /// </summary>
        public bool Supports(ServiceType type)
        {
            bool supported = false;

            if (serviceData != null)
            {
                supported = serviceData.ServiceType == type;
            }

            return supported;
        }

        public bool CanInteract()
        {
            return !isOccupied && serviceData != null;
        }

        /// <summary>
        /// Sets occupancy. Owned by WorkerServiceRunner: reserved (true) when a worker is dispatched
        /// here, released (false) on cancellation or service completion. Drives CanInteract/availability.
        /// </summary>
        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;
        }

        /// <summary>
        /// Shows the progress bar (if assigned) and sets its fill to the given normalized value.
        /// </summary>
        public void ShowServiceProgress(float normalized)
        {
            if (progressBarRoot != null)
            {
                progressBarRoot.SetActive(true);
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(normalized);
            }
        }

        /// <summary>
        /// Hides the progress bar (if assigned).
        /// </summary>
        public void HideServiceProgress()
        {
            if (progressBarRoot != null)
            {
                progressBarRoot.SetActive(false);
            }
        }

        public void OnHoverEnter()
        {
            if (targetRenderer != null && highlightedMaterials != null)
            {
                targetRenderer.sharedMaterials = highlightedMaterials;
            }
        }

        public void OnHoverExit()
        {
            if (targetRenderer != null && defaultMaterials != null)
            {
                targetRenderer.sharedMaterials = defaultMaterials;
            }
        }

        // Reachable only when CanInteract() is true (the interaction layer gates it), so a selected
        // station is always a valid service target. Broadcast it; player-control systems decide what to do.
        public void OnSelect()
        {
            EventBus.Publish(new StationSelectedEvent(this));
        }

        public void OnDeselect()
        {
        }
    }
}
