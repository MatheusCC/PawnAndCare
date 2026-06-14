using UnityEngine;
using UnityEngine.AI;
using PawsAndCare.Services;

namespace PawsAndCare.Workers
{
    /// <summary>
    /// Player-controllable worker with NavMeshAgent-based pathfinding and movement.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Worker : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Visual shown under the worker when selected by the player. Disabled by default.")]
        private GameObject selectionIndicator = null;

        [SerializeField]
        [Tooltip("Defines this worker's role, move speed, and per-service skill ratings.")]
        private WorkerData workerData = null;

        private NavMeshAgent navMeshAgent = null;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();

            if (workerData != null)
            {
                navMeshAgent.speed = workerData.BaseMoveSpeed;
            }
            else
            {
                Debug.LogWarning("[Worker] workerData is missing — using prefab default speed and zero skill ratings. Assign one in the inspector.", this);
            }

            // Force the indicator off at boot so prefab-authoring mistakes (left enabled in editor)
            // don't produce a "selected" worker on spawn.
            SetSelectionIndicatorActive(false);
        }

        /// <summary>
        /// Returns this worker's skill rating [0,1] for the given service type, or 0 if no data is assigned.
        /// </summary>
        public float GetSkillRating(ServiceType serviceType)
        {
            float rating = 0.0f;

            if (workerData != null)
            {
                rating = workerData.GetSkillRating(serviceType);
            }

            return rating;
        }

        /// <summary>
        /// Toggles the selection indicator visual. Called by AgentController on selection change.
        /// </summary>
        public void SetSelectionIndicatorActive(bool selected)
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(selected);
            }
            else
            {
                Debug.LogWarning("[Worker] selectionIndicator is missing — assign one in the inspector.", this);
            }
        }

        /// <summary>
        /// Commands the worker to move to a world position using NavMesh pathfinding.
        /// </summary>
        public void MoveTo(Vector3 destination)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(destination);
            }
        }

        /// <summary>
        /// Returns true only when the worker has actually arrived at its destination.
        /// </summary>
        public bool HasReachedDestination()
        {
            bool reached = false;

            if (navMeshAgent != null)
            {
                if (navMeshAgent.isOnNavMesh && navMeshAgent.hasPath)
                {
                    // Agent stops when within its stoppingDistance — exact arrival is impossible
                    // because of agent radius and float-precision drift in the final approach.
                    reached = navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance;
                }
            }

            return reached;
        }
    }
}
