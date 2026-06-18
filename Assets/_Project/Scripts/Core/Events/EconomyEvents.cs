namespace PawsAndCare.Core
{
    /// <summary>
    /// Published by EconomyManager whenever the running balance changes. Delta is the signed amount
    /// applied (positive for revenue, negative for spending), so UI can show both the new total and
    /// what just changed.
    /// </summary>
    public readonly struct BalanceChangedEvent
    {
        private readonly float newBalance;
        private readonly float delta;

        public float NewBalance
        {
            get { return newBalance; }
        }

        public float Delta
        {
            get { return delta; }
        }

        public BalanceChangedEvent(float newBalance, float delta)
        {
            this.newBalance = newBalance;
            this.delta = delta;
        }
    }
}
