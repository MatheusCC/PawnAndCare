using System.Collections.Generic;
using UnityEngine;
using PawsAndCare.Core;
using PawsAndCare.Pets;
using PawsAndCare.Workers;

namespace PawsAndCare.Services
{
    /// <summary>
    /// Auto-assign brain for the customer loop (Phase 2 Task 5). Owns the convergence handshake that
    /// no single actor can: it seats arriving pets at free stations, dispatches free workers to
    /// staffed stations, starts the ServiceSession once both the pet and worker have arrived, mirrors
    /// progress, and releases everyone on completion.
    ///
    /// Model: a pet is seated at a free station the moment it arrives (reserving it); it only waits in
    /// the ReceptionQueue when no station is free. A worker is sent to a station as soon as one has a
    /// pet but no worker. When a service completes, the freed station immediately pulls the
    /// longest-waiting matching pet out of the queue.
    /// </summary>
    public class ServiceDispatcher : Singleton<ServiceDispatcher>
    {
        private List<ServiceJob> jobs;

        protected override void OnInitialize()
        {
            jobs = new List<ServiceJob>();
        }

        /// <summary>
        /// Intake (Task 5): seat the pet at a free station if one supports its service, otherwise put
        /// it in the reception queue. Returns false when no station is free and the queue is full, so
        /// the spawner can turn the pet away.
        /// </summary>
        public bool AdmitPet(PetStateMachine pet)
        {
            bool admitted = false;

            if (pet != null)
            {
                if (TrySeatAtFreeStation(pet))
                {
                    admitted = true;
                }
                else if (ReceptionQueue.Instance != null)
                {
                    admitted = ReceptionQueue.Instance.TryEnqueue(pet);
                }
            }

            return admitted;
        }

        private void Update()
        {
            StaffJobsNeedingWorkers();
            StartReadyJobs();
            AdvanceActiveJobs();
        }

        // Pass 1: every staffed-less job tries to pull the best free worker for its service type.
        private void StaffJobsNeedingWorkers()
        {
            if (WorkerManager.Instance != null)
            {
                for (int i = 0; i < jobs.Count; i++)
                {
                    ServiceJob job = jobs[i];

                    if (job.Worker == null && job.Station.Data != null)
                    {
                        WorkerServiceRunner worker = WorkerManager.Instance.GetAvailableWorker(job.Station.Data.ServiceType);

                        if (worker != null)
                        {
                            job.Worker = worker;
                            worker.GoToStation(job.Station);
                        }
                    }
                }
            }
        }

        // Pass 2: once a job's worker and pet have both arrived at the station, start the session.
        private void StartReadyJobs()
        {
            if (ServiceManager.Instance != null)
            {
                for (int i = 0; i < jobs.Count; i++)
                {
                    ServiceJob job = jobs[i];

                    if (job.Session == null && job.Worker != null && IsJobReady(job))
                    {
                        StartJob(job);
                    }
                }
            }
        }

        // Pass 3: mirror progress to the station bar; on completion, release everyone and re-fill the station.
        private void AdvanceActiveJobs()
        {
            // Iterate backward so completed jobs can be removed in place.
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                ServiceJob job = jobs[i];

                if (job.Session != null)
                {
                    job.Station.ShowServiceProgress(job.Session.Progress);

                    if (job.Session.Status == ServiceStatus.COMPLETED)
                    {
                        CompleteJob(job);
                        jobs.RemoveAt(i);
                    }
                }
            }
        }

        private bool IsJobReady(ServiceJob job)
        {
            // Pet arrival is signalled by its own state machine entering BEING_SERVICED on reaching
            // the customer anchor; the worker latches HasArrivedAtStation at its anchor.
            bool ready = job.Worker.HasArrivedAtStation && job.Pet.CurrentState == PetState.BEING_SERVICED;

            return ready;
        }

        private void StartJob(ServiceJob job)
        {
            job.Session = ServiceManager.Instance.StartService(job.Station, job.Station.Data, job.Worker.Worker, job.Pet);

            if (job.Session != null)
            {
                job.Station.ShowServiceProgress(0.0f);
            }
        }

        private void CompleteJob(ServiceJob job)
        {
            job.Station.HideServiceProgress();
            job.Station.SetOccupied(false);
            job.Worker.Release();
            job.Pet.CompleteService();

            // The station is free again — immediately seat the longest-waiting matching queued pet.
            TrySeatQueuedPetAt(job.Station);
        }

        private bool TrySeatAtFreeStation(PetStateMachine pet)
        {
            bool seated = false;

            if (StationManager.Instance != null)
            {
                ServiceStation station = StationManager.Instance.GetAvailableStation(pet.DesiredService);

                if (station != null)
                {
                    SeatPet(station, pet);
                    seated = true;
                }
            }

            return seated;
        }

        private void TrySeatQueuedPetAt(ServiceStation station)
        {
            if (ReceptionQueue.Instance != null && station.Data != null)
            {
                PetStateMachine queuedPet = ReceptionQueue.Instance.PeekNextForService(station.Data.ServiceType);

                if (queuedPet != null)
                {
                    ReceptionQueue.Instance.Remove(queuedPet, QueueLeaveReason.DISPATCHED);
                    SeatPet(station, queuedPet);
                }
            }
        }

        // Reserves the station and sends the pet to it. The pet waits at the customer anchor until a
        // worker arrives and the session starts.
        private void SeatPet(ServiceStation station, PetStateMachine pet)
        {
            station.SetOccupied(true);
            jobs.Add(new ServiceJob(station, pet));

            if (station.CustomerAnchor != null)
            {
                pet.SendToStation(station.CustomerAnchor.position);
            }
            else
            {
                Debug.LogError("[ServiceDispatcher] Seated a pet at a station with no CustomerAnchor — assign one in the inspector.", this);
            }
        }

        // One pet's path through a station: reserved → staffed → in service. Worker is null until a
        // worker is dispatched; Session is null until both have arrived and the service starts.
        private class ServiceJob
        {
            public ServiceStation Station { get; }
            public PetStateMachine Pet { get; }
            public WorkerServiceRunner Worker { get; set; }
            public ServiceSession Session { get; set; }

            public ServiceJob(ServiceStation station, PetStateMachine pet)
            {
                Station = station;
                Pet = pet;
            }
        }
    }
}
