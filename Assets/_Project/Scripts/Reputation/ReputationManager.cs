using UnityEngine;
using PawsAndCare.Core;
using PawsAndCare.Services;

namespace PawsAndCare.Reputation
{
    /// <summary>
    /// Owns the business's reputation score (0-100), the primary progression metric. Reacts to the
    /// signals the customer loop already emits: a completed service leaves a 1-5 star review that
    /// nudges reputation up or down from neutral, and a pet that gives up in the queue (a turn-away)
    /// costs reputation. All changes funnel through ApplyDelta, which clamps and publishes
    /// ReputationChangedEvent. Reputation-driven gameplay (customer tier, unlocks) is deferred.
    /// </summary>
    public class ReputationManager : Singleton<ReputationManager>
    {
        private const float MIN_REPUTATION = 0.0f;
        private const float MAX_REPUTATION = 100.0f;
        private const int MIN_STARS = 1;
        private const int MAX_STARS = 5;
        private const int NEUTRAL_STARS = 3;

        [SerializeField]
        [Tooltip("Reputation the business starts with (0-100).")]
        private float startingReputation = 50.0f;

        [SerializeField]
        [Tooltip("Reputation change per star above/below neutral (3) on a completed service.")]
        private float perStarWeight = 1.0f;

        [SerializeField]
        [Tooltip("Reputation lost when a pet gives up in the queue and leaves (a turn-away).")]
        private float abandonPenalty = 3.0f;

        private float reputation;

        public float CurrentReputation
        {
            get { return reputation; }
        }

        protected override void OnInitialize()
        {
            reputation = Mathf.Clamp(startingReputation, MIN_REPUTATION, MAX_REPUTATION);
            EventBus.Subscribe<ServiceCompletedEvent>(OnServiceCompleted);
            EventBus.Subscribe<PetLeftQueueEvent>(OnPetLeftQueue);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<ServiceCompletedEvent>(OnServiceCompleted);
            EventBus.Unsubscribe<PetLeftQueueEvent>(OnPetLeftQueue);
            base.OnDestroy();
        }

        private void OnServiceCompleted(ServiceCompletedEvent eventData)
        {
            int stars = QualityToStars(eventData.Quality);
            float delta = (stars - NEUTRAL_STARS) * perStarWeight;
            ApplyDelta(delta);
        }

        private void OnPetLeftQueue(PetLeftQueueEvent eventData)
        {
            if (eventData.Reason == QueueLeaveReason.ABANDONED)
            {
                ApplyDelta(-abandonPenalty);
            }
        }

        // Maps a [0,1] service quality to a 1-5 star review.
        private int QualityToStars(float quality)
        {
            int stars = Mathf.Clamp(Mathf.RoundToInt(MIN_STARS + quality * (MAX_STARS - MIN_STARS)), MIN_STARS, MAX_STARS);

            return stars;
        }

        // Single chokepoint for reputation changes: clamp, then announce only when it actually moved.
        private void ApplyDelta(float amount)
        {
            float previous = reputation;
            reputation = Mathf.Clamp(reputation + amount, MIN_REPUTATION, MAX_REPUTATION);
            float actualDelta = reputation - previous;

            if (actualDelta != 0.0f)
            {
                EventBus.Publish(new ReputationChangedEvent(reputation, actualDelta));
            }
        }
    }
}
