namespace PawsAndCare.Core
{
    /// <summary>
    /// Phases of an in-game day, driven by DayManager. PreOpen is the between-days planning beat;
    /// Morning/Midday/Afternoon are the open hours (customers accepted, wave intensity varies);
    /// Closing stops accepting new customers while in-progress services finish; Closed triggers the
    /// end-of-day summary.
    /// </summary>
    public enum DayPhase
    {
        PRE_OPEN = 0,
        MORNING = 1,
        MIDDAY = 2,
        AFTERNOON = 3,
        CLOSING = 4,
        CLOSED = 5
    }
}
