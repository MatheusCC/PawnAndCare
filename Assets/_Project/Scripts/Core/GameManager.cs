using UnityEngine;
using PawsAndCare.Building;
using PawsAndCare.Workers;
using PawsAndCare.Pets;

namespace PawsAndCare.Core
{
    public enum GameState
    {
        MAIN_MENU = 0,
        PLAYING = 1,
        PAUSED = 2,
        LOADING = 3
    }

    /// <summary>
    /// Central authority for the game's high-level state and boot sequence.
    /// Owns the GameState lifecycle and orchestrates ordered system startup.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField]
        private FacilityBuilder facilityBuilder = null;
        [SerializeField]
        private WorkerSpawner workerSpawner = null;
        [SerializeField]
        private CustomerSpawner customerSpawner = null;

        private GameState currentState;
        private GameState previousState;

        public GameState CurrentState
        {
            get { return currentState; }
        }

        public GameState PreviousState
        {
            get { return previousState; }
        }

        protected override void OnInitialize()
        {
            // Boot starts in LOADING so any system listening for "game ready" only triggers
            // after the LOADING → PLAYING transition fired at the end of BootGame().
            currentState = GameState.LOADING;
            previousState = GameState.LOADING;
        }

        private void Start()
        {
            // Start (not OnInitialize/Awake) so every other system's Awake has finished —
            // GridSystem.Awake allocates cells, and FacilityBuilder needs those cells.
            BootGame();
        }

        private void BootGame()
        {
            // Strict ordering: facility geometry + NavMesh must exist before any
            // NavMeshAgent (pet, worker) spawns, otherwise agents land off-mesh and
            // SetDestination silently does nothing.
            if (facilityBuilder != null)
            {
                facilityBuilder.Build();
            }
            else
            {
                Debug.LogError("[GameManager] FacilityBuilder reference is missing — assign one in the inspector.", this);
            }

            if (workerSpawner != null)
            {
                workerSpawner.Spawn();
            }
            else
            {
                Debug.LogWarning("[GameManager] WorkerSpawner reference is missing — assign one in the inspector.", this);
            }

            if (customerSpawner != null)
            {
                customerSpawner.StartSpawning();
            }
            else
            {
                Debug.LogWarning("[GameManager] CustomerSpawner reference is missing — assign one in the inspector.", this);
            }

            ChangeState(GameState.PLAYING);
        }

        /// <summary>
        /// Transitions the game to a new state. Same-state calls are ignored.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (newState != currentState)
            {
                previousState = currentState;
                currentState = newState;

#if UNITY_EDITOR
                Debug.Log($"[GameManager] State: {previousState} → {currentState}");
#endif

                EventBus.Publish(new GameStateChangedEvent(previousState, currentState));
            }
        }
    }
}
