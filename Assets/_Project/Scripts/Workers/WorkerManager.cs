using System.Collections.Generic;
using UnityEngine;
using PawsAndCare.Core;
using PawsAndCare.Services;

namespace PawsAndCare.Workers
{
    /// <summary>
    /// Tracks every live WorkerServiceRunner in the scene and answers availability queries for the
    /// ServiceDispatcher. Workers self-register on Start and unregister on destroy. Mirrors StationManager.
    /// </summary>
    public class WorkerManager : Singleton<WorkerManager>
    {
        private List<WorkerServiceRunner> registeredWorkers;

        protected override void OnInitialize()
        {
            registeredWorkers = new List<WorkerServiceRunner>();
        }

        /// <summary>
        /// Adds a worker to the registry. Called by WorkerServiceRunner.Start.
        /// </summary>
        public void Register(WorkerServiceRunner worker)
        {
            if (worker != null && !registeredWorkers.Contains(worker))
            {
                registeredWorkers.Add(worker);
            }
        }

        /// <summary>
        /// Removes a worker from the registry. Called by WorkerServiceRunner.OnDestroy.
        /// </summary>
        public void Unregister(WorkerServiceRunner worker)
        {
            if (worker != null)
            {
                registeredWorkers.Remove(worker);
            }
        }

        /// <summary>
        /// Returns the free worker with the highest skill for the given service type, or null if none
        /// is available — so the best-suited idle worker takes the job.
        /// </summary>
        public WorkerServiceRunner GetAvailableWorker(ServiceType type)
        {
            // Linear scan ranked by skill: worker count is tiny, so picking the max in one pass
            // beats maintaining a sorted structure. Revisit if profiling shows a hot spot.
            WorkerServiceRunner result = null;
            float bestSkill = -1.0f;

            for (int i = 0; i < registeredWorkers.Count; i++)
            {
                WorkerServiceRunner candidate = registeredWorkers[i];
                if (!candidate.IsBusy)
                {
                    float skill = candidate.Worker.GetSkillRating(type);
                    if (skill > bestSkill)
                    {
                        bestSkill = skill;
                        result = candidate;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Sums the daily salary of every registered worker — the payroll owed at day end.
        /// </summary>
        public float GetDailyPayroll()
        {
            float total = 0.0f;

            for (int i = 0; i < registeredWorkers.Count; i++)
            {
                total += registeredWorkers[i].Worker.GetDailySalary();
            }

            return total;
        }
    }
}
