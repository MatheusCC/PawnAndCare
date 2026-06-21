using UnityEngine;
using PawsAndCare.Core;
using PawsAndCare.Services;

namespace PawsAndCare.Pets
{
    /// <summary>
    /// Spawns customer pets on a timer while the facility is accepting customers. Each spawn picks a
    /// random pet prefab (each prefab carries its own PetDefinition), drops it at the entrance, records
    /// its exit via BeginArrival, then admits it through the ServiceDispatcher (which seats it at a free
    /// station or queues it). A pet that can't be placed at all is turned away straight to the exit.
    ///
    /// Spawning is gated by the day cycle: it runs only during DayManager's open phases
    /// (Morning/Midday/Afternoon) and pauses otherwise, with a shorter interval during the Midday rush.
    /// The full AnimationCurve wave system (TDD §9.2) is deferred to polish.
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

        [SerializeField]
        [Range(0.1f, 1.0f)]
        [Tooltip("Spawn-interval multiplier during the Midday rush (lower = busier peak).")]
        private float middayRushFactor = 0.6f;

        private float spawnTimer;

        private void Awake()
        {
            // Subscribe here (not Start) so a disabled spawner still hears the day's opening phase,
            // and so the subscription is in place before DayManager fires its first phase events.
            EventBus.Subscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);

            if (!HasValidSetup())
            {
                Debug.LogError("[CustomerSpawner] Setup is incomplete — assign pet prefabs and the entrance/exit points in the inspector.", this);
            }

            // Dormant until a day phase starts accepting customers. Disabled means Update (the spawn
            // loop) is off, but the EventBus subscription above still fires.
            enabled = false;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);
        }

        private void OnDayPhaseChanged(DayPhaseChangedEvent eventData)
        {
            bool accepting = DayManager.Instance != null && DayManager.Instance.IsAcceptingCustomers;

            if (accepting && HasValidSetup())
            {
                ResetTimer();
                enabled = true;
            }
            else
            {
                enabled = false;
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

                if (ServiceDispatcher.Instance != null)
                {
                    if (!ServiceDispatcher.Instance.AdmitPet(stateMachine))
                    {
                        // No free station and the queue is full — turn the pet away to the exit.
                        stateMachine.LeaveFacility();
                    }
                }
                else
                {
                    Debug.LogError("[CustomerSpawner] No ServiceDispatcher in scene — cannot admit spawned pet.", this);
                }
            }
            else
            {
                Debug.LogError("[CustomerSpawner] Spawned pet prefab is missing a PetStateMachine component.", this);
            }
        }

        private void ResetTimer()
        {
            spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval) * PhaseIntervalFactor();
        }

        // Tighter spawn cadence during the Midday peak; full daily wave shaping is deferred (TDD §9.2).
        private float PhaseIntervalFactor()
        {
            float factor = 1.0f;

            if (DayManager.Instance != null && DayManager.Instance.CurrentPhase == DayPhase.MIDDAY)
            {
                factor = middayRushFactor;
            }

            return factor;
        }

        private bool HasValidSetup()
        {
            bool valid = petPrefabs != null && petPrefabs.Length > 0
                        && entrancePoint != null && exitPoint != null;

            return valid;
        }
    }
}
