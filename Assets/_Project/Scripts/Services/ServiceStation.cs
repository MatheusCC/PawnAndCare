using UnityEngine;
using UnityEngine.UI;
using PawsAndCare.Core;
using PawsAndCare.Interaction;

namespace PawsAndCare.Services
{
    // A station where a Worker performs a service. Configuration (service type, name,
    // duration, price) is sourced from the assigned ServiceData. Tracks runtime
    // occupancy and physical condition. Self-registers with StationManager.
    public class ServiceStation : MonoBehaviour, IInteractable
    {
        [SerializeField]
        [Tooltip("The service this station performs. Drives ServiceType matching, duration, price, and display name.")]
        private ServiceData serviceData = null;

        [SerializeField]
        [Tooltip("World-space anchor where the Worker should stand to perform the service.")]
        private Transform workerAnchor = null;

        [SerializeField]
        [Tooltip("Seat on top of the station. The pet walks up beside it, then is placed here (position + rotation) for the service.")]
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

        private Renderer[] targetRenderers;

        // Per-renderer cached material arrays, index-aligned with targetRenderers, so hover/exit just
        // swaps references instead of allocating each time.
        // defaultMaterials[i] = renderer i's authored materials at boot.
        // highlightedMaterials[i] = defaultMaterials[i] with outlineMaterial appended at the end.
        private Material[][] defaultMaterials;
        private Material[][] highlightedMaterials;

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

        // Collects every renderer in the station's hierarchy (the station is built from child meshes)
        // and builds the cached hover material arrays once at boot.
        private void Awake()
        {
            targetRenderers = GetComponentsInChildren<Renderer>();

            if (targetRenderers.Length > 0)
            {
                BuildHighlightMaterials();
            }
            else
            {
                Debug.LogWarning("[ServiceStation] No Renderer found on this station or its children — hover highlight will not work.", this);
            }
        }

        // Snapshots each renderer's authored materials and a copy with outlineMaterial appended.
        private void BuildHighlightMaterials()
        {
            if (outlineMaterial != null)
            {
                defaultMaterials = new Material[targetRenderers.Length][];
                highlightedMaterials = new Material[targetRenderers.Length][];

                for (int i = 0; i < targetRenderers.Length; i++)
                {
                    defaultMaterials[i] = targetRenderers[i].sharedMaterials;
                    highlightedMaterials[i] = AppendOutline(defaultMaterials[i]);
                }
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

        // Returns true if this station can perform the given service type.
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

        // Sets occupancy. Owned by WorkerServiceRunner: reserved (true) when a worker is dispatched
        // here, released (false) on cancellation or service completion. Drives CanInteract/availability.
        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;
        }

        // Shows the progress bar (if assigned) and sets its fill to the given normalized value.
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

        // Hides the progress bar (if assigned).
        public void HideServiceProgress()
        {
            if (progressBarRoot != null)
            {
                progressBarRoot.SetActive(false);
            }
        }

        private Material[] AppendOutline(Material[] materials)
        {
            Material[] result = new Material[materials.Length + 1];

            for (int i = 0; i < materials.Length; i++)
            {
                result[i] = materials[i];
            }

            result[materials.Length] = outlineMaterial;

            return result;
        }

        // Assigns each renderer its cached material set. No-op if the sets were never built.
        private void ApplyMaterials(Material[][] materialSets)
        {
            if (materialSets != null)
            {
                for (int i = 0; i < targetRenderers.Length; i++)
                {
                    targetRenderers[i].sharedMaterials = materialSets[i];
                }
            }
        }

        public void OnHoverEnter()
        {
            ApplyMaterials(highlightedMaterials);
        }

        public void OnHoverExit()
        {
            ApplyMaterials(defaultMaterials);
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
