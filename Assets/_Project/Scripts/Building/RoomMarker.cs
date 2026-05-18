using System.Collections.Generic;
using UnityEngine;

namespace PawsAndCare.Building
{
    /// <summary>
    /// Scene-placed marker that registers its GameObject as a room with the GridSystem at boot.
    /// Attach to a room's floor GameObject and configure the grid origin and size in cells.
    /// </summary>
    public class RoomMarker : MonoBehaviour
    {
        [SerializeField]
        private RoomType roomType = RoomType.RECEPTION;

        [SerializeField]
        [Tooltip("Bottom-left grid cell of this room (inclusive).")]
        private Vector2Int origin = new Vector2Int(0, 0);

        [SerializeField]
        [Tooltip("Room size in cells (width × height).")]
        private Vector2Int size = new Vector2Int(1, 1);

        public RoomType RoomType
        {
            get { return roomType; }
        }

        public Vector2Int Origin
        {
            get { return origin; }
        }

        public Vector2Int Size
        {
            get { return size; }
        }

        /// <summary>
        /// Returns the list of grid cells this room occupies, computed from origin + size.
        /// </summary>
        public List<Vector2Int> GetCells()
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    cells.Add(origin + new Vector2Int(x, y));
                }
            }

            return cells;
        }

        // OnDrawGizmos draws a wireframe rectangle in the scene view showing which grid cells the
        // marker claims. Editor-only callback (Unity strips it from builds), and FindFirstObjectByType
        // is acceptable here because gizmos only render in the editor — never in the runtime hot path.
        private void OnDrawGizmos()
        {
            GridSystem grid = FindFirstObjectByType<GridSystem>();

            if (grid != null)
            {
                // GridToWorld returns the CENTER of each cell, so the midpoint of the near and far
                // corner-cell centers is the room's world-space center; world size = cells × cellSize.
                Vector3 nearWorld = grid.GridToWorld(origin);
                Vector3 farWorld = grid.GridToWorld(origin + size - new Vector2Int(1, 1));
                Vector3 center = (nearWorld + farWorld) * 0.5f;
                Vector3 worldSize = new Vector3(size.x * grid.CellSize, 0.05f, size.y * grid.CellSize);

                Gizmos.color = new Color(0.0f, 0.85f, 1.0f, 0.9f);
                Gizmos.DrawWireCube(center, worldSize);
            }
        }
    }
}
