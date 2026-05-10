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
        private float cellSize = 1f;

        private GridCell[,] cells;

        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public float CellSize { get { return cellSize; } }

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
            bool available = false;
            GridCell cell = GetCell(position);

            if (cell != null && !cell.IsOccupied && cell.IsWalkable)
            {
                available = true;
            }

            return available;
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
        }
    }
}