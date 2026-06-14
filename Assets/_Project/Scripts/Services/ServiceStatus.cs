namespace PawsAndCare.Services
{
    /// <summary>
    /// Lifecycle states of a ServiceSession.
    /// </summary>
    public enum ServiceStatus
    {
        QUEUED = 0,
        IN_PROGRESS = 1,
        PAUSED = 2,
        COMPLETED = 3,
        FAILED = 4
    }
}
