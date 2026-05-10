namespace PawsAndCare.Core
{
    /// <summary>
    /// Published by DayManager when a new in-game day begins.
    /// </summary>
    public readonly struct DayStartedEvent
    {
        private readonly int dayNumber;

        public int DayNumber { get { return dayNumber; } }

        public DayStartedEvent(int dayNumber)
        {
            this.dayNumber = dayNumber;
        }
    }

    /// <summary>
    /// Published by DayManager when an in-game day ends.
    /// </summary>
    public readonly struct DayEndedEvent
    {
        private readonly int dayNumber;
        private readonly float totalRevenue;

        public int DayNumber { get { return dayNumber; } }
        public float TotalRevenue { get { return totalRevenue; } }

        public DayEndedEvent(int dayNumber, float totalRevenue)
        {
            this.dayNumber = dayNumber;
            this.totalRevenue = totalRevenue;
        }
    }
}