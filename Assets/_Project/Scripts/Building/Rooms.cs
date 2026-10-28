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

    // A logical grouping of grid cells — tracks its identifier, type, member cells,
    // and the objects placed within it.
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

        // Adds a cell coordinate to this room's cell list.
        // Does not update GridSystem state — use GridSystem.CreateRoom or AddCellToRoom for that.
        public void AddCell(Vector2Int cell)
        {
            cells.Add(cell);
        }

        // Removes a cell coordinate from this room's cell list. No-op if not present.
        public void RemoveCell(Vector2Int cell)
        {
            cells.Remove(cell);
        }

        // Tracks a placed object as belonging to this room.
        public void AddPlacedObject(GameObject obj)
        {
            placedObjects.Add(obj);
        }

        // Untracks a placed object from this room. No-op if not present.
        public void RemovePlacedObject(GameObject obj)
        {
            placedObjects.Remove(obj);
        }
    }
}