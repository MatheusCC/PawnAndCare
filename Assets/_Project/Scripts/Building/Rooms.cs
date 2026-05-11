using System.Collections.Generic;
using UnityEngine;

namespace PawsAndCare.Building
{
    public enum RoomType
    {
        NONE = 0,
        RECEPTION = 1,
        BATHING_STATION = 2,
        GROOMING_ROOM = 3,
        VET_ROOM = 4,
        DAYCARE_YARD = 5,
        STAFF_ROOM = 6,
        STORAGE = 7
    }

    /// <summary>
    /// A logical grouping of grid cells. Tracks its identifier, type, member cells,
    /// and the objects placed within it. Cells and placed objects can be added or
    /// removed after construction to allow layout flexibility.
    /// </summary>
    public class Room
    {
        private readonly int roomId;
        private readonly RoomType roomType;
        private readonly List<Vector2Int> cells;
        private readonly List<GameObject> placedObjects;

        public int RoomId { get { return roomId; } }
        public RoomType RoomType { get { return roomType; } }
        public List<Vector2Int> Cells { get { return cells; } }
        public List<GameObject> PlacedObjects { get { return placedObjects; } }

        public Room(int roomId, RoomType roomType)
        {
            this.roomId = roomId;
            this.roomType = roomType;
            this.cells = new List<Vector2Int>();
            this.placedObjects = new List<GameObject>();
        }

        /// <summary>
        /// Adds a cell coordinate to this room's cell list.
        /// Does not update GridSystem state — use GridSystem.CreateRoom or AddCellToRoom for that.
        /// </summary>
        public void AddCell(Vector2Int cell)
        {
            cells.Add(cell);
        }

        /// <summary>
        /// Removes a cell coordinate from this room's cell list. No-op if not present.
        /// </summary>
        public void RemoveCell(Vector2Int cell)
        {
            cells.Remove(cell);
        }

        /// <summary>
        /// Tracks a placed object as belonging to this room.
        /// </summary>
        public void AddPlacedObject(GameObject obj)
        {
            placedObjects.Add(obj);
        }

        /// <summary>
        /// Untracks a placed object from this room. No-op if not present.
        /// </summary>
        public void RemovePlacedObject(GameObject obj)
        {
            placedObjects.Remove(obj);
        }
    }
}