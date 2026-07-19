# Paws & Care — Phase 3: Facility Building & Expansion

**Phase goal:** Turn the static, hand-authored facility into one the player **builds out and grows**. Buy stations/furniture/decor and place, rotate, and rearrange them on the grid within rooms; unlock pre-authored rooms to expand the footprint. Earn → invest → grow → earn more.

**Why this phase:** Phase 2 ends with a complete but *inert* level — it never changes, the grid is boot-time only, reputation gates nothing, and money has almost nothing to buy. This phase gives the player agency over their space and a reason to keep earning.

**Design decision (locked):** **no wall/door construction.** Rooms are hand-authored in the scene (preserving level-design quality); "expansion" = **unlocking a preset locked room**, which is a fraction of the cost of true construction and sidesteps exactly the complexity the TDD defers — *"wall rendering, and structural validation"* (§12.2). Within any open room, placement/decoration is free-form on the grid. This blends four concepts on purpose: **building** (rooms), **placement** (stations/furniture), **progression** (reputation-gated unlocks), **expansion** (growing footprint).

**First vertical slice:** **Buy & place a station** end-to-end — open the catalog (game pauses), pick an affordable buildable, ghost-preview it on valid grid cells, confirm, pay, NavMesh rebakes, and the station immediately serves customers. Tasks 1→2→3→4.

**Maps to:** TDD §12 (Building System — Grid §12.1, Placement §12.3, NavMesh §12.4), extended past strict §12.2 MVP with the light room-unlock model above. GDD §8.1–8.2 (grid placement, rooms + Unlock Phases), §9.3 (reputation + money unlock layers; the XP skill tree stays post-MVP).

---

## Carried-over context from Phase 1–2 (verified against code)

Systems already in place that Phase 3 builds on:

- **`GridSystem`** (plain `MonoBehaviour`, **not a singleton** — components take a serialized reference, as `FacilityBuilder`/camera already do): `WorldToGrid` / `GridToWorld` / `GetCell` / `IsCellAvailable`, `CreateRoom(RoomType, cells)` / `GetRoomById` / `Rooms`, `Width` / `Height` / `CellSize`. Draws occupied/unwalkable gizmos (currently blank — occupancy is never populated).
- **`GridCell`**: `Position`, `IsOccupied`, `OccupiedBy` (GameObject), `RoomId`, `IsWalkable`, and setters `SetOccupied(GameObject)` / `SetRoomId(int)` / `SetWalkable(bool)`. **Occupancy setters are never called at runtime today** — Task 2 activates them.
- **`FacilityBuilder.Build()`** (boot): registers scene `RoomMarker`s via `GridSystem.CreateRoom`, then `navMeshSurface.BuildNavMesh()`. Called by `GameManager` for deterministic ordering.
- **`RoomMarker`**: `RoomType`, `GetCells()`.
- **`ServiceStation`**: self-registers with `StationManager` on spawn; its `SetOccupied(bool)` is the **service-busy reservation** (dispatcher-owned) — a *different concept* from grid-cell occupancy.
- **`EconomyManager.ApplyDelta`** chokepoint + `ExpenseIncurredEvent` / `ExpenseType` (append-only) + `BalanceChangedEvent`.
- **`ReputationManager`** (0–100) + `ReputationChangedEvent` — currently gates nothing.
- **`UIPanel` / `UIManager`** framework (panels pause the day via `pausesGame`); `HireScreen` is the reference panel. `StatusBarHud`, pooled `FloatingPopup`s, `MoneyFormatUtils`.
- **Interaction layer**: `IInteractable` / `InteractableType` / `InteractionMode` / `InteractionManager` / `AgentController` (raycast + selection).
- **`CustomerSpawner`** + seat-first `ServiceDispatcher` — new station capacity is consumed with zero extra wiring.

---

## Task 1 — Buildable Data Foundations `[TODO]`

Mirrors the `ServiceData` / `PetDefinition` SO pattern: data in ScriptableObjects, logic in components.

### 1A — BuildableDefinition ScriptableObject
- [ ] 1A.1 `BuildableDefinition : ScriptableObject` in `Scripts/Building/`: `displayName`, `description`, `cost`, `footprint` (Vector2Int, in cells), placed prefab reference, `uiIcon`
- [ ] 1A.2 `category` (`BuildCategory` enum: `STATION`, `FURNITURE`, `DECORATION`) and optional `requiredRoomType` (`RoomType`; `NONE` = any room) — drives placement rules + which catalog tab it lives in
- [ ] 1A.3 `requiredReputation` (0 = available from start) — the reputation gate for this buildable
- [ ] 1A.4 `[CreateAssetMenu]` (menu: `PawsAndCare/Building/Buildable Definition`)

### 1B — Enums & expense types (append-only per CLAUDE.md)
- [ ] 1B.1 `BuildCategory` enum (own file, UPPER_SNAKE): `STATION`, `FURNITURE`, `DECORATION`
- [ ] 1B.2 Append to `ExpenseType`: `FURNITURE` (buying/placing a buildable) and `ROOM_UNLOCK` (Task 5) — **at end, never reorder**

### 1C — Buildable assets
- [ ] 1C.1 `Buildable_BathingStation` and `Buildable_GroomingStation` from the existing station prefabs (available from start)
- [ ] 1C.2 One furniture + one decoration asset to prove the non-station categories place correctly
- [ ] 1C.3 One reputation-locked buildable (e.g. `Buildable_VetStation`, `requiredReputation` > 0) to prove gating end-to-end

---

## Task 2 — Grid-Occupancy Activation `[TODO]` `(FOUNDATION)`

The grid tracks coordinates and rooms but **not what sits on it** — `GridCell` occupancy setters are never called at runtime, so `IsCellAvailable` treats station cells as free. This task makes occupancy the facility's **spatial source of truth**, which placement (Task 3), rearrange/sell, layout **persistence** (Task 10 — the save data *is* the occupancy map), and **Chaos** spatial queries (Phase 4 — facility-size modifier, room containment) all read. Done here, before build mode, so everything downstream stands on a truthful grid.

### 2A — Footprint component
- [ ] 2A.1 `GridFootprint : MonoBehaviour` in `Scripts/Building/` on any placed object (station/furniture/decor): holds its `footprint` (from its `BuildableDefinition`) and a serialized `GridSystem` reference
- [ ] 2A.2 `Occupy()` stamps every footprint cell `SetOccupied(gameObject)`; `Free()` clears them (`SetOccupied(null)`). Derive footprint cells from the object's grid origin + footprint size
- [ ] 2A.3 Expose `OccupiedCells` so rearrange/sell (Task 3) can free the right cells

### 2B — Register authored stations at boot
- [ ] 2B.1 `FacilityBuilder.Build()` (or a small pass it calls) stamps occupancy for scene-authored `GridFootprint` objects **after** `RegisterRooms` and **before** the NavMesh bake — so hand-placed stations occupy their cells just like runtime-placed ones
- [ ] 2B.2 Verify the `GridSystem` occupied-cell gizmo now renders the authored facility (free visual confirmation that occupancy is live)

### 2C — Occupancy query helpers
- [ ] 2C.1 `GridSystem.AreCellsAvailable(origin, footprint)` — multi-cell wrapper over `IsCellAvailable` (in bounds + not occupied + walkable), used by both placement validity and the authored-station sanity check
- [ ] 2C.2 `GridSystem.GetCellsInRoom` / room lookup by cell (via `GridCell.RoomId`) — supports the "fits entirely within one room" rule and future room-scoped logic (ambiance, routing)

---

## Task 3 — Build Mode & Placement `[TODO]` `(THE CRUX)`

Runtime placement on the now-truthful grid — TDD §12.3. Build mode is a distinct interaction mode entered from the catalog, with time paused.

### 3A — Build mode entry/exit
- [ ] 3A.1 Append `BUILD_MODE` to the existing `InteractionMode` enum (append-only); `InteractionManager`/`AgentController` route input to build mode while active
- [ ] 3A.2 `BuildModeController : MonoBehaviour` in `Scripts/Building/` (input-layer controller, like `AgentController`) — entered with a selected `BuildableDefinition`, exited on place / cancel (right-click / Esc)
- [ ] 3A.3 Build mode pauses the day through `UIManager`/`pausesGame` (single pause owner — never call `DayManager.SetPaused` directly)

### 3B — Ghost preview & validation (§12.3)
- [ ] 3B.1 Mouse raycast → `WorldToGrid` → ghost prefab snapped to cell centers (`GridToWorld`), footprint-aware; re-evaluate on cell change, not every frame
- [ ] 3B.2 Validity rules: all footprint cells `AreCellsAvailable`; fits entirely within **one** room; respects `requiredRoomType`; doesn't block a room entrance (reachability — keep it simple: entrance cells stay walkable/reachable). Tint ghost valid/invalid (Sage Green / Coral, Art Bible)
- [ ] 3B.3 Rotate (90° steps, swaps footprint x/y) and cancel

### 3C — Placement commit
- [ ] 3C.1 On confirm: re-validate, then charge via `ExpenseIncurredEvent(cost, FURNITURE)` — **validate before charging** (same rule as `TryHire`)
- [ ] 3C.2 Instantiate the placed prefab at the footprint center; its `GridFootprint.Occupy()` stamps the cells; a placed `ServiceStation` self-registers with `StationManager` (existing behavior — Task 2B makes its occupancy real too)
- [ ] 3C.3 Publish `BuildablePlacedEvent` (definition + grid origin) for milestones/UI/persistence

### 3D — Rearrange & sell
- [ ] 3D.1 Select an already-placed object in build mode → pick it up: `GridFootprint.Free()` its cells, re-enter the ghost flow to re-place it (no re-charge for a move)
- [ ] 3D.2 Sell/remove: `Free()` cells, destroy, refund a fraction via `ApplyDelta` (positive); publish `BuildableRemovedEvent`

### 3E — NavMesh (§12.4)
- [ ] 3E.1 Async NavMesh rebake on **build-mode exit** (not per placement); agents mid-path must survive it (watch the `HasReachedDestination` arrival latch fixed in Phase 2)

---

## Task 4 — Build Catalog UI `[TODO]`

Second real panel on the `UIPanel` framework (`HireScreen` is the template).

- [ ] 4.1 `BuildMenuScreen : UIPanel` in `Scripts/UI/` — lists `BuildableDefinition`s (icon, name, cost), grouped/filtered by `BuildCategory`; locked entries greyed with their reputation requirement
- [ ] 4.2 Entry click → close panel → enter build mode with that definition (hand pause from panel to build mode with no un-pause flicker)
- [ ] 4.3 Affordability greying via `BalanceChangedEvent` while open (same pattern as `HireScreen`); locked state via `ProgressionManager` (Task 5)
- [ ] 4.4 Status-bar **Build** button (beside Hire) opens the catalog

---

## Task 5 — Room Unlocking & Expansion `[TODO]`

The progression + expansion layer: pre-authored rooms that start locked and open up when purchased. No construction — just registering an existing room and extending the NavMesh.

### 5A — ProgressionManager
- [ ] 5A.1 `ProgressionManager : Singleton<ProgressionManager>` in `Scripts/Progression/` — single query point `IsUnlocked(BuildableDefinition)` (reputation gate) and room-unlock state
- [ ] 5A.2 Subscribes to `ReputationChangedEvent`; crossing a threshold publishes `BuildableUnlockedEvent`. Unlocks are **latching** (a later reputation drop never re-locks — punitive; GDD §9.2)

### 5B — Locked expansion rooms
- [ ] 5B.1 `ExpansionRoom : MonoBehaviour` in `Scripts/Building/` — holds its `RoomMarker`, unlock cost, optional `requiredReputation` (maps to the GDD §8.2 Unlock-Phase column), and a locked visual (fence/tarp)
- [ ] 5B.2 Locked rooms are click-interactable (`IInteractable`) showing "Unlock — $X" (or "Needs reputation Y" when gated)
- [ ] 5B.3 On purchase: charge `ExpenseIncurredEvent(cost, ROOM_UNLOCK)`; `GridSystem.CreateRoom(marker.RoomType, marker.GetCells())`; swap locked→open visual; async NavMesh update so the new floor is pathable; publish `RoomUnlockedEvent`
- [ ] 5B.4 At least one locked room in the scene proving: unlock room → place stations inside → customers use them (zero extra dispatch wiring)

---

## Task 6 — Decoration & Milestones (light) `[TODO]`

