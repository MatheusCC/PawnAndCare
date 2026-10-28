namespace PawsAndCare.Services
{
    // Why a pet left the reception queue. Consumed by ReputationManager.
    public enum QueueLeaveReason
    {
        DISPATCHED = 0,   // pulled into service by ServiceDispatcher (good outcome)
        ABANDONED = 1     // patience ran out while waiting (reputation penalty)
    }
}
