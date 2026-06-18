using UnityEngine;
using PawsAndCare.Core;

namespace PawsAndCare.Economy
{
    /// <summary>
    /// Owns the business's running balance. Turns completed services into revenue: subscribes to
    /// ServiceCompletedEvent, charges the service's BasePrice scaled by the achieved quality, and
    /// publishes BalanceChangedEvent. Every balance change funnels through ApplyDelta, so future
    /// spending (staff salaries, Task 11) reuses the same chokepoint.
    /// </summary>
    public class EconomyManager : Singleton<EconomyManager>
    {
        [SerializeField]
        [Tooltip("Cash the business starts the game with.")]
        private float startingBalance = 100.0f;

        private float balance;

        public float Balance
        {
            get { return balance; }
        }

        protected override void OnInitialize()
        {
            balance = startingBalance;
            EventBus.Subscribe<ServiceCompletedEvent>(OnServiceCompleted);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<ServiceCompletedEvent>(OnServiceCompleted);
            base.OnDestroy();
        }

        private void OnServiceCompleted(ServiceCompletedEvent eventData)
        {
            // Revenue = the full-quality price (BasePrice) scaled by the achieved quality [0.5, 1.0],
            // so a higher-skilled worker earns more for the same service.
            float revenue = eventData.Session.Service.BasePrice * eventData.Quality;
            ApplyDelta(revenue);
        }

        // Single chokepoint for every balance change: mutate, then announce.
        private void ApplyDelta(float amount)
        {
            balance += amount;
            EventBus.Publish(new BalanceChangedEvent(balance, amount));
        }
    }
}
