namespace PawsAndCare.Core
{
    /// <summary>
    /// Published by ReputationManager whenever the reputation score changes. Delta is the signed
    /// change applied (positive for good reviews, negative for turn-aways or poor service).
    /// </summary>
    public readonly struct ReputationChangedEvent
    {
        private readonly float newReputation;
        private readonly float delta;

        public float NewReputation
        {
            get { return newReputation; }
        }

        public float Delta
        {
            get { return delta; }
        }

        public ReputationChangedEvent(float newReputation, float delta)
        {
            this.newReputation = newReputation;
            this.delta = delta;
        }
    }
}
