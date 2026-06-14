# Paws & Care — Phase 1: Foundation

**Goal:** Player can select a Worker, assign it to a service station, and watch the service complete.

**Pivot from original plan:** Workers (player-controlled agents) come first; Pets (autonomous AI customers) are deferred to Phase 2. The original "place a pet at a bathing station" goal is reframed around the Worker since the player will never directly control a Pet long-term.

**Estimated Tasks:** 12 main tasks

---

## Task 1 — Unity Project Setup `[DONE]`

- [X] 1.1 Create new Unity 6 project with URP template
- [X] 1.2 Configure URP settings (shadow cascade, anti-aliasing MSAA 4x, color space Linear)
- [X] 1.3 Create project folder structure as defined in TDD Section 17
- [X] 1.4 Create scene files: PersistentScene, GameplayScene, UIScene, MainMenuScene
- [X] 1.5 Initialize Git repository with proper `.gitignore` (include `Docs/*`, `!Docs/README.md`)
- [X] 1.6 Create `Docs/` folder with README.md and copy GDD, TDD, Art Bible locally
- [X] 1.7 Install required packages (Newtonsoft JSON, UI Toolkit if not included)
- [X] 1.8 First commit: "Initial project setup"

---

## Task 2 — Singleton Base & GameManager `[DONE]`

### 2A — Singleton Base Class
- [X] 2A.1 Create generic `Singleton<T> : MonoBehaviour` base class in `Scripts/Core/`
- [X] 2A.2 Implement `DontDestroyOnLoad` persistence
- [X] 2A.3 Implement duplicate prevention (destroy extra instances)
- [X] 2A.4 Add `Instance` property with lazy initialization
- [X] 2A.5 Add virtual `OnInitialize()` method for subclass setup

### 2B — GameManager
- [X] 2B.1 Create `GameManager : Singleton<GameManager>` in `Scripts/Core/`
- [X] 2B.2 Define `GameState` enum: MainMenu, Playing, Paused, Loading
- [X] 2B.3 Implement state transitions with `ChangeState(GameState newState)`
- [X] 2B.4 Add `OnGameStateChanged` event for other systems to react
- [ ] 2B.5 Set up PersistentScene as the bootstrap scene with GameManager GameObject → `(DEFERRED post-Phase 1)`
- [ ] 2B.6 Implement basic scene loading flow (PersistentScene → loads GameplayScene additively) → `(DEFERRED post-Phase 1)`
- [ ] 2B.7 Test: Play from PersistentScene, verify GameManager persists and GameplayScene loads → `(DEFERRED post-Phase 1)`

> **Note:** GameManager gained a boot-orchestration role in Task 7A (ordered Build → Spawn → state transition).

---

## Task 3 — Event Bus `[DONE]`

### 3A — Core Event Bus Implementation
- [X] 3A.1 Create `EventBus` static class in `Scripts/Core/`
- [X] 3A.2 Implement `Subscribe<T>(Action<T> handler)` method
- [X] 3A.3 Implement `Unsubscribe<T>(Action<T> handler)` method
- [X] 3A.4 Implement `Publish<T>(T eventData)` method
- [X] 3A.5 Use `Dictionary<Type, List<Delegate>>` for handler storage
- [X] 3A.6 Add safety: null checks, prevent duplicate subscriptions, handle exceptions in handlers

### 3B — Initial Event Definitions
- [X] 3B.1 Create `Events/` subfolder in `Scripts/Core/`
- [X] 3B.2 Define `DayStartedEvent` struct: `{ int DayNumber }`
- [X] 3B.3 Define `DayEndedEvent` struct: `{ int DayNumber, float TotalRevenue }`
- [ ] 3B.4 Define `ServiceStartedEvent` struct: `{ ServiceSession Session }` → `(DEFERRED to Task 12)`
- [ ] 3B.5 Define `ServiceCompletedEvent` struct: `{ ServiceSession Session, float Quality }` → `(DEFERRED to Task 12)`
- [X] 3B.6 Define `GameStateChangedEvent` struct: `{ GameState OldState, GameState NewState }`

### 3C — Testing
- [X] 3C.1 Create a simple test MonoBehaviour that subscribes to an event, publishes it, and logs the result
- [X] 3C.2 Verify subscribe, publish, and unsubscribe all work correctly
- [X] 3C.3 Verify multiple subscribers receive the same event
- [X] 3C.4 Clean up test script after validation

---

## Task 4 — Grid System & Basic Facility `[DONE]`

### 4A — Grid System Core
- [X] 4A.1 Create `GridCell` class in `Scripts/Building/`: position (Vector2Int), isOccupied, occupiedBy, roomId, isWalkable
- [X] 4A.2 Create `GridSystem : MonoBehaviour` in `Scripts/Building/`
- [X] 4A.3 Implement grid initialization with configurable width, height, cellSize
- [X] 4A.4 Implement `GetCell(Vector2Int position)` and `WorldToGrid(Vector3 worldPos)` conversion methods
- [X] 4A.5 Implement `GridToWorld(Vector2Int gridPos)` conversion method
- [X] 4A.6 Implement `IsCellAvailable(Vector2Int position)` validation
- [X] 4A.7 Add debug visualization: draw grid lines with Gizmos in editor

### 4B — Room Definition
- [X] 4B.1 Create `RoomType` enum: Reception, BathingStation, GroomingRoom, VetRoom, DaycareYard, StaffRoom, Storage
- [X] 4B.2 Create `Room` class: roomId, roomType, cells list, placedObjects list
- [X] 4B.3 Implement `CreateRoom(RoomType type, List<Vector2Int> cells)` in GridSystem
- [X] 4B.4 Assign cells to rooms on creation

### 4C — Placeholder Facility
- [X] 4C.1 Create `FacilityBuilder : MonoBehaviour` that generates the starter layout (renamed from `FacilityBootstrap`)
- [X] 4C.2 Define Reception room (5x4 cells near entrance)
- [X] 4C.3 Define Bathing Station room (4x4 cells)
- [X] 4C.4 Define Grooming Room (4x4 cells)
- [X] 4C.5 Place placeholder geometry (cubes with colored materials) for floors
- [X] 4C.6 Place placeholder service station objects (colored cubes) at grid positions
- [X] 4C.7 Test: verify grid debug visualization matches the physical layout

---

## Task 5 — Isometric Camera Controller `[DONE]`

### 5A — Camera Rig Setup
- [X] 5A.1 Create camera rig hierarchy: CameraRig (empty) → CameraPivot (rotation) → MainCamera
- [X] 5A.2 Set camera to Orthographic projection
- [X] 5A.3 Set initial rotation to isometric angle (~45° Y, ~35° X pitch)
- [X] 5A.4 Configure initial orthographic size (zoom level)
- [X] 5A.5 Create `IsometricCameraController : MonoBehaviour` in `Scripts/Camera/`

### 5B — Camera Movement
- [X] 5B.1 Implement WASD / Arrow key panning with speed scaled by zoom level
- [X] 5B.2 Implement middle mouse button drag panning
- [X] 5B.3 Implement scroll wheel zoom (adjust orthographic size) with min/max limits
- [X] 5B.4 Implement Q/E snap rotation (90° increments) with smooth interpolation
- [X] 5B.5 Add smooth damping to all movement (SmoothDamp or Lerp)

### 5C — Camera Bounds & Polish
- [X] 5C.1 Implement camera bounds (Rect) to prevent panning outside facility area
- [X] 5C.2 Implement `FocusOn(Vector3 worldPosition)` method for snapping camera to a target
- [X] 5C.3 Add edge-of-screen panning (optional, can be toggled in settings)
- [X] 5C.4 Test: verify smooth pan, zoom, and rotation feel good at all zoom levels

---

## Task 6 — NavMesh Setup `[DONE]`

### 6A — NavMesh Configuration
- [X] 6A.1 Add `NavMeshSurface` component (referenced by FacilityBuilder, baked at runtime after geometry is spawned)
- [X] 6A.2 Configure default agent settings (size-per-species deferred to Phase 2 alongside Pets)
- [X] 6A.3 Mark non-walkable zones via `NavMeshModifier` on station cubes (`"Not Walkable"` area)
- [X] 6A.4 Bake the NavMesh at runtime via `NavMeshSurface.BuildNavMesh()` in `FacilityBuilder.Build()`
- [X] 6A.5 Verify baked NavMesh covers all walkable areas in the editor

### 6B — Navigation Validation
- [X] 6B.1 Spawn a placeholder agent on the NavMesh (used `Worker`, not `Pet` — pivot to Worker-first)
- [X] 6B.2 Implement click-to-move (handled by `AgentController` in Task 7C)
- [X] 6B.3 Test pathfinding between rooms (Reception → Bathing → Grooming)
- [X] 6B.4 Verify the agent avoids walls and furniture obstacles
- [X] 6B.5 ~~Remove test script~~ → kept as real infrastructure (`Worker`, `WorkerSpawner`, `AgentController` are not throwaway)

---

## Task 7 — Boot Orchestration & Worker Foundation `[DONE]` `(NEW)`

**Captures work built outside the original plan: ordered system startup and the player-controllable Worker.**

### 7A — GameManager Boot Sequence
- [X] 7A.1 Add `facilityBuilder` and `workerSpawner` SerializeField references to GameManager
- [X] 7A.2 Convert `FacilityBuilder.Start()` → public `Build()` (no auto-start)
- [X] 7A.3 Convert `WorkerSpawner.Start()` → public `Spawn()` (no auto-start)
- [X] 7A.4 In `GameManager.Start()`, call `Build()` then `Spawn()` in deterministic order (NavMesh must exist before agents spawn)
- [X] 7A.5 Boot starts in `GameState.LOADING`; transitions to `PLAYING` after `BootGame()` completes (fires `GameStateChangedEvent`)

### 7B — Worker Component & Prefab
- [X] 7B.1 Create `Worker : MonoBehaviour` in `Scripts/Workers/` with NavMeshAgent wrapper
- [X] 7B.2 Expose `MoveTo(Vector3)` and `HasReachedDestination()` API
- [X] 7B.3 Add `[RequireComponent(typeof(NavMeshAgent))]` to enforce prefab setup
- [X] 7B.4 Create `WorkerSpawner` with `GameObject workerPrefab` SerializeField + `Spawn()` method
- [X] 7B.5 Build Worker prefab: visual + Collider + NavMeshAgent + Worker component

### 7C — Agent Selection & Control
- [X] 7C.1 Add `Click` (Button → `<Mouse>/leftButton`) action to `IA_CameraInput.inputactions`
- [X] 7C.2 Confirm `PointerPosition` (Vector2 → `<Mouse>/position`) action exists
- [X] 7C.3 Create `AgentController : MonoBehaviour` in `Scripts/Player/` (scene-level, single instance)
- [X] 7C.4 Subscribe to `Click.performed`, dispose properly in `OnDestroy`
- [X] 7C.5 On click: raycast through camera; if hit has `Worker` → select it; else if `selectedWorker != null` → command move to hit point
- [X] 7C.6 Test: spawn two workers, swap selection, command each independently

---

## Task 8 — ScriptableObject Data (Worker-first) `[DONE]`

**Was Task 7 (Pet Definitions). Reordered: Worker definitions come first; Pet definitions deferred to Phase 2.**

