using PawsAndCare.Services;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Published by ServiceManager when a new service session begins.
    /// </summary>
    public readonly struct ServiceStartedEvent
    {
        private readonly ServiceSession session;

        public ServiceSession Session
        {
            get { return session; }
        }

        public ServiceStartedEvent(ServiceSession session)
        {
            this.session = session;
        }
    }

    /// <summary>
    /// Published by ServiceManager when a service session reaches completion.
    /// </summary>
    public readonly struct ServiceCompletedEvent
    {
        private readonly ServiceSession session;
        private readonly float quality;

        public ServiceSession Session
        {
            get { return session; }
        }

        public float Quality
        {
            get { return quality; }
        }

        public ServiceCompletedEvent(ServiceSession session, float quality)
        {
            this.session = session;
            this.quality = quality;
        }
    }
}