### 6A — Decoration
- [ ] 6A.1 `DECORATION` buildables place exactly like furniture (Task 3 flow) — visual only for now; the **ambiance *score*** (GDD §8.3) is deferred (it's a system unto itself)

### 6B — Milestones
- [ ] 6B.1 `MilestoneTracker : MonoBehaviour` in `Scripts/Progression/` (non-singleton, event-driven), 2–3 launch milestones from GDD §9.1: **Employee of the Month** (first hire), **Expanding Horizons** (first room unlock), **Five Star Review** (N high-quality services)
- [ ] 6B.2 Reward = money bonus via `ApplyDelta` + popup; publishes `MilestoneReachedEvent`. State is runtime-only for now, flagged as save data for Task 10

---

## Task 7 — Building Validation `[TODO]`

- [ ] 7.1 Catalog opens (game pauses); station purchase charges the right amount; placement occupies the right cells (gizmo confirms)
- [ ] 7.2 Placed station serves customers with zero manual wiring (self-registration + dispatcher pick it up)
- [ ] 7.3 Invalid placements impossible: occupied cells, out of bounds, spanning two rooms, wrong/locked room, blocked entrance, insufficient funds
- [ ] 7.4 Rearrange (free move) and sell (refund) update occupancy correctly; no orphaned occupied cells
- [ ] 7.5 NavMesh updates on build-mode exit; pets/workers path around new objects; agents mid-path don't deadlock
- [ ] 7.6 Reputation threshold unlocks a locked buildable at runtime (latching); locked room unlock → build inside → full loop
- [ ] 7.7 Full building playtest across a day; more stations = more throughput/revenue

---

## Later / Post-MVP (out of scope for Phase 3)

Per TDD §12.2 + GDD §8–9: **wall/door construction & free-form room drawing**, multi-floor + lot purchase / relocation, ambiance **scoring** + decoration themes, cleanliness/mess per-cell, the XP **skill tree** (third unlock layer), sell/move refinements beyond the basics, customer-tier scaling from reputation. Chaos-prevention upgrades (GDD §8.4) land in **Phase 4** on top of this system.

---

## Architectural Notes (Phase 3)

- **Grid is the single source of truth for space.** All placement/room validity flows through `GridSystem` + `GridCell` occupancy. No secondary occupancy bookkeeping. Occupancy is populated for *both* authored (boot) and runtime-placed objects so the two are indistinguishable to consumers.
- **`GridSystem` is not a singleton** — Phase 3 components take a serialized reference (the established pattern), never a global lookup.
- **Economy chokepoint untouched.** Buying furniture and unlocking rooms spend via `ExpenseIncurredEvent` → `ApplyDelta`, like salaries/hiring. New `ExpenseType` values are appended, never reordered.
- **Pause ownership stays with `UIManager`.** Build mode reuses the same pause path panels use; nothing else touches `DayManager.SetPaused` directly.
- **Unlock state lives in one place** (`ProgressionManager`), queried (never cached) by UI. Events (`BuildableUnlockedEvent` / `RoomUnlockedEvent`) notify; the manager answers.
- **Pre-authored rooms over procedural construction.** Player agency is *what to place and when to expand*, not drawing walls — a fraction of the tooling cost, consistent with the scene-authored facility, and honoring the TDD's stated reason for deferring construction.
- **Persistence impact (Task 10, deferred):** placed buildables (the occupancy map), unlocked rooms, unlock + milestone state all become save data — a core reason persistence runs after this phase.

---

## Phase 3 Definition of Done `[DRAFT]`

- [ ] Player can buy and place ≥2 station types + furniture/decor from a catalog; placed stations serve customers immediately
- [ ] Objects can be rearranged (free move) and sold (partial refund) with occupancy staying correct
- [ ] Grid occupancy is truthful for authored *and* placed objects (gizmo confirms); placement validity enforces all §12.3 rules
- [ ] At least one locked room unlocks (money + optional reputation gate) and is buildable inside
- [ ] At least one buildable is reputation-gated and unlocks at runtime (latching)
- [ ] All spending flows through `ApplyDelta` with appended `ExpenseType`s; NavMesh stays correct after every change
- [ ] 2–3 milestones fire with rewards
- [ ] All code follows CLAUDE.md conventions