### 8A — Worker Definitions
- [X] 8A.1 Create `WorkerRole` enum: Groomer, Bather, Vet, Receptionist, Generalist
- [X] 8A.2 Create `SkillLevel` enum or float scale for proficiency per service type
- [X] 8A.3 Create `WorkerDefinition : ScriptableObject` with fields: role, baseMoveSpeed, baseServiceSpeed, skill ratings per ServiceType, hireCost, salary
- [X] 8A.4 Add `[CreateAssetMenu]` attribute
- [X] 8A.5 Create asset: "Worker_Generalist_Starter" — balanced stats, low hire cost
- [X] 8A.6 Create asset: "Worker_Bather_Specialist" — high bathing skill, medium cost

### 8B — Service Definitions
- [X] 8B.1 Create `ServiceType` enum: Grooming, Bathing, VetCheckup, NailTrimming, TeethCleaning, Daycare, Training, Spa
- [X] 8B.2 Create `ServiceDefinition : ScriptableObject` with fields from TDD Section 3.1
- [X] 8B.3 Add `[CreateAssetMenu]` attribute
- [X] 8B.4 Create asset: "Bathing" — base duration 60s, base price $25, required station "BathTub"
- [X] 8B.5 Create asset: "Grooming" — base duration 90s, base price $40, required station "GroomingTable"

---

## Task 9 — Service Station Component `[DONE]`

### 9A — Station Core
- [X] 9A.1 Create `ServiceStation : MonoBehaviour` in `Scripts/Services/`
- [X] 9A.2 Add fields: stationType (string), workerAnchor (Transform), isOccupied (bool)
- [X] 9A.3 Add reference to currentSession (null when empty)
- [X] 9A.4 Add condition field (float 0–100, starts at 100)
- [ ] 9A.5 Implement `IInteractable` interface (from Task 10, can stub for now)

### 9B — Station Placement
- [X] 9B.1 Add ServiceStation component to the placeholder bathing station object
- [X] 9B.2 Configure workerAnchor transform (child object where the Worker should stand)
- [X] 9B.3 Add ServiceStation component to the placeholder grooming station object
- [X] 9B.4 Configure anchor for grooming station
- [X] 9B.5 Create a `StationManager` (or use `ServiceManager` from Task 12) to track all stations
- [X] 9B.6 Implement `GetAvailableStation(ServiceType type)` to find unoccupied stations

---

## Task 10 — Station Interaction & Visual Feedback `[DONE]`

**Reduced scope from original Task 9.** AgentController already routes left-click to Worker selection/movement. This task adds the hover + station-interaction layer on top, focused on stations (not agents).

### 10A — IInteractable Interface
- [X] 10A.1 Create `InteractableType` enum: Station, Furniture, Floor (Pet/Staff added in later phases)
- [X] 10A.2 Create `IInteractable` interface in `Scripts/Interaction/`: OnHoverEnter(), OnHoverExit(), OnSelect(), OnDeselect(), CanInteract()
- [X] 10A.3 Add `InteractableType Type` property to the interface

### 10B — Hover Detection
- [X] 10B.1 Decide: extend AgentController with hover handling, or create a separate `InteractionManager : Singleton<InteractionManager>`
- [X] 10B.2 Per-frame raycast from pointer; track `hoveredEntity`, fire OnHoverEnter/Exit on changes
- [X] 10B.3 Make clicks route to: Worker (existing AgentController logic) OR IInteractable.OnSelect (new path)
- [X] 10B.4 Define `InteractionMode` enum: Normal, Assigning, BuildMode (ChaosResponse later)
- [X] 10B.5 Filter interactions based on current mode

### 10C — Visual Feedback
- [X] 10C.1 Create a simple highlight shader or material swap for hover state (outline or color tint)
- [X] 10C.2 Create a selection indicator (circle on the ground under selected Worker / station)
- [X] 10C.3 Apply hover highlight to ServiceStation when mouse is over it
- [X] 10C.4 Apply selection indicator to the currently controlled Worker (currently no visual cue)
- [X] 10C.5 Test: hovering and clicking stations shows correct visual feedback; selected Worker is visually distinct

---

## Task 11 — DayManager (Minimal) `[DEFERRED to Phase 2]`

**Decision (Task 12 wrap-up):** Doesn't impact gameplay yet (no economy/sessions depend on time-of-day), so pushed to Phase 2 alongside Pets. `ServiceSession.UpdateProgress` uses raw `Time.deltaTime` for now — no `speedMultiplier` hook (see 12A.3 below).

### 11A — Time System
- [ ] 11A.1 Create `DayManager : Singleton<DayManager>` in `Scripts/DayCycle/`
- [ ] 11A.2 Implement `currentTime` (float 0.0–24.0) that advances based on `gameMinutesPerRealSecond`
- [ ] 11A.3 Implement `timeScale` multiplier (1x, 2x, 3x) that affects time progression speed
- [ ] 11A.4 Implement pause/unpause toggle (`isPaused` stops time advancement)
- [ ] 11A.5 Track `currentDay` (int) that increments on day rollover

### 11B — Day Phases
- [ ] 11B.1 Create `DayPhase` enum: PreOpen, Morning, Midday, Afternoon, Closing, Closed
- [ ] 11B.2 Define phase time boundaries (e.g., PreOpen 7–8, Morning 8–11, Midday 11–14, Afternoon 14–17, Closing 17–18, Closed 18–7)
- [ ] 11B.3 Implement phase transition detection based on currentTime
- [ ] 11B.4 Publish `DayPhaseChangedEvent` when phase changes
- [ ] 11B.5 Publish `DayStartedEvent` when a new day begins (transition from Closed to PreOpen)
- [ ] 11B.6 Publish `DayEndedEvent` when day ends (transition to Closed)

### 11C — Debug UI
- [ ] 11C.1 Create a temporary debug HUD: current day, current time (HH:MM), current phase
- [ ] 11C.2 Add speed control buttons (1x / 2x / 3x / pause)
- [ ] 11C.3 Test: verify day progresses through all phases, day number increments, speed controls work

---

## Task 12 — First ServiceSession (Worker-driven) `[DONE]`

**Was Task 11 ("place a pet at a bathing station"). Reframed around the Worker since pets aren't yet present.**

### 12A — ServiceSession Class
- [X] 12A.1 Create `ServiceSession` class in `Scripts/Services/`: sessionId, service (ServiceData), station (ServiceStation), worker (Worker), progress (float 0–1), quality, status enum
- [X] 12A.2 Create `ServiceStatus` enum: Queued, InProgress, Paused, Completed, Failed
- [X] 12A.3 Implement `UpdateProgress(float deltaTime)` that advances progress based on service duration → `speedMultiplier` dropped (DayManager deferred to Phase 2, see Task 11)
- [X] 12A.4 Implement quality calculation (simplified for Phase 1: base 1.0, modulated by Worker skill via `WorkerData.GetSkillRating`)
- [X] 12A.5 Detect completion when progress >= 1.0, change status to Completed

### 12B — ServiceManager
- [X] 12B.1 Create `ServiceManager : Singleton<ServiceManager>` in `Scripts/Services/`
- [X] 12B.2 Track `activeSessions` list
- [X] 12B.3 Implement `StartService(ServiceStation station, ServiceData service, Worker worker)` that creates a ServiceSession and begins progress
- [X] 12B.4 Update all active sessions each frame in the manager's Update loop
- [X] 12B.5 On completion: publish `ServiceCompletedEvent` via EventBus, free up the station (isOccupied = false), free the worker
- [X] 12B.6 Add `ServiceStartedEvent` and `ServiceCompletedEvent` to `Scripts/Core/Events/` (deferred from Task 3B)

