using UnityEngine;

namespace PawsAndCare.Services
{
    /// <summary>
    /// Service template: type, duration, price, and required station type.
    /// </summary>
    [CreateAssetMenu(fileName = "Service_", menuName = "PawsAndCare/Services/Service Definition")]
    public class ServiceDefinition : ScriptableObject
    {
        [SerializeField]
        private ServiceType serviceType = ServiceType.GROOMING;

        [SerializeField]
        [Tooltip("Service duration in seconds")]
        private float baseDuration = 60.0f;

        [SerializeField]
        [Tooltip("Base price in dollars")]
        private float basePrice = 25.0f;

        [SerializeField]
        [Tooltip("Type of station required to perform this service")]
        private string requiredStationType = "GroomingTable";

        [SerializeField]
        [TextArea(2, 4)]
        private string description = "";

        public ServiceType ServiceType
        {
            get { return serviceType; }
        }

        public float BaseDuration
        {
            get { return baseDuration; }
        }

        public float BasePrice
        {
            get { return basePrice; }
        }

        public string RequiredStationType
        {
            get { return requiredStationType; }
        }

        public string Description
        {
            get { return description; }
        }
    }
}
