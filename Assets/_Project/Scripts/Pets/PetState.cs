namespace PawsAndCare.Pets
{
    /// <summary>
    /// Lifecycle states of a customer pet, driven by PetStateMachine.
    /// </summary>
    public enum PetState
    {
        ARRIVING = 0,
        QUEUING = 1,
        MOVING_TO_STATION = 2,
        BEING_SERVICED = 3,
        LEAVING = 4,
        DESPAWNING = 5
    }
}