### 12C — First Playable Moment
- [X] 12C.1 With a Worker selected, clicking an unoccupied station issues "go to station and start service" → via `StationSelectedEvent` → `WorkerServiceRunner.AssignStation`
- [X] 12C.2 Worker navigates to the station's workerAnchor via NavMesh, arrives, and triggers `ServiceManager.StartService(...)`
- [X] 12C.3 Add a visual progress indicator on the station — World Space Canvas + filled `Image`, kept screen-facing via new `CameraFacingBillboard` component
- [X] 12C.4 On service completion: free station, free worker (worker returns to idle / free roam)
- [X] 12C.5 Log ServiceCompletedEvent to console to verify the full flow works
- [X] 12C.6 Test the complete chain: select Worker → click station → Worker walks there → progress fills → service completes → Worker is free again — **confirmed working in playtest**

**New architecture beyond the original plan:** `WorkerServiceRunner` (on the Worker prefab) owns the full assignment lifecycle — station reservation happens at dispatch time (not arrival), preventing double-booking during the walk. `AgentController` stays a thin input router; it dispatches via `StationSelectedEvent` and never touches navigation/session state directly.

---

## Phase 1 Definition of Done `[REVISED]`

All of the following must be true to consider Phase 1 complete:

- [X] Project structure matches TDD Section 17
- [ ] GameManager, EventBus, and DayManager are functional → DayManager deferred to Phase 2 (Task 11)
- [X] GameManager orchestrates an ordered boot sequence (Build → Spawn → PLAYING)
- [X] Isometric camera moves, zooms, and rotates smoothly
- [X] A placeholder facility with 3 rooms is visible and navigable (NavMesh works)
- [X] Player can select a Worker and command it to move (click-to-select-then-move)
- [X] Multiple Workers can coexist; player controls exactly one at a time
- [X] At least 2 ServiceStations are interactive (hover highlight, click select)
- [X] Selecting a Worker and clicking a station starts a service; Worker walks there and the service runs to completion
- [ ] EventBus correctly dispatches ServiceCompleted and DayPhase events → ServiceCompleted done; DayPhase deferred to Phase 2 (Task 11)
- [ ] Time progresses through day phases with speed control → deferred to Phase 2 (Task 11)
- [X] All code follows the naming conventions from TDD Section 1.4
- [X] Git commits with descriptive messages for each task

---

## Deferred to Phase 2

The original Phase 1 plan included these — they're now Phase 2 work:

- **Pets as game entities** — autonomous AI customers that arrive, wait, get serviced, leave
- **Pet definitions** (PetDefinition ScriptableObject, species/size/temperament data) — originally Task 7A
- **Customer arrival flow** — spawn, queue, assignment, departure
- **Per-species NavMesh agent sizes** — small vs. large pet radii (originally 6A.2)
- **Persistent scene + additive scene loading** (originally 2B.5–2B.7)
- **Save/Load, Economy, Reputation, Staff progression** — out of Phase 1 scope already

---

## Architectural Notes (added during Phase 1)

These weren't in the original plan but emerged during implementation. Recorded for future-Matheus:

- **GameManager as boot orchestrator.** Tasks/systems that need ordered startup should be invoked from `GameManager.BootGame()` rather than relying on `Start()` ordering. Example: NavMesh bake must complete before any NavMeshAgent spawns.
- **Workers are player-controlled; Pets are AI.** The select-then-command pattern (`AgentController`) is intended for Workers. Pets will have their own autonomous behavior tree / state machine in Phase 2 and won't share the AgentController.
- **`AgentController` is scene-level, single-instance.** It tracks `selectedWorker` and routes left-click to either "select another Worker" or "command move". Hover/station-interaction (Task 10) will either extend it or coexist with a separate `InteractionManager`.
- **Input lives in `IA_CameraInput.inputactions` (asset) → `CameraInputActions` (generated class) in `PawsAndCare.Input` namespace.** New player actions get added there until a separate asset (e.g., `IA_PlayerInput`) is worth splitting out.
- **Naming.** `FacilityBuilder` (not `FacilityBootstrap`). `AgentController` (not `WorkerController`) so it can naturally generalize when Pets/other agent types get selectable subsets later.
