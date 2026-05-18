using UnityEngine;

namespace PawsAndCare.Services
{
    /// <summary>
    /// A station where a Worker performs a service. Configuration (service type, name,
    /// duration, price) is sourced from the assigned ServiceDefinition. Tracks runtime
    /// occupancy and physical condition. Self-registers with StationManager.
    /// </summary>
    public class ServiceStation : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The service this station performs. Drives ServiceType matching, duration, price, and display name.")]
        private ServiceDefinition serviceDefinition = null;

        [SerializeField]
        [Tooltip("World-space anchor where the Worker should stand to perform the service.")]
        private Transform workerAnchor = null;

        [SerializeField]
        private bool isOccupied = false;

        [SerializeField]
        [Range(0.0f, 100.0f)]
        [Tooltip("Physical condition of the station (0–100). Degrades with use; affects service quality.")]
        private float condition = 100.0f;

        // TODO Task 12: add `private ServiceSession currentSession = null;` once ServiceSession exists.
        // TODO Task 10: implement IInteractable (OnHoverEnter / OnHoverExit / OnSelect / OnDeselect / CanInteract).

        public ServiceDefinition Definition
        {
            get { return serviceDefinition; }
        }

        public Transform WorkerAnchor
        {
            get { return workerAnchor; }
        }

        public bool IsOccupied
        {
            get { return isOccupied; }
        }

        public float Condition
        {
            get { return condition; }
        }

        // Start (not Awake): StationManager is a Singleton initialised in its own Awake. Using Start
        // here means every singleton's Awake has already run, so StationManager.Instance is non-null
        // regardless of which order Unity wakes the GameObjects.
        private void Start()
        {
            if (serviceDefinition != null)
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
                Debug.LogError("[ServiceStation] ServiceDefinition is missing — assign one in the inspector. Station will not be registered.", this);
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

            if (serviceDefinition != null)
            {
                supported = serviceDefinition.ServiceType == type;
            }

            return supported;
        }
    }
}
