# Paws & Care — Phase 3: Progression & Expansion

**Phase goal:** Turn the static facility into a **growing business**. The player spends earnings to place new stations and unlock new rooms/services, gated by reputation — activating the dormant systems (reputation, grid, economy chokepoint) as one growth loop.

**Why this phase:** Phase 2 ends with a complete but *inert* loop — the level never changes, reputation gates nothing, and money has almost nothing to buy. Per GDD §7.3, *"reputation is the primary progression metric — it determines what tier of customers visit and what services can be unlocked."* This phase is the missing agency: earn → invest → grow → earn more.

**Why before Chaos (renumbering rationale):** progression activates systems that already exist rather than building from zero, delivers the "reason to keep playing," and gives Phase 4's chaos-prevention upgrades (GDD §8.4) a purchase/placement system to hook into.

**First vertical slice:** **Buy & place a station** end-to-end — open the build menu (game pauses), pick an affordable, unlocked station, place its ghost on valid grid cells, pay, NavMesh rebakes, and the new station immediately starts serving customers.

**Maps to:** GDD §2.2 (growth arc), §7.3 (reputation gating), §8.1–8.2 (building system & rooms), §9.1 (milestones), §9.3 (unlock tree — reputation + money layers only; XP skill tree deferred).

---

## Carried-over context from Phase 1–2

Systems already in place that Phase 3 builds on:

- `GridSystem` (cell↔world conversion, occupancy, walkability, `IsCellAvailable`) + `GridCell` + `Room`/`RoomType`/`RoomMarker` + `FacilityBuilder` (boot-time registration + NavMesh bake) — **all boot-time only today; this phase makes it interactive**.
- `EconomyManager.ApplyDelta` chokepoint + `ExpenseIncurredEvent`/`ExpenseType` (append-only enum) + `BalanceChangedEvent`.
- `ReputationManager` (0–100) + `ReputationChangedEvent` — currently gates nothing.
- `ServiceStation` self-registers with `StationManager` on spawn — a placed station joins dispatch automatically.
- `UIPanel`/`UIManager` framework (panels pause the day via `pausesGame`), `HireScreen` as the reference panel, `StatusBarHud`, pooled `FloatingPopup`s, `MoneyFormatUtils`.
- `CustomerSpawner` + seat-first `ServiceDispatcher` — new capacity is consumed with zero extra wiring.

---

## Task 1 — Buildable Data Foundations `[TODO]`

Mirrors the `ServiceData`/`PetDefinition` SO pattern: data in ScriptableObjects, logic in components.

### 1A — BuildableDefinition ScriptableObject
- [ ] 1A.1 `BuildableDefinition : ScriptableObject` in `Scripts/Building/`: `displayName`, `description`, `cost`, `footprint` (Vector2Int, in cells), station prefab reference, `uiIcon`
- [ ] 1A.2 Unlock gating fields: `requiredReputation` (0 = available from start), optional `requiredRoomType` (station must sit inside a room of this type; `NONE` = anywhere)
- [ ] 1A.3 `[CreateAssetMenu]` (menu: `PawsAndCare/Building/Buildable Definition`)

### 1B — Buildable assets
- [ ] 1B.1 `Buildable_BathingStation` and `Buildable_GroomingStation` from the existing station prefabs (available from start; costs per GDD §9.2 "tight but never punishing")
- [ ] 1B.2 One reputation-locked buildable to prove gating end-to-end (e.g. `Buildable_VetStation`, requires reputation ≥ threshold)

### 1C — Expense type
- [ ] 1C.1 Append `CONSTRUCTION` to `ExpenseType` (append-only per CLAUDE.md)

---

## Task 2 — ProgressionManager & Unlock Gating `[TODO]`

Reputation + money layers of the GDD §9.3 unlock tree. The XP skill tree is explicitly deferred.

### 2A — ProgressionManager
- [ ] 2A.1 `ProgressionManager : Singleton<ProgressionManager>` in `Scripts/Progression/` — owns unlock state; single query point `IsUnlocked(BuildableDefinition)`
- [ ] 2A.2 Subscribes to `ReputationChangedEvent`; when a threshold is crossed, publishes `BuildableUnlockedEvent` (readonly struct in `Core/Events/ProgressionEvents.cs`)
- [ ] 2A.3 Unlocks are **latching** (reputation dropping later never re-locks — losing access feels punitive, GDD §9.2)

### 2B — Unlock feedback
- [ ] 2B.1 Build menu shows locked entries greyed with their reputation requirement (visible goals > hidden content)
- [ ] 2B.2 Unlock moment surfaced to the player (floating popup or status-bar flash on `BuildableUnlockedEvent`; full toast/notification UI deferred)

---

## Task 3 — Build Mode & Grid Placement `[TODO]` `(THE CRUX)`

Runtime placement on the existing grid — the heart of the phase.

### 3A — BuildModeController
- [ ] 3A.1 `BuildModeController : MonoBehaviour` in `Scripts/Building/` — non-singleton (role name per convention); entered with a selected `BuildableDefinition`, exited by placing or cancelling (right-click/Esc)
- [ ] 3A.2 Mouse raycast → `GridSystem.WorldToGrid` → ghost preview snapped to cell centers; footprint-aware (multi-cell buildables)
- [ ] 3A.3 Validity check per frame-of-movement (not per frame): every footprint cell in bounds + `IsCellAvailable` + room-type requirement; ghost tinted valid/invalid (Sage Green / Coral, Art Bible palette)
- [ ] 3A.4 While in build mode the day is paused (reuse the `UIManager`/`pausesGame` path so pause bookkeeping stays in one owner)

### 3B — Placement commit
- [ ] 3B.1 On confirm: re-validate, charge via `ExpenseIncurredEvent(cost, CONSTRUCTION)` — **validate before charging** (same rule as `TryHire`)
- [ ] 3B.2 Instantiate the station prefab at the footprint center; `SetOccupied` on all footprint cells; station self-registers with `StationManager` (existing behavior — verify, don't rewrite)
- [ ] 3B.3 NavMesh update after placement (`NavMeshSurface.UpdateNavMesh` async preferred over a full blocking rebake; agents mid-path must survive the rebake — watch the arrival latch)
- [ ] 3B.4 Publish `BuildablePlacedEvent` (definition + grid origin) for milestones/UI/persistence

### 3C — Room expansion (pre-authored plots)
- [ ] 3C.1 **Scope decision:** no free-form room drawing. Expansion = purchasing **pre-authored locked plots**: scene-placed room areas (RoomMarker + blocked visual) that open up when bought
- [ ] 3C.2 `ExpansionPlot : MonoBehaviour` in `Scripts/Building/` — holds its `RoomMarker`, price, locked visual (fence/tarp); on purchase: register room with `GridSystem`, swap visuals, NavMesh update, publish `RoomPurchasedEvent`
- [ ] 3C.3 Locked plots are click-interactable (existing `IInteractable` layer) showing a "Buy plot — $X" prompt
- [ ] 3C.4 At least one locked plot in the scene proving: buy plot → place stations inside it → customers use them

---

## Task 4 — Build Menu UI `[TODO]`

Second real panel on the `UIPanel` framework (HireScreen is the template).

- [ ] 4.1 `BuildMenuScreen : UIPanel` in `Scripts/UI/` — lists all `BuildableDefinition`s: icon, name, cost, locked/unlocked state
- [ ] 4.2 Entry click → close panel → enter build mode with that definition (panel closes but pause is handed over to build mode, no un-pause flicker)
- [ ] 4.3 Affordability greying via `BalanceChangedEvent` while open (same pattern as HireScreen); locked entries per 2B.1
- [ ] 4.4 Status bar gets a Build button (alongside the Hire button) opening the menu

---

## Task 5 — Milestones (light) `[TODO]`

GDD §9.1, minimal slice — recognition + a concrete reward, not a quest system.

- [ ] 5.1 `MilestoneTracker : MonoBehaviour` in `Scripts/Progression/` (non-singleton, event-driven) with 2–3 launch milestones: **Employee of the Month** (first hire), **Expanding Horizons** (first plot purchased), **Five Star Review** (N high-quality services)
- [ ] 5.2 Reward = money bonus via `ApplyDelta` + popup; publishes `MilestoneReachedEvent` for future UI/save
- [ ] 5.3 Milestone state is runtime-only for now; flagged as save data for Task 10 persistence

---

## Task 6 — Progression Validation `[TODO]`

- [ ] 6.1 Build menu opens (game pauses), station purchase charges the right amount, placement occupies the right cells
- [ ] 6.2 Placed station serves customers with zero manual wiring (self-registration + dispatcher pick it up)
- [ ] 6.3 Invalid placements impossible (occupied cells, out of bounds, wrong/unpurchased room, insufficient funds)
- [ ] 6.4 NavMesh updates: pets/workers path around new stations; agents mid-path don't deadlock
- [ ] 6.5 Reputation threshold unlocks the locked buildable at runtime; unlock is latching
- [ ] 6.6 Plot purchase → build inside it → full loop playtest across a day, including saving throughput (more stations = more revenue)

---

## Later / Post-MVP (out of scope for Phase 3)

Per GDD §8–9: XP **skill tree** (the third unlock layer), free-form room drawing/walls, multi-floor + lot purchase beyond authored plots, relocation, decoration/ambiance score, decoration themes, cleanliness, sell/refund + move placed objects, customer-tier scaling from reputation, franchise/endgame. Chaos-prevention upgrades (§8.4) land in **Phase 4** on top of this system.

---

## Architectural Notes (Phase 3)

- **Grid stays the single source of truth for space.** All placement validity flows through `GridSystem` (`IsCellAvailable` + room checks). No secondary occupancy bookkeeping.
- **The economy chokepoint is untouched.** Construction spends via `ExpenseIncurredEvent` → `ApplyDelta`, like salaries and hiring. New `ExpenseType` values are appended, never reordered.
- **Unlock state lives in one place** (`ProgressionManager`), queried — never cached — by UI. Events (`BuildableUnlockedEvent`) notify; the manager answers.
- **Pre-authored plots over procedural rooms.** Player agency comes from *when/whether* to buy, not free-form construction — a fraction of the tooling cost, and consistent with the existing scene-authored facility.
- **Pause ownership stays with UIManager.** Build mode reuses the same pause path panels use; nothing else touches `DayManager.SetPaused` directly.
- **Persistence impact (Task 10, deferred):** placed buildables, purchased plots, unlock + milestone state all become save data — one more reason persistence runs after this phase.

---

## Phase 3 Definition of Done `[DRAFT]`

- [ ] Player can buy and place at least two station types from a build menu; placed stations serve customers immediately
- [ ] At least one expansion plot can be purchased and built inside
- [ ] At least one buildable is reputation-gated and unlocks at runtime (latching)
- [ ] All spending flows through the `ApplyDelta` chokepoint with appended `ExpenseType`s
- [ ] Invalid placement is impossible; NavMesh stays correct after every placement
- [ ] 2–3 milestones fire with rewards
- [ ] All code follows CLAUDE.md conventions
