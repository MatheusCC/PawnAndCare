using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

namespace PawsAndCare.Building
{
    /// <summary>
    /// Boot-time facility orchestrator. Registers scene-placed RoomMarkers with the GridSystem
    /// and bakes the NavMesh. All level geometry (base floor, room floors, stations) is authored
    /// in the scene rather than generated or spawned here.
    /// </summary>
    public class FacilityBuilder : MonoBehaviour
    {
        [SerializeField]
        private GridSystem gridSystem = null;
        [SerializeField]
        private NavMeshSurface navMeshSurface = null;
        [SerializeField]
        [Tooltip("Scene-placed RoomMarkers to register with the GridSystem at boot. Drag each room's GameObject into this list.")]
        private List<RoomMarker> roomMarkers = new List<RoomMarker>();

        /// <summary>
        /// Registers scene rooms with the GridSystem and bakes the NavMesh.
        /// Called by GameManager during boot so ordering vs. other systems (worker spawn) is deterministic.
        /// </summary>
        public void Build()
        {
            if (gridSystem != null)
            {
                RegisterRooms();

                // Bake the NavMesh after rooms are registered. Scene-placed station prefabs already
                // carry the NavMeshModifier components that mark their cells unwalkable.
                if (navMeshSurface != null)
                {
                    navMeshSurface.BuildNavMesh();
                }
                else
                {
                    Debug.LogWarning("[FacilityBuilder] NavMeshSurface reference is missing — assign one in the inspector.", this);
                }
            }
            else
            {
                Debug.LogError("[FacilityBuilder] GridSystem reference is missing — assign one in the inspector.", this);
            }
        }

        private void RegisterRooms()
        {
            for (int i = 0; i < roomMarkers.Count; i++)
            {
                RoomMarker marker = roomMarkers[i];

                if (marker != null)
                {
                    Room room = gridSystem.CreateRoom(marker.RoomType, marker.GetCells());

                    if (room != null)
                    {
                        // Treat the marker's GameObject (typically the room's floor) as the room's anchor object.
                        room.AddPlacedObject(marker.gameObject);
                    }
                }
                else
                {
                    Debug.LogWarning($"[FacilityBuilder] roomMarkers[{i}] is null — assign a RoomMarker in the inspector or remove the empty slot.", this);
                }
            }
        }
    }
}
