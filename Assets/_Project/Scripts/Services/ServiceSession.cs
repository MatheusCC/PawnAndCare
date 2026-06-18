using PawsAndCare.Pets;
using PawsAndCare.Workers;

namespace PawsAndCare.Services
{
    /// <summary>
    /// One run of a service: which service, at which station, by which worker, for which pet.
    /// Owns progress and the final quality result. Progress is advanced externally
    /// (by ServiceManager) so the session itself stays a pure data + math object.
    /// </summary>
    public class ServiceSession
    {
        // Quality at 0 skill is BASE_QUALITY * MIN_SKILL_QUALITY_FACTOR; at full skill it is BASE_QUALITY.
        private const float BASE_QUALITY = 1.0f;
        private const float MIN_SKILL_QUALITY_FACTOR = 0.5f;

        private readonly int sessionId;
        private readonly ServiceData service;
        private readonly ServiceStation station;
        private readonly Worker worker;
        private readonly PetStateMachine pet;
        private float progress;
        private float quality;
        private ServiceStatus status;

        public int SessionId { get { return sessionId; } }
        public ServiceData Service { get { return service; } }
        public ServiceStation Station { get { return station; } }
        public Worker Worker { get { return worker; } }
        public PetStateMachine Pet { get { return pet; } }
        public float Progress { get { return progress; } }
        public float Quality { get { return quality; } }
        public ServiceStatus Status { get { return status; } }

        public ServiceSession(int sessionId, ServiceData service, ServiceStation station, Worker worker, PetStateMachine pet)
        {
            this.sessionId = sessionId;
            this.service = service;
            this.station = station;
            this.worker = worker;
            this.pet = pet;
            progress = 0.0f;
            quality = 0.0f;
            status = ServiceStatus.IN_PROGRESS;
        }

        /// <summary>
        /// Advances progress by the elapsed time. Marks the session Completed (and locks in
        /// quality) once progress reaches 1.0. No-op unless the session is in progress.
        /// </summary>
        public void UpdateProgress(float deltaTime)
        {
            if (status == ServiceStatus.IN_PROGRESS)
            {
                if (service.BaseDuration > 0.0f)
                {
                    progress += deltaTime / service.BaseDuration;
                }
                else
                {
                    progress = 1.0f;
                }

                if (progress >= 1.0f)
                {
                    progress = 1.0f;
                    quality = CalculateQuality();
                    status = ServiceStatus.COMPLETED;
                }
            }
        }

        // Phase 1 quality: base value scaled by the worker's skill for this service type.
        private float CalculateQuality()
        {
            float skill = worker.GetSkillRating(service.ServiceType);
            float result = BASE_QUALITY * (MIN_SKILL_QUALITY_FACTOR + ((1.0f - MIN_SKILL_QUALITY_FACTOR) * skill));
            return result;
        }
    }
}
