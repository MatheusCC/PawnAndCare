namespace PawsAndCare.Services
{
    /// <summary>
    /// Published by ServiceStation when it is selected through the interaction layer.
    /// Lets player-control systems (e.g. AgentController) react without ServiceStation
    /// needing any knowledge of workers or selection state.
    /// </summary>
    public readonly struct StationSelectedEvent
    {
        private readonly ServiceStation station;

        public ServiceStation Station
        {
            get { return station; }
        }

        public StationSelectedEvent(ServiceStation station)
        {
            this.station = station;
        }
    }
}
