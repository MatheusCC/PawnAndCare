using UnityEngine;

namespace PawsAndCare.Workers
{
    /// <summary>
    /// Spawns worker instances from a prefab at designated positions during boot.
    /// </summary>
    public class WorkerSpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject workerPrefab = null;
        [SerializeField]
        private Transform spawnPosition = null;

        /// <summary>
        /// Instantiates the worker prefab at the configured spawn position.
        /// Called by GameManager after the facility + NavMesh are built so the agent lands on a valid mesh.
        /// </summary>
        public void Spawn()
        {
            if (workerPrefab != null)
            {
                Instantiate(workerPrefab, spawnPosition.position, Quaternion.identity, transform);
            }
            else
            {
                Debug.LogWarning("[WorkerSpawner] Worker prefab is missing — assign one in the inspector.", this);
            }
        }
    }
}
