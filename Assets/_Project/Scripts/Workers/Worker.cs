using System.Collections.Generic;
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

        private NavMeshAgent navMeshAgent;

        // Per-service skill seeded from WorkerData at spawn, then grown at runtime as the worker
        // performs services (StaffDirector). The SO stays the immutable starting template.
        private Dictionary<ServiceType, float> runtimeSkill;

        /// <summary>
        /// Returns this worker's daily salary, or 0 if no data is assigned.
        /// </summary>
        public float GetDailySalary()
        {
            return workerData != null ? workerData.DailySalary : 0.0f;
        }

        /// <summary>
        /// Returns this worker's one-time hire cost, or 0 if no data is assigned.
        /// </summary>
        public float GetHireCost()
        {
            return workerData != null ? workerData.HireCost : 0.0f;
        }

        public WorkerRole Role
        {
            get { return workerData != null ? workerData.Role : WorkerRole.GENERALIST; }
        }

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            runtimeSkill = new Dictionary<ServiceType, float>();

            if (workerData != null)
            {
                navMeshAgent.speed = workerData.BaseMoveSpeed;
                SeedRuntimeSkill();
            }
            else
            {
                Debug.LogWarning("[Worker] workerData is missing — using prefab default speed and zero skill ratings. Assign one in the inspector.", this);
            }

            // Force the indicator off at boot so prefab-authoring mistakes (left enabled in editor)
            // don't produce a "selected" worker on spawn.
            SetSelectionIndicatorActive(false);
        }

        // Copies the starting skill ratings out of the SO into the per-instance runtime store.
        private void SeedRuntimeSkill()
        {
            ServiceType[] types = (ServiceType[])System.Enum.GetValues(typeof(ServiceType));

            for (int i = 0; i < types.Length; i++)
            {
                runtimeSkill[types[i]] = workerData.GetSkillRating(types[i]);
            }
        }

        /// <summary>
        /// Returns this worker's current (runtime) skill rating [0,1] for the given service type, or 0
        /// if none is tracked.
        /// </summary>
        public float GetSkillRating(ServiceType serviceType)
        {
            float rating;
            runtimeSkill.TryGetValue(serviceType, out rating);

            return rating;
        }

        /// <summary>
        /// Improves this worker's skill in a service type by performing it, clamped to [0,1].
        /// Called by StaffDirector on service completion.
        /// </summary>
        public void GainSkill(ServiceType serviceType, float amount)
        {
            float current;
            runtimeSkill.TryGetValue(serviceType, out current);
            runtimeSkill[serviceType] = Mathf.Clamp01(current + amount);
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
            const float ARRIVAL_SPEED_SQR_THRESHOLD = 0.01f;
            bool reached = false;

            // Wait for the path to finish computing, then count "within stoppingDistance and
            // effectively stopped" as arrived. We must NOT require hasPath: Unity clears it the instant
            // the agent finishes (or when avoidance parks it at the spot), which would otherwise leave
            // an agent that physically arrived reporting "not arrived" forever — deadlocking the
            // service handshake.
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh && !navMeshAgent.pathPending)
            {
                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    reached = !navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude <= ARRIVAL_SPEED_SQR_THRESHOLD;
                }
            }

            return reached;
        }
    }
}
