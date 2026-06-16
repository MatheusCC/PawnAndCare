using UnityEngine;
using PawsAndCare.Services;

namespace PawsAndCare.Pets
{
    /// <summary>
    /// Spawns customer pets on a timer once the game is running. Each spawn picks a random pet
    /// prefab (each prefab carries its own PetDefinition), drops it at the entrance, records its
    /// exit via BeginArrival, then enqueues it into the ReceptionQueue (which walks it to a slot).
    /// A full queue turns the pet away straight to the exit.
    ///
    /// Pacing is a simple random interval for now; day-phase gating wires in when DayManager lands
    /// (Phase 2 Task 8).
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Pet prefab variants to spawn. Each must carry a Pet + PetStateMachine and its own PetDefinition.")]
        private GameObject[] petPrefabs = null;

        [SerializeField]
        [Tooltip("Where pets appear. Must sit on the NavMesh.")]
        private Transform entrancePoint = null;

        [SerializeField]
        [Tooltip("Where pets walk to before despawning. Must sit on the NavMesh.")]
        private Transform exitPoint = null;

        [SerializeField]
        [Tooltip("Shortest delay between spawns, in seconds.")]
        private float minSpawnInterval = 5.0f;

        [SerializeField]
        [Tooltip("Longest delay between spawns, in seconds.")]
        private float maxSpawnInterval = 12.0f;

        private float spawnTimer;

        private void Awake()
        {
            // Dormant until GameManager calls StartSpawning — spawning a NavMeshAgent before the
            // facility's NavMesh exists would land it off-mesh and break pathing.
            enabled = false;
        }

        /// <summary>
        /// Begins timed spawning. Called by GameManager.BootGame after the facility + NavMesh exist.
        /// </summary>
        public void StartSpawning()
        {
            if (HasValidSetup())
            {
                ResetTimer();
                enabled = true;
            }
            else
            {
                Debug.LogError("[CustomerSpawner] Setup is incomplete — assign pet prefabs and the entrance/exit points in the inspector.", this);
            }
        }

        private void Update()
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0.0f)
            {
                SpawnPet();
                ResetTimer();
            }
        }

        private void SpawnPet()
        {
            int prefabIndex = Random.Range(0, petPrefabs.Length);
            GameObject petInstance = Instantiate(petPrefabs[prefabIndex], entrancePoint.position, Quaternion.identity, transform);
            PetStateMachine stateMachine = petInstance.GetComponent<PetStateMachine>();

            if (stateMachine != null)
            {
                stateMachine.BeginArrival(exitPoint.position);

                if (ReceptionQueue.Instance != null)
                {
                    if (!ReceptionQueue.Instance.TryEnqueue(stateMachine))
                    {
                        // Queue is full — turn the pet away straight to the exit.
                        stateMachine.LeaveFacility();
                    }
                }
                else
                {
                    Debug.LogError("[CustomerSpawner] No ReceptionQueue in scene — cannot enqueue spawned pet.", this);
                }
            }
            else
            {
                Debug.LogError("[CustomerSpawner] Spawned pet prefab is missing a PetStateMachine component.", this);
            }
        }

        private void ResetTimer()
        {
            spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        private bool HasValidSetup()
        {
            bool valid = petPrefabs != null && petPrefabs.Length > 0
                        && entrancePoint != null && exitPoint != null;

            return valid;
        }
    }
}
