using System.Collections.Generic;
using UnityEngine;
using PawsAndCare.Core;
using PawsAndCare.Economy;

namespace PawsAndCare.Workers
{
    /// <summary>
    /// The staff/HR system — a facility-wide scene component (not a singleton; nothing queries it
    /// globally, it only reacts to events and the hire panel). Grows a worker's skill each time it
    /// completes a service, pays daily salaries at day end, and hires new workers on demand. Skill
    /// growth feeds service quality and reputation; salaries make the economy a real
    /// income-vs-payroll loop. The hire UI reads HireOptions and calls TryHire.
    /// </summary>
    public class StaffDirector : MonoBehaviour
    {
        /// <summary>A hireable worker option surfaced to the UI: what it is and what it costs.</summary>
        public readonly struct HireOption
        {
            private readonly WorkerRole role;
            private readonly float hireCost;
            private readonly bool isValid;

            public WorkerRole Role
            {
                get { return role; }
            }

            public float HireCost
            {
                get { return hireCost; }
            }

            /// <summary>
            /// True only for options built through the constructor. A mis-authored prefab (missing
            /// Worker component) leaves the default struct in the array — invalid, so the UI skips it
            /// and TryHire refuses it instead of hiring a broken worker for free.
            /// </summary>
            public bool IsValid
            {
                get { return isValid; }
            }

            public HireOption(WorkerRole role, float hireCost)
            {
                this.role = role;
                this.hireCost = hireCost;
                this.isValid = true;
            }
        }

        [SerializeField]
        [Range(0.0f, 0.5f)]
        [Tooltip("Skill (0-1) the performing worker gains each time a service completes.")]
        private float skillGainPerService = 0.02f;

        [SerializeField]
        [Tooltip("Worker prefab variants the player can hire. Each must carry a Worker + its own WorkerData.")]
        private GameObject[] hireablePrefabs = null;

        [SerializeField]
        [Tooltip("Where hired workers appear. Must sit on the NavMesh.")]
        private Transform hireSpawnPoint = null;

        // Immutable role/cost snapshot of each hireable prefab, so the UI reads it without a
        // per-frame GetComponent. Index-aligned with hireablePrefabs.
        private HireOption[] hireOptions;

        /// <summary>The hireable options, index-aligned with TryHire's index parameter.</summary>
        public IReadOnlyList<HireOption> HireOptions
        {
            get { return hireOptions; }
        }

        private void Awake()
        {
            CacheHireOptions();
            EventBus.Subscribe<ServiceCompletedEvent>(OnServiceCompleted);
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ServiceCompletedEvent>(OnServiceCompleted);
            EventBus.Unsubscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnServiceCompleted(ServiceCompletedEvent eventData)
        {
            // The worker who performed it gets a little better at that service — for next time, since
            // this session's quality is already locked in.
            Worker worker = eventData.Session.Worker;

            if (worker != null && eventData.Session.Service != null)
            {
                worker.GainSkill(eventData.Session.Service.ServiceType, skillGainPerService);
            }
        }

        private void OnDayEnded(DayEndedEvent eventData)
        {
            if (WorkerManager.Instance != null)
            {
                float payroll = WorkerManager.Instance.GetDailyPayroll();

                if (payroll > 0.0f)
                {
                    EventBus.Publish(new ExpenseIncurredEvent(payroll, ExpenseType.SALARY));
                }
            }
        }

        /// <summary>
        /// Hires the candidate at the given index if it is valid and affordable: charges the hire cost
        /// and spawns the worker (which self-registers with WorkerManager on Start). Validated before
        /// charging so we never bill without placing the worker. Returns true on success.
        /// </summary>
        public bool TryHire(int index)
        {
            bool hired = false;

            if (index >= 0 && index < hireOptions.Length && hireOptions[index].IsValid && hireablePrefabs[index] != null)
            {
                float hireCost = hireOptions[index].HireCost;
                bool affordable = EconomyManager.Instance != null && EconomyManager.Instance.Balance >= hireCost;

                if (affordable && hireSpawnPoint != null)
                {
                    EventBus.Publish(new ExpenseIncurredEvent(hireCost, ExpenseType.HIRING));
                    Instantiate(hireablePrefabs[index], hireSpawnPoint.position, Quaternion.identity, transform);
                    hired = true;
                }
                else if (hireSpawnPoint == null)
                {
                    Debug.LogError("[StaffDirector] hireSpawnPoint is missing — cannot place a hired worker. Assign one in the inspector.", this);
                }
            }

            return hired;
        }

        // Snapshots each hireable prefab's role + cost off its Worker component once at boot.
        private void CacheHireOptions()
        {
            if (hireablePrefabs != null)
            {
                hireOptions = new HireOption[hireablePrefabs.Length];

                for (int i = 0; i < hireablePrefabs.Length; i++)
                {
                    Worker preview = hireablePrefabs[i] != null ? hireablePrefabs[i].GetComponent<Worker>() : null;

                    if (preview != null)
                    {
                        hireOptions[i] = new HireOption(preview.Role, preview.GetHireCost());
                    }
                    else
                    {
                        Debug.LogError($"[StaffDirector] hireablePrefabs[{i}] is missing or has no Worker component.", this);
                    }
                }
            }
            else
            {
                hireOptions = new HireOption[0];
            }
        }
    }
}
