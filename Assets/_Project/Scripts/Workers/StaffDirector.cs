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
    /// income-vs-payroll loop.
    /// </summary>
    public class StaffDirector : MonoBehaviour
    {
        private const float HIRE_PANEL_X = 10.0f;
        private const float HIRE_PANEL_Y = 90.0f;
        private const float HIRE_BUTTON_WIDTH = 220.0f;
        private const float HIRE_BUTTON_HEIGHT = 28.0f;
        private const float HIRE_BUTTON_GAP = 4.0f;

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

        // Cached Worker components off the hireable prefabs, so the debug panel reads role/cost
        // without a per-frame GetComponent.
        private Worker[] hireablePreviews;

        private void Awake()
        {
            CacheHireablePreviews();
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

        // Temporary debug hire panel — clicking a button hires that worker if affordable. The real
        // hiring UI replaces this in the polish pass.
        private void OnGUI()
        {
            float y = HIRE_PANEL_Y;

            for (int i = 0; i < hireablePreviews.Length; i++)
            {
                Worker preview = hireablePreviews[i];

                if (preview != null)
                {
                    Rect rect = new Rect(HIRE_PANEL_X, y, HIRE_BUTTON_WIDTH, HIRE_BUTTON_HEIGHT);

                    if (GUI.Button(rect, $"Hire {preview.Role}  (${preview.GetHireCost():0})"))
                    {
                        TryHire(hireablePrefabs[i], preview.GetHireCost());
                    }

                    y += HIRE_BUTTON_HEIGHT + HIRE_BUTTON_GAP;
                }
            }
        }

        private void CacheHireablePreviews()
        {
            if (hireablePrefabs != null)
            {
                hireablePreviews = new Worker[hireablePrefabs.Length];

                for (int i = 0; i < hireablePrefabs.Length; i++)
                {
                    if (hireablePrefabs[i] != null)
                    {
                        hireablePreviews[i] = hireablePrefabs[i].GetComponent<Worker>();
                    }
                }
            }
            else
            {
                hireablePreviews = new Worker[0];
            }
        }

        // Charges the hire cost and spawns the worker (which self-registers with WorkerManager on
        // Start). Validated before charging so we never bill without placing the worker.
        private void TryHire(GameObject workerPrefab, float hireCost)
        {
            bool affordable = EconomyManager.Instance != null && EconomyManager.Instance.Balance >= hireCost;

            if (affordable && hireSpawnPoint != null)
            {
                EventBus.Publish(new ExpenseIncurredEvent(hireCost, ExpenseType.HIRING));
                Instantiate(workerPrefab, hireSpawnPoint.position, Quaternion.identity, transform);
            }
            else if (hireSpawnPoint == null)
            {
                Debug.LogError("[StaffDirector] hireSpawnPoint is missing — cannot place a hired worker. Assign one in the inspector.", this);
            }
        }
    }
}
