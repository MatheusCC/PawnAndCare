namespace PawsAndCare.Core
{
    /// <summary>
    /// Published by DayManager when a new in-game day begins (start of its PreOpen phase).
    /// </summary>
    public readonly struct DayStartedEvent
    {
        private readonly int dayNumber;

        public int DayNumber
        {
            get { return dayNumber; }
        }

        public DayStartedEvent(int dayNumber)
        {
            this.dayNumber = dayNumber;
        }
    }

    /// <summary>
    /// Published by DayManager when the day's open hours end (it enters Closed). A pure day-boundary
    /// signal — consumers read their own daily stats (e.g. EconomyManager.DailyRevenue) to build the
    /// end-of-day summary, keeping DayManager decoupled from those feature systems.
    /// </summary>
    public readonly struct DayEndedEvent
    {
        private readonly int dayNumber;

        public int DayNumber
        {
            get { return dayNumber; }
        }

        public DayEndedEvent(int dayNumber)
        {
            this.dayNumber = dayNumber;
        }
    }

    /// <summary>
    /// Published by DayManager on every day-phase transition. Systems gate behaviour on the new
    /// phase (e.g. CustomerSpawner spawns only while customers are accepted).
    /// </summary>
    public readonly struct DayPhaseChangedEvent
    {
        private readonly DayPhase previousPhase;
        private readonly DayPhase currentPhase;

        public DayPhase PreviousPhase
        {
            get { return previousPhase; }
        }

        public DayPhase CurrentPhase
        {
            get { return currentPhase; }
        }

        public DayPhaseChangedEvent(DayPhase previousPhase, DayPhase currentPhase)
        {
            this.previousPhase = previousPhase;
            this.currentPhase = currentPhase;
        }
    }

    /// <summary>
    /// Published by DayManager when the in-game clock advances to a new minute (not every frame), so
    /// clock displays can update event-driven without per-frame polling.
    /// </summary>
    public readonly struct GameMinuteChangedEvent
    {
        private readonly int hour;
        private readonly int minute;

        public int Hour
        {
            get { return hour; }
        }

        public int Minute
        {
            get { return minute; }
        }

        public GameMinuteChangedEvent(int hour, int minute)
        {
            this.hour = hour;
            this.minute = minute;
        }
    }
}
