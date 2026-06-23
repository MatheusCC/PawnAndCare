using UnityEngine;
using UnityEngine.AI;
using PawsAndCare.Services;

namespace PawsAndCare.Pets
{
    /// <summary>
    /// A customer pet: a NavMeshAgent wrapper (mirrors Worker) plus its PetDefinition data —
    /// the service it wants and how long it will wait. Movement + data live here; the lifecycle
    /// (arrive, queue, get serviced, leave) lives in the sibling PetStateMachine.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Pet : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Defines this pet's species, the service it wants, and its patience.")]
        private PetDefinition petDefinition = null;

        private NavMeshAgent navMeshAgent;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();

            if (petDefinition == null)
            {
                Debug.LogWarning("[Pet] petDefinition is missing — pet has no desired service or patience. Assign one in the inspector.", this);
            }
        }

        /// <summary>
        /// Returns the service this pet arrives wanting, or GROOMING as a fallback if no definition is assigned.
        /// </summary>
        public ServiceType GetDesiredService()
        {
            ServiceType result = ServiceType.GROOMING;

            if (petDefinition != null)
            {
                result = petDefinition.DesiredService;
            }

            return result;
        }

        /// <summary>
        /// Returns the seconds this pet will wait in the queue before leaving unhappy, or 0 if no definition is assigned.
        /// </summary>
        public float GetPatience()
        {
            float result = 0.0f;

            if (petDefinition != null)
            {
                result = petDefinition.Patience;
            }

            return result;
        }


        /// <summary>
        /// Commands the pet to move to a world position using NavMesh pathfinding.
        /// </summary>
        public void MoveTo(Vector3 destination)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(destination);
            }
        }

        /// <summary>
        /// Returns true only when the pet has actually arrived at its destination.
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
