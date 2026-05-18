using UnityEngine;
using UnityEngine.AI;

namespace PawsAndCare.Workers
{
    /// <summary>
    /// Player-controllable worker with NavMeshAgent-based pathfinding and movement.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Worker : MonoBehaviour
    {
        private NavMeshAgent navMeshAgent = null;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
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
