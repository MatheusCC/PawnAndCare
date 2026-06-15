using UnityEngine;

namespace PawsAndCare.Pets
{
    /// <summary>
    /// Drives a pet through its customer lifecycle: Arriving → Queuing → MovingToStation →
    /// BeingServiced → Leaving → Despawning. Pets are autonomous (AI) and never routed through
    /// AgentController. External systems steer it through a small primitive-based API:
    ///   - CustomerSpawner calls BeginArrival (Task 3)
    ///   - ServiceDispatcher calls SendToStation and CompleteService (Task 5)
    /// The dispatcher polls CurrentState (e.g. for BEING_SERVICED) to coordinate the worker/pet
    /// handshake, so this machine stays decoupled from queue/station/dispatcher types.
    ///
    /// Performance: state changes go through EnterState, which switches Update off for passive
    /// states (BEING_SERVICED while the session runs, DESPAWNING) that have no per-frame work.
    /// Only the polling states — which must check NavMesh arrival or tick the patience timer every
    /// frame — keep Update alive. The enum switch is intentionally kept: it allocates nothing,
    /// whereas a cached-delegate dispatch would allocate on every state change.
    /// </summary>
    [RequireComponent(typeof(Pet))]
    public class PetStateMachine : MonoBehaviour
    {
        private Pet pet;
        private PetState currentState;
        private float queueWaitTimer;
        private Vector3 exitPoint;
        private bool hasExitPoint;

        public PetState CurrentState
        {
            get { return currentState; }
        }

        private void Awake()
        {
            pet = GetComponent<Pet>();
            currentState = PetState.ARRIVING;
            // enabled = false stops Update() running; stays dormant until BeginArrival.
            this.enabled = false;
        }

        /// <summary>
        /// Spawner entry point (Task 3): records the facility exit and sends the pet to reception.
        /// </summary>
        public void BeginArrival(Vector3 receptionPoint, Vector3 facilityExit)
        {
            exitPoint = facilityExit;
            hasExitPoint = true;
            pet.MoveTo(receptionPoint);
            EnterState(PetState.ARRIVING);
        }

        /// <summary>
        /// Dispatcher call (Task 5): send the pet to its station's customer spot. On arrival the
        /// pet enters BEING_SERVICED, which the dispatcher polls for to start the session.
        /// </summary>
        public void SendToStation(Vector3 customerPoint)
        {
            pet.MoveTo(customerPoint);
            EnterState(PetState.MOVING_TO_STATION);
        }

        /// <summary>
        /// Dispatcher/session call (Task 5): the service finished — send the pet home.
        /// </summary>
        public void CompleteService()
        {
            BeginLeaving();
        }

        private void Update()
        {
            switch (currentState)
            {
                case PetState.ARRIVING:
                    TickArriving();
                    break;
                case PetState.QUEUING:
                    TickQueuing();
                    break;
                case PetState.MOVING_TO_STATION:
                    TickMovingToStation();
                    break;
                case PetState.LEAVING:
                    TickLeaving();
                    break;
            }
        }

        private void TickArriving()
        {
            if (pet.HasReachedDestination())
            {
                queueWaitTimer = 0.0f;
                EnterState(PetState.QUEUING);
            }
        }

        private void TickQueuing()
        {
            queueWaitTimer += Time.deltaTime;

            if (queueWaitTimer >= pet.GetPatience())
            {
                // Waited too long — leave unhappy. Reputation penalty hooks in at a later milestone.
                BeginLeaving();
            }
        }

        private void TickMovingToStation()
        {
            if (pet.HasReachedDestination())
            {
                // Parked at the station; the dispatcher takes over and starts the session.
                // EnterState switches Update off — the passive wait needs no per-frame work.
                EnterState(PetState.BEING_SERVICED);
            }
        }

        private void TickLeaving()
        {
            if (pet.HasReachedDestination())
            {
                EnterState(PetState.DESPAWNING);
                Destroy(gameObject);
            }
        }

        private void BeginLeaving()
        {
            if (hasExitPoint)
            {
                pet.MoveTo(exitPoint);
                EnterState(PetState.LEAVING);
            }
            else
            {
                // No exit was set (pet wasn't spawned through BeginArrival) — just despawn.
                EnterState(PetState.DESPAWNING);
                Destroy(gameObject);
            }
        }

        // Gates Update() via enabled: passive states don't poll. Public methods still work while
        // disabled, so the dispatcher can call CompleteService on a serviced pet.
        private void EnterState(PetState newState)
        {
            currentState = newState;
            // true = polls every frame (arrival/patience); false = passive wait, Update() off.
            this.enabled = StateNeedsTicking(newState);
        }

        private bool StateNeedsTicking(PetState state)
        {
            bool needsTicking =
                state == PetState.ARRIVING
                || state == PetState.QUEUING
                || state == PetState.MOVING_TO_STATION
                || state == PetState.LEAVING;

            return needsTicking;
        }
    }
}
