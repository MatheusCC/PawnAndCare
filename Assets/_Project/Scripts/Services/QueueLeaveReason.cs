namespace PawsAndCare.Services
{
    /// <summary>
    /// Why a pet left the reception queue. Consumed by reputation (Task 9) and future UI.
    /// </summary>
    public enum QueueLeaveReason
    {
        DISPATCHED = 0,   // pulled into service by ServiceDispatcher (good outcome)
        ABANDONED = 1     // patience ran out while waiting (reputation penalty)
    }
}
