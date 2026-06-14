using UnityEngine;
using PawsAndCare.Services;

namespace PawsAndCare.Workers
{
    /// <summary>
    /// Owns a worker's "go to a station and perform its service" behavior: reserve the station,
    /// walk to its anchor, start the session on arrival, drive the station's progress bar, and
    /// release everything on completion or cancellation. Sits beside Worker (movement primitive)
    /// so the worker's job-execution state stays out of the input layer.
    /// </summary>
    [RequireComponent(typeof(Worker))]
    public class WorkerServiceRunner : MonoBehaviour
    {
        private Worker worker;
        private ServiceStation assignedStation;
        private ServiceSession activeSession;
        private bool isMovingToStation;

        /// <summary>
        /// True once a session is running — the worker is locked to the station until it completes.
        /// </summary>
        public bool IsServicing
        {
            get { return activeSession != null; }
        }

        private void Awake()
        {
            worker = GetComponent<Worker>();
        }

        /// <summary>
        /// Player order: reserve the station and walk the worker to it to perform its service.
        /// Replaces any previous assignment.
        /// </summary>
        public void AssignStation(ServiceStation station)
        {
            if (station != null && station.WorkerAnchor != null)
            {
                ClearAssignment();
                assignedStation = station;
                assignedStation.SetOccupied(true);
                worker.MoveTo(assignedStation.WorkerAnchor.position);
                isMovingToStation = true;
            }
            else
            {
                Debug.LogError("[WorkerServiceRunner] AssignStation requires a station with a WorkerAnchor.", this);
            }
        }

        /// <summary>
        /// Player order: walk to a free point. Ignored while a service is in progress so the
        /// worker can't be pulled off an active station mid-service.
        /// </summary>
        public void RequestMove(Vector3 worldPoint)
        {
            if (!IsServicing)
            {
                ClearAssignment();
                worker.MoveTo(worldPoint);
            }
        }

        private void Update()
        {
            if (assignedStation != null)
            {
                if (isMovingToStation)
                {
                    AdvanceToStation();
                }
                else
                {
                    AdvanceService();
                }
            }
        }

        // Arrival phase: once at the anchor, hand off to ServiceManager to start the session.
        private void AdvanceToStation()
        {
            if (worker.HasReachedDestination())
            {
                isMovingToStation = false;
                StartServiceAtStation();
            }
        }

        private void StartServiceAtStation()
        {
            if (ServiceManager.Instance != null)
            {
                activeSession = ServiceManager.Instance.StartService(assignedStation, assignedStation.Data, worker);
            }
            else
            {
                Debug.LogError("[WorkerServiceRunner] ServiceManager.Instance is null — cannot start service.", this);
            }

            if (activeSession != null)
            {
                assignedStation.ShowServiceProgress(0.0f);
            }
            else
            {
                ClearAssignment();
            }
        }

        // Service phase: mirror progress to the station bar; release when the session completes.
        private void AdvanceService()
        {
            if (activeSession != null)
            {
                assignedStation.ShowServiceProgress(activeSession.Progress);

                if (activeSession.Status == ServiceStatus.COMPLETED)
                {
                    ClearAssignment();
                }
            }
        }

        // Releases the station reservation + progress bar and returns the worker to idle.
        private void ClearAssignment()
        {
            if (assignedStation != null)
            {
                assignedStation.SetOccupied(false);
                assignedStation.HideServiceProgress();
            }

            assignedStation = null;
            activeSession = null;
            isMovingToStation = false;
        }
    }
}
