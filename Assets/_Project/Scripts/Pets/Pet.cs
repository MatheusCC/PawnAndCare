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
            bool reached = false;

            if (navMeshAgent != null)
            {
                if (navMeshAgent.isOnNavMesh && navMeshAgent.hasPath)
                {
                    // Agent stops within its stoppingDistance — exact arrival is impossible
                    // due to agent radius and float-precision drift on the final approach.
                    reached = navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance;
                }
            }

            return reached;
        }
    }
}
