using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace PawsAndCare.Building
{
    /// <summary>
    /// Inspector-editable rectangle describing a placeholder room: its type, origin
    /// grid coordinate, size in cells, and floor color.
    /// </summary>
    [System.Serializable]
    public class RoomLayout
    {
        [SerializeField]
        private RoomType type;
        [SerializeField]
        private Vector2Int origin;
        [SerializeField]
        private Vector2Int size;
        [SerializeField]
        private Color floorColor;

        public RoomType Type { get { return type; } }
        public Vector2Int Origin { get { return origin; } }
        public Vector2Int Size { get { return size; } }
        public Color FloorColor { get { return floorColor; } }

        public RoomLayout(RoomType type, Vector2Int origin, Vector2Int size, Color floorColor)
        {
            this.type = type;
            this.origin = origin;
            this.size = size;
            this.floorColor = floorColor;
        }
    }

    /// <summary>
    /// Inspector-editable description of a placeholder service station: its label,
    /// grid cell, and color. Spawned as a cube sitting on top of the room floor.
    /// </summary>
    [System.Serializable]
    public class StationPlaceholder
    {
        [SerializeField]
        private string label;
        [SerializeField]
        private Vector2Int gridPosition;
        [SerializeField]
        private Color color;

        public string Label { get { return label; } }
        public Vector2Int GridPosition { get { return gridPosition; } }
        public Color Color { get { return color; } }

        public StationPlaceholder(string label, Vector2Int gridPosition, Color color)
        {
            this.label = label;
            this.gridPosition = gridPosition;
            this.color = color;
        }
    }

    /// <summary>
    /// Builds the Phase 1 placeholder facility at scene start (base floor, room floors,
    /// station cubes) using inspector-driven RoomLayouts and StationPlaceholders.
    /// </summary>
    // Runs in Start so GridSystem.Awake has finished allocating cells before we
    // ask for them.
    public class FacilityBuilder : MonoBehaviour
    {
        [SerializeField]
        private GridSystem gridSystem = null;
        [SerializeField]
        private NavMeshSurface navMeshSurface = null;
        [SerializeField]
        private float floorThickness = 0.1f;
        [SerializeField]
        private Color baseFloorColor = new Color(1.0f, 0.97f, 0.94f); // Cream White (Art Bible)
        [SerializeField]
        private List<RoomLayout> roomLayouts = new List<RoomLayout>
        {
            new RoomLayout(RoomType.RECEPTION,       new Vector2Int(0, 0),  new Vector2Int(5, 4), new Color(0.96f, 0.76f, 0.42f)), // Honey Gold
            new RoomLayout(RoomType.BATHING_STATION, new Vector2Int(6, 0),  new Vector2Int(4, 4), new Color(0.49f, 0.71f, 0.84f)), // Sky Blue
            new RoomLayout(RoomType.GROOMING_ROOM,   new Vector2Int(11, 0), new Vector2Int(4, 4), new Color(0.83f, 0.41f, 0.55f)), // Petal Pink
        };
        [SerializeField]
        private List<StationPlaceholder> stationPlaceholders = new List<StationPlaceholder>
        {
            new StationPlaceholder("BathTub",       new Vector2Int(7, 1),  new Color(0.30f, 0.50f, 0.65f)),
            new StationPlaceholder("GroomingTable", new Vector2Int(12, 1), new Color(0.65f, 0.25f, 0.40f)),
        };

        private void Start()
        {
            if (gridSystem != null)
            {
                BuildBaseFloor();

                foreach (RoomLayout layout in roomLayouts)
                {
                    BuildRoom(layout);
                }

                foreach (StationPlaceholder station in stationPlaceholders)
                {
                    BuildStation(station);
                }

                // Bake the NavMesh at runtime after all geometry is spawned.
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

        private void BuildBaseFloor()
        {
            // Find the world-space center of the full grid by averaging the near and
            // far cell centers. GridToWorld returns the CENTER of the named cell, so
            // averaging (0,0) and (W-1, H-1) gives the midpoint of the whole grid.
            Vector3 nearWorld = gridSystem.GridToWorld(new Vector2Int(0, 0));
            Vector3 farWorld = gridSystem.GridToWorld(new Vector2Int(gridSystem.Width - 1, gridSystem.Height - 1));
            Vector3 center = (nearWorld + farWorld) * 0.5f;

            // Sit the base floor 1cm below the room-floor tops to prevent z-fighting.
            // Half a floor thickness drops the cube's center to that level (cube's
            // local center is at half-height).
            float offsetBelow = 0.01f;
            float baseCenterY = center.y - offsetBelow - (floorThickness * 0.5f);

            GameObject baseFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseFloor.name = "BaseFloor";
            baseFloor.transform.position = new Vector3(center.x, baseCenterY, center.z);
            // Scale spans the full grid extent — width × thickness × height.
            baseFloor.transform.localScale = new Vector3(
                gridSystem.Width * gridSystem.CellSize,
                floorThickness,
                gridSystem.Height * gridSystem.CellSize);
            baseFloor.transform.SetParent(transform);

            Renderer renderer = baseFloor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = baseFloorColor;
            }
        }

        private void BuildRoom(RoomLayout layout)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            for (int x = 0; x < layout.Size.x; x++)
            {
                for (int y = 0; y < layout.Size.y; y++)
                {
                    cells.Add(layout.Origin + new Vector2Int(x, y));
                }
            }

            Room room = gridSystem.CreateRoom(layout.Type, cells);
            BuildFloor(layout, room);
        }

        private void BuildFloor(RoomLayout layout, Room room)
        {
            // Center of the room = midpoint of the near-corner cell and the far-corner
            // cell. We subtract (1,1) from origin+size because Size counts cells (not
            // indices), so the last cell's index is origin + size - 1.
            Vector3 nearCorner = gridSystem.GridToWorld(layout.Origin);
            Vector3 farCorner = gridSystem.GridToWorld(layout.Origin + layout.Size - new Vector2Int(1, 1));
            Vector3 center = (nearCorner + farCorner) * 0.5f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = $"Floor_{layout.Type}_{room.RoomId}";
            // Drop center Y by half-thickness so the cube's TOP sits exactly at the
            // grid plane. Visually the room appears flush with grid level.
            floor.transform.position = new Vector3(center.x, center.y - (floorThickness * 0.5f), center.z);
            // Scale spans room width × thickness × room depth in world units.
            floor.transform.localScale = new Vector3(
                layout.Size.x * gridSystem.CellSize,
                floorThickness,
                layout.Size.y * gridSystem.CellSize);
            floor.transform.SetParent(transform);

            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = layout.FloorColor;
            }

            room.AddPlacedObject(floor);
        }

        private void BuildStation(StationPlaceholder station)
        {
            // Place the station at the world-space center of its grid cell.
            Vector3 cellCenter = gridSystem.GridToWorld(station.GridPosition);
            // Cube height = 80% of a cell so it reads as "a thing on the floor",
            // not a column that fills the cell.
            float height = gridSystem.CellSize * 0.8f;

            GameObject stationObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stationObj.name = $"Station_{station.Label}";
            // Raise the center by half-height so the cube's BOTTOM sits on the floor
            // (the grid plane), not buried halfway into it.
            stationObj.transform.position = new Vector3(
                cellCenter.x,
                cellCenter.y + (height * 0.5f),
                cellCenter.z);
            // Footprint is 70% of a cell — gives a small visible margin around the
            // station so the room floor color reads underneath.
            stationObj.transform.localScale = new Vector3(
                gridSystem.CellSize * 0.7f,
                height,
                gridSystem.CellSize * 0.7f);
            stationObj.transform.SetParent(transform);

            Renderer renderer = stationObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = station.Color;
            }

            // Mark station as a non-walkable obstacle for pathfinding.
            NavMeshModifier modifier = stationObj.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = NavMesh.GetAreaFromName("Not Walkable");

            GridCell cell = gridSystem.GetCell(station.GridPosition);

            if (cell != null)
            {
                cell.SetOccupied(stationObj);
                Room room = gridSystem.GetRoomById(cell.RoomId);

                if (room != null)
                {
                    room.AddPlacedObject(stationObj);
                }
            }
            else
            {
                Debug.LogWarning($"[FacilityBuilder] Station '{station.Label}' at {station.GridPosition} is out of bounds.", this);
            }
        }
    }
}