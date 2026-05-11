using System.Collections.Generic;
using UnityEngine;

namespace PawsAndCare.Building
{
    /// <summary>
    /// Owns the 2D grid of cells used for placement, room assignment, and pathfinding.
    /// Coordinates: grid (x, y) maps to world (X, Z) relative to this transform's position.
    /// Grid (0, 0) is at transform.position; cell centers are offset by cellSize/2.
    /// </summary>
    public class GridSystem : MonoBehaviour
    {
        [SerializeField]
        private int width = 20;
        [SerializeField]
        private int height = 20;
        [SerializeField]
        private float cellSize = 1.0f;
        [SerializeField]
        private Color occupiedGizmoColor = new Color(0.88f, 0.44f, 0.33f, 0.4f);
        [SerializeField]
        private Color unwalkableGizmoColor = new Color(0.18f, 0.20f, 0.21f, 0.4f);

        private GridCell[,] cells;
        private readonly List<Room> rooms = new List<Room>();
        private int nextRoomId = 1;

        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public float CellSize { get { return cellSize; } }
        public List<Room> Rooms { get { return rooms; } }

        private void Awake()
        {
            cells = new GridCell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y] = new GridCell(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>
        /// Returns the cell at the given grid position, or null if out of bounds.
        /// </summary>
        public GridCell GetCell(Vector2Int position)
        {
            GridCell result = null;

            if (IsInBounds(position))
            {
                result = cells[position.x, position.y];
            }

            return result;
        }

        /// <summary>
        /// Converts a grid coordinate to the world-space center of that cell.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float worldX = transform.position.x + (gridPos.x * cellSize) + (cellSize * 0.5f);
            float worldZ = transform.position.z + (gridPos.y * cellSize) + (cellSize * 0.5f);
            float worldY = transform.position.y;
            return new Vector3(worldX, worldY, worldZ);
        }

        /// <summary>
        /// Converts a world position to the grid coordinate it falls within.
        /// May return out-of-bounds coordinates; use GetCell for safe lookup.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float localX = worldPos.x - transform.position.x;
            float localZ = worldPos.z - transform.position.z;
            int gridX = Mathf.FloorToInt(localX / cellSize);
            int gridY = Mathf.FloorToInt(localZ / cellSize);
            return new Vector2Int(gridX, gridY);
        }

        /// <summary>
        /// True when the cell exists, is not occupied, and is walkable.
        /// </summary>
        public bool IsCellAvailable(Vector2Int position)
        {
            GridCell cell = GetCell(position);

            bool available = cell != null && !cell.IsOccupied && cell.IsWalkable;

            return available;
        }

        /// <summary>
        /// Creates a new room of the given type and assigns the listed cells to it.
        /// Cells that are out of bounds or already assigned to another room are
        /// skipped with a warning. Returns the created Room.
        /// </summary>
        public Room CreateRoom(RoomType type, List<Vector2Int> cellsToAssign)
        {
            Room room = new Room(nextRoomId, type);
            nextRoomId++;

            foreach (Vector2Int cellPos in cellsToAssign)
            {
                GridCell cell = GetCell(cellPos);

                if (cell != null)
                {          
                    if (cell.RoomId == 0)
                    {
                        cell.SetRoomId(room.RoomId);
                        room.AddCell(cellPos);     
                    }
                    else
                    {
                        Debug.LogWarning($"[GridSystem] CreateRoom: cell {cellPos} already belongs to room {cell.RoomId}, skipping.", this);
                    }              
                }
                else
                {
                    Debug.LogWarning($"[GridSystem] CreateRoom: cell {cellPos} is out of bounds, skipping.", this);
                }              
            }

            rooms.Add(room);
            return room;
        }

        /// <summary>
        /// Returns the room with the given ID, or null if not found.
        /// </summary>
        public Room GetRoomById(int id)
        {
            Room result = null;

            foreach (Room room in rooms)
            {
                if (room.RoomId == id)
                {
                    result = room;
                }
            }

            return result;
        }

        private bool IsInBounds(Vector2Int position)
        {
            return position.x >= 0
                && position.x < width
                && position.y >= 0
                && position.y < height;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Vector3 origin = transform.position;

            for (int x = 0; x <= width; x++)
            {
                Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
                Vector3 end = origin + new Vector3(x * cellSize, 0f, height * cellSize);
                Gizmos.DrawLine(start, end);
            }

            for (int y = 0; y <= height; y++)
            {
                Vector3 start = origin + new Vector3(0f, 0f, y * cellSize);
                Vector3 end = origin + new Vector3(width * cellSize, 0f, y * cellSize);
                Gizmos.DrawLine(start, end);
            }

            if (cells != null)
            {
                Vector3 overlaySize = new Vector3(cellSize * 0.9f, 0.05f, cellSize * 0.9f);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        GridCell cell = cells[x, y];

                        if (cell.IsOccupied || !cell.IsWalkable)
                        {
                            if (cell.IsOccupied)
                            {
                                Gizmos.color = occupiedGizmoColor;
                            }
                            else
                            {
                                Gizmos.color = unwalkableGizmoColor;
                            }

                            Vector3 cellCenter = GridToWorld(new Vector2Int(x, y));
                            Gizmos.DrawCube(cellCenter, overlaySize);
                        }
                    }
                }
            }
        }
    }
}