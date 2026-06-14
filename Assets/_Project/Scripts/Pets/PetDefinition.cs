using UnityEngine;
using PawsAndCare.Services;

namespace PawsAndCare.Pets
{
    /// <summary>
    /// Customer pet template: species, size, temperament, the service it arrives wanting,
    /// how long it will wait in the queue, and its visual.
    /// </summary>
    [CreateAssetMenu(fileName = "Pet_", menuName = "PawsAndCare/Pets/Pet Definition")]
    public class PetDefinition : ScriptableObject
    {
        [SerializeField]
        private PetSpecies species = PetSpecies.DOG;

        [SerializeField]
        private PetSize size = PetSize.MEDIUM;

        [SerializeField]
        private Temperament temperament = Temperament.CALM;

        [SerializeField]
        [Tooltip("The service this pet arrives wanting. Drives dispatcher matching against stations.")]
        private ServiceType desiredService = ServiceType.BATHING;

        [SerializeField]
        [Tooltip("Seconds the pet will wait in the queue before leaving unhappy.")]
        private float patience = 30.0f;

        [SerializeField]
        [Tooltip("Optional. Visual prefab/model spawned for this pet.")]
        private GameObject visualPrefab = null;

        public PetSpecies Species
        {
            get { return species; }
        }

        public PetSize Size
        {
            get { return size; }
        }

        public Temperament Temperament
        {
            get { return temperament; }
        }

        public ServiceType DesiredService
        {
            get { return desiredService; }
        }

        public float Patience
        {
            get { return patience; }
        }

        public GameObject VisualPrefab
        {
            get { return visualPrefab; }
        }
    }
}
