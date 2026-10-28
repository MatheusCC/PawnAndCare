using PawsAndCare.Services;

namespace PawsAndCare.Core
{
    // Published by ServiceManager when a new service session begins.
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

    // Published by ServiceManager when a service session reaches completion.
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
