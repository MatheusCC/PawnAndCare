using UnityEngine;
using PawsAndCare.Core;

namespace PawsAndCare.Economy
{
    /// <summary>
    /// Owns the business's running balance and the current day's revenue. Turns completed services
    /// into revenue: subscribes to ServiceCompletedEvent, charges the service's BasePrice scaled by
    /// the achieved quality, and publishes BalanceChangedEvent. Tracks DailyRevenue (reset each day
    /// start) so the end-of-day summary can read it. Every balance change funnels through ApplyDelta,
    /// so future spending (staff salaries, Task 11) reuses the same chokepoint.
    /// </summary>
    public class EconomyManager : Singleton<EconomyManager>
    {
        [SerializeField]
        [Tooltip("Cash the business starts the game with.")]
        private float startingBalance = 100.0f;

        private float balance;
        private float dailyRevenue;

        public float Balance
        {
            get { return balance; }
        }

        public float DailyRevenue
        {
            get { return dailyRevenue; }
        }

        protected override void OnInitialize()
        {
            balance = startingBalance;
            dailyRevenue = 0.0f;
            EventBus.Subscribe<ServiceCompletedEvent>(OnServiceCompleted);
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<ServiceCompletedEvent>(OnServiceCompleted);
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<DayEndedEvent>(OnDayEnded);
            base.OnDestroy();
        }

        private void OnServiceCompleted(ServiceCompletedEvent eventData)
        {
            // Revenue = the full-quality price (BasePrice) scaled by the achieved quality [0.5, 1.0],
            // so a higher-skilled worker earns more for the same service.
            float revenue = eventData.Session.Service.BasePrice * eventData.Quality;
            dailyRevenue += revenue;
            ApplyDelta(revenue);
        }

        private void OnDayStarted(DayStartedEvent eventData)
        {
            dailyRevenue = 0.0f;
        }

        private void OnDayEnded(DayEndedEvent eventData)
        {
            // Minimal end-of-day revenue summary (6B.2). The full-screen summary panel (TDD §9.3) is
            // deferred to the polish pass.
#if UNITY_EDITOR
            Debug.Log($"[EconomyManager] Day {eventData.DayNumber} ended — revenue ${dailyRevenue:0.00}, balance ${balance:0.00}.");
#endif
        }

        // Single chokepoint for every balance change: mutate, then announce.
        private void ApplyDelta(float amount)
        {
            balance += amount;
            EventBus.Publish(new BalanceChangedEvent(balance, amount));
        }
    }
}
