using UnityEngine;
using PawsAndCare.Services;

namespace PawsAndCare.Pets
{
    // Drives a pet through its customer lifecycle: Arriving → Queuing → MovingToStation →
    // BeingServiced → Leaving → Despawning. Pets are autonomous (AI) and never routed through
    // AgentController. External systems steer it through a small primitive-based API:
    //   - CustomerSpawner calls BeginArrival
    //   - ReceptionQueue calls SendToQueueSlot / MoveToQueueSlot / LeaveFacility
    //   - ServiceDispatcher calls SendToStation and CompleteService
    // Those systems poll CurrentState (e.g. QUEUING to time patience, BEING_SERVICED to start the
    // session) and read the DesiredService/Patience passthroughs, so this machine stays decoupled
    // from queue/station/dispatcher types.
    //
    // Performance: state changes go through EnterState, which switches Update off for passive
    // states (QUEUING — ReceptionQueue owns the patience timer; BEING_SERVICED while the session
    // runs; DESPAWNING) that have no per-frame work. Only the movement-polling states, which must
    // check NavMesh arrival every frame, keep Update alive. The enum switch is intentionally kept:
    // it allocates nothing, whereas a cached-delegate dispatch would allocate on every state change.
    [RequireComponent(typeof(Pet))]
    public class PetStateMachine : MonoBehaviour
    {
        private Pet pet;
        private PetState currentState;
        private Vector3 exitPoint;
        private bool hasExitPoint;
        private Transform stationSeat;

        public PetState CurrentState
        {
            get { return currentState; }
        }

        public ServiceType DesiredService
        {
            get { return pet.GetDesiredService(); }
        }

        public float Patience
        {
            get { return pet.GetPatience(); }
        }

        private void Awake()
        {
            pet = GetComponent<Pet>();
            currentState = PetState.ARRIVING;
            // enabled = false stops Update() running; stays dormant until BeginArrival.
            this.enabled = false;
        }

        // Spawner entry point: records the facility exit. ReceptionQueue drives the
        // first move via SendToQueueSlot once it assigns a slot (or the spawner calls LeaveFacility
        // if the queue is full), so no movement happens here.
        public void BeginArrival(Vector3 facilityExit)
        {
            exitPoint = facilityExit;
            hasExitPoint = true;
        }

        // ReceptionQueue call: walk to a freshly assigned queue slot. On arrival the pet
        // settles into QUEUING, where ReceptionQueue starts timing its patience.
        public void SendToQueueSlot(Vector3 slotPosition)
        {
            pet.MoveTo(slotPosition);
            EnterState(PetState.ARRIVING);
        }

        // ReceptionQueue call: shift forward as the line advances. Stays in its current
        // state — moving up the line isn't a fresh wait, so the patience clock keeps running.
        public void MoveToQueueSlot(Vector3 slotPosition)
        {
            pet.MoveTo(slotPosition);
        }

        // ReceptionQueue/spawner call: turn the pet away to the exit — the queue was full
        // on arrival, or its patience ran out while waiting.
        public void LeaveFacility()
        {
            BeginLeaving();
        }

        // Dispatcher call: send the pet to its station's seat. On arrival the pet hops onto
        // the seat and enters BEING_SERVICED, which the dispatcher polls for to start the session.
        public void SendToStation(Transform seat)
        {
            // The seat sits on top of the station; NavMesh projects it to the nearest floor point,
            // so the pet walks up beside the station and SitAt lifts it onto the seat on arrival.
            stationSeat = seat;
            pet.MoveTo(seat.position);
            EnterState(PetState.MOVING_TO_STATION);
        }

        // Dispatcher/session call: the service finished — send the pet home.
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
                // Parked at the queue slot; ReceptionQueue takes over and times the patience.
                // EnterState switches Update off — QUEUING has no per-frame work for the pet.
                EnterState(PetState.QUEUING);
            }
        }

        private void TickMovingToStation()
        {
            if (pet.HasReachedDestination())
            {
                // Parked at the station; the dispatcher takes over and starts the session.
                // EnterState switches Update off — the passive wait needs no per-frame work.
                pet.SitAt(stationSeat);
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
            // No-op unless seated; the agent must be back on the NavMesh before MoveTo can path.
            pet.StandUp();
            stationSeat = null;

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
        // disabled, so ReceptionQueue/dispatcher can steer a waiting or serviced pet.
        private void EnterState(PetState newState)
        {
            currentState = newState;
            // true = polls NavMesh arrival every frame; false = passive wait, Update() off.
            this.enabled = StateNeedsTicking(newState);
        }

        private bool StateNeedsTicking(PetState state)
        {
            bool needsTicking =
                state == PetState.ARRIVING
                || state == PetState.MOVING_TO_STATION
                || state == PetState.LEAVING;

            return needsTicking;
        }
    }
}
