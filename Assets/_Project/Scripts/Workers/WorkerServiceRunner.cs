using UnityEngine;
using PawsAndCare.Services;

namespace PawsAndCare.Workers
{
    /// <summary>
    /// A worker's "walk to a station's worker anchor and report arrival" behavior. Sits beside Worker
    /// (the movement primitive) so job execution stays out of the input layer. In the Phase 2
    /// auto-assign model, ServiceDispatcher owns the match: it reserves the station, starts the
    /// session once both worker and pet have arrived, drives progress, and calls Release on
    /// completion. This runner only walks the worker where told and latches whether it has arrived —
    /// it knows nothing about sessions, progress, or pets.
    /// </summary>
    [RequireComponent(typeof(Worker))]
    public class WorkerServiceRunner : MonoBehaviour
    {
        private Worker worker;
        private ServiceStation assignedStation;
        private bool hasArrived;

        /// <summary>
        /// True while the worker is committed to a station (walking there or working it), so
        /// WorkerManager won't hand it a second job until Release.
        /// </summary>
        public bool IsBusy
        {
            get { return assignedStation != null; }
        }

        /// <summary>
        /// True once the worker has reached its assigned station's worker anchor. The dispatcher
        /// polls this for the worker side of the both-arrived handshake.
        /// </summary>
        public bool HasArrivedAtStation
        {
            get { return hasArrived; }
        }

        public Worker Worker
        {
            get { return worker; }
        }

        private void Awake()
        {
            worker = GetComponent<Worker>();
        }

        // Start (not Awake): WorkerManager is a Singleton initialised in its own Awake. Using Start
        // means every singleton's Awake has run, so WorkerManager.Instance is non-null regardless of
        // GameObject wake order.
        private void Start()
        {
            if (WorkerManager.Instance != null)
            {
                WorkerManager.Instance.Register(this);
            }
            else
            {
                Debug.LogWarning("[WorkerServiceRunner] WorkerManager.Instance is null at Start — worker will not be available for dispatch.", this);
            }
        }

        private void OnDestroy()
        {
            if (WorkerManager.Instance != null)
            {
                WorkerManager.Instance.Unregister(this);
            }
        }

        /// <summary>
        /// Dispatcher order (Task 5): walk the worker to the station's worker anchor. Reservation and
        /// session start are owned by the dispatcher, not here.
        /// </summary>
        public void GoToStation(ServiceStation station)
        {
            if (station != null && station.WorkerAnchor != null)
            {
                assignedStation = station;
                hasArrived = false;
                worker.MoveTo(station.WorkerAnchor.position);
            }
            else
            {
                Debug.LogError("[WorkerServiceRunner] GoToStation requires a station with a WorkerAnchor.", this);
            }
        }

        /// <summary>
        /// Dispatcher call (Task 5): the job finished (or was cancelled) — free the worker for new work.
        /// </summary>
        public void Release()
        {
            assignedStation = null;
            hasArrived = false;
        }

        private void Update()
        {
            if (assignedStation != null && !hasArrived && worker.HasReachedDestination())
            {
                hasArrived = true;
            }
        }
    }
}
