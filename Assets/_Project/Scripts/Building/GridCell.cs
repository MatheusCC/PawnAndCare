using UnityEngine;

namespace PawsAndCare.Building
{
    /// <summary>
    /// Single cell in the grid — owns its coordinate, occupancy, room assignment,
    /// and walkability. Position is fixed at construction; mutate the rest via setters.
    /// </summary>
    public class GridCell
    {
        private readonly Vector2Int position;
        private bool isOccupied;
        private GameObject occupiedBy;
        private int roomId;
        private bool isWalkable;

        public Vector2Int Position { get { return position; } }
        public bool IsOccupied { get { return isOccupied; } }
        public GameObject OccupiedBy { get { return occupiedBy; } }
        public int RoomId { get { return roomId; } }
        public bool IsWalkable { get { return isWalkable; } }

        public GridCell(Vector2Int position)
        {
            this.position = position;
            this.isOccupied = false;
            this.occupiedBy = null;
            this.roomId = 0;
            this.isWalkable = true;
        }

        /// <summary>
        /// Sets the occupant. Pass null to free the cell.
        /// </summary>
        public void SetOccupied(GameObject occupant)
        {
            occupiedBy = occupant;
            isOccupied = (occupant != null);
        }

        /// <summary>
        /// Assigns this cell to a room. Use 0 for "unassigned".
        /// </summary>
        public void SetRoomId(int newRoomId)
        {
            roomId = newRoomId;
        }

        /// <summary>
        /// Marks the cell as walkable or non-walkable for pathfinding.
        /// </summary>
        public void SetWalkable(bool walkable)
        {
            isWalkable = walkable;
        }
    }
}