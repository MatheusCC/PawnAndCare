# Paws & Care — Phase 2: The Customer Loop & Living Business

**Phase goal:** Turn the Phase 1 sandbox (manually select a Worker → assign to a station → watch a service complete) into a self-driving business loop where **pets arrive as customers, queue, get serviced, pay, and leave** — with money, time, and reputation on the line.

**Why this phase:** Phase 1 ends with `ServiceCompletedEvent` firing into the void — nothing consumes it. Phase 2 closes that loop: completions produce revenue, customers create demand, and the day cycle gives it all a rhythm.

**First vertical slice:** The **Customer Loop** (Tasks 1–7). Everything after it (DayManager, Reputation, Persistence, Staff) builds on a working customer.

---

## Carried-over context from Phase 1

Systems already in place that Phase 2 builds on:

- `ServiceData` (SO), `WorkerData` (SO with `GetSkillRating`, `DailySalary`), `ServiceType` enum
- `ServiceStation` (self-registers with `StationManager`; `GetAvailableStation(ServiceType)`; reservation via `SetOccupied`)
- `ServiceSession` + `ServiceStatus` + `ServiceManager` (per-frame progress, publishes `ServiceStartedEvent`/`ServiceCompletedEvent`)
- `Worker` (NavMeshAgent wrapper) + `WorkerServiceRunner` (assignment lifecycle) + `AgentController` (player input router)
- `EventBus`, `Singleton<T>`, `CameraFacingBillboard`
- `GameManager.BootGame()` ordered startup (Build → Spawn → PLAYING)

**Key architecture decision locked for Phase 2 (agreed):** the "start the session once *both* the pet and the worker have arrived" handshake will be owned by a new **`ServiceDispatcher`**, NOT by `WorkerServiceRunner`. `WorkerServiceRunner` gets slimmed to "go to this anchor, report when arrived." See Task 5.

---

## Task 1 — Pet Data Foundations `[DONE]`

Mirrors the existing `WorkerData`/`ServiceData` SO pattern.

### 1A — Enums
- [x] 1A.1 `PetSpecies` enum (start small: `DOG`, `CAT`; UPPER_SNAKE per convention, new items appended only)
- [x] 1A.2 `PetSize` enum (`SMALL`, `MEDIUM`, `LARGE`) — drives NavMesh agent radius + station fit later
- [x] 1A.3 `Temperament` enum (`CALM`, `ANXIOUS`, `AGGRESSIVE`) — behavioral effects deferred; data only for now

### 1B — PetDefinition ScriptableObject
- [x] 1B.1 `PetDefinition : ScriptableObject` in `Scripts/Pets/`: species, size, temperament, desired `ServiceType`, patience (seconds before leaving unhappy), visual prefab/material ref
- [x] 1B.2 `[CreateAssetMenu]` attribute (menu: `PawsAndCare/Pets/Pet Definition`)
- [x] 1B.3 Create asset: "Pet_Dog_Bathing" — wants Bathing, medium patience
- [x] 1B.4 Create asset: "Pet_Cat_Grooming" — wants Grooming, lower patience

---

## Task 2 — Pet Entity & State Machine `[DONE]`

### 2A — Pet Component
- [x] 2A.1 `Pet : MonoBehaviour` in `Scripts/Pets/` — NavMeshAgent wrapper mirroring `Worker` (`MoveTo`, `HasReachedDestination`)
- [x] 2A.2 `[RequireComponent(typeof(NavMeshAgent))]`; hold a `PetDefinition` reference
- [x] 2A.3 Expose desired `ServiceType` (from definition) for the dispatcher to match against — via `GetDesiredService()`

### 2B — Pet State Machine (autonomous brain)
- [x] 2B.1 `PetStateMachine` component beside `Pet` — composition, like `WorkerServiceRunner`. **Not** routed through `AgentController` (pets are AI, not player-controlled)
- [x] 2B.2 `PetState` enum: `ARRIVING`, `QUEUING`, `MOVING_TO_STATION`, `BEING_SERVICED`, `LEAVING`, `DESPAWNING`
- [x] 2B.3 State transitions: Arriving → reaches reception → Queuing; dispatcher assigns → MovingToStation; arrives → BeingServiced; session completes → Leaving; reaches exit → Despawning
- [x] 2B.4 Patience timer during Queuing: on timeout → Leaving (unhappy) — reputation hook for later

### 2C — Pet Prefab
- [x] 2C.1 Build pet prefab: visual + Collider + NavMeshAgent + `Pet` + `PetStateMachine`

---

## Task 3 — Spawning & Exit `[DONE]`

### 3A — Spawn/Exit Points
- [x] 3A.1 Scene-authored entrance Transform (spawn) and exit Transform (despawn target) — plus a reception Transform placeholder until Task 4
- [x] 3A.2 Both on the NavMesh so pets path in/out

### 3B — Customer Spawner
- [x] 3B.1 `CustomerSpawner : MonoBehaviour` in `Scripts/Pets/` — instantiates from a pool of pet **prefabs** (each carries its own `PetDefinition`, avoiding a runtime setter on `Pet`)
- [x] 3B.2 **Timer-based pacing for now** (random interval via `min/maxSpawnInterval`). Day-phase gating wired in when DayManager lands (Task 8)
- [x] 3B.3 Started by `GameManager.BootGame()` after the facility/NavMesh exist; spawner is `enabled = false` until `StartSpawning()`, which validates setup first
- [x] 3B.4 Despawn lifecycle: handled by `PetStateMachine.BeginLeaving()` (walk to exit → `Destroy` on arrival). Object Pooling deferred to polish

---

## Task 4 — Reception Queue `[DONE]`

### 4A — Queue Structure
- [x] 4A.1 `ReceptionQueue : Singleton<ReceptionQueue>` in `Scripts/Services/`, tracking ordered waiting pets
- [x] 4A.2 Physical queue slot Transforms (`queueSlots`); pet walks to its assigned slot on enqueue via `TryEnqueue` → `SendToQueueSlot`
- [x] 4A.3 Advance slots forward as pets are removed — `Remove(pet, reason)` shifts the rest via `MoveToQueueSlot`. Used by both the patience timeout and the dispatcher (Task 5)
- [x] 4A.4 `PeekNextForService(ServiceType)` — front-to-back scan returning the earliest-arrived pet wanting that service (per-service FIFO, no head-of-line blocking)

### 4B — Queue Events
- [x] 4B.1 Publish `PetEnqueuedEvent` (+ slot index) / `PetLeftQueueEvent` (+ `QueueLeaveReason`: `DISPATCHED` / `ABANDONED`, so Task 9 reputation can tell a serviced pet from a timeout)

**Design note:** patience moved out of `PetStateMachine` into `ReceptionQueue` (`Update` loop ticks each waiting pet's timer). Keeps the pet primitive-driven and decoupled from queue/dispatcher types, and makes `QUEUING` a passive (Update-off) state. `CustomerSpawner` now enqueues on spawn and turns pets away to the exit when the queue is full.

---

## Task 5 — Service Dispatch & Convergence `[TODO]` `(THE CRUX)`

**The agreed refactor lives here.** In Phase 1, `WorkerServiceRunner` owned "walk to station → start service" because the worker was the only actor. With a pet as a second actor, the "start once everyone has arrived" handshake moves to a neutral owner.

### 5A — WorkerServiceRunner refactor
- [ ] 5A.1 Slim `WorkerServiceRunner`: keep "go to anchor, report arrival" + reservation release on cancel; **remove** the `ServiceManager.StartService` call (dispatcher owns it now)
- [ ] 5A.2 Expose an arrival signal (event or polled `HasArrivedAtStation`) the dispatcher can read
- [ ] 5A.3 Verify Phase 1 manual-click flow still works (or is intentionally superseded — see open question below)

### 5B — ServiceDispatcher
- [ ] 5B.1 `ServiceDispatcher : Singleton<ServiceDispatcher>` in `Scripts/Services/`
- [ ] 5B.2 Matchmaking: front-of-queue pet (wants `ServiceType` X) ↔ `StationManager.GetAvailableStation(X)` ↔ a free worker (see Task 5C)
- [ ] 5B.3 On match: reserve station, dispatch worker to `workerAnchor`, dispatch pet to a new station `customerAnchor`
- [ ] 5B.4 Both-arrived handshake: when worker AND pet report arrival → `ServiceManager.StartService(station, service, worker, pet)`
- [ ] 5B.5 Add `customerAnchor` Transform to `ServiceStation` (where the pet stands/sits)

### 5C — Worker availability
- [ ] 5C.1 `WorkerManager : Singleton<WorkerManager>` registry (mirrors `StationManager`); workers self-register on spawn
- [ ] 5C.2 `GetAvailableWorker(ServiceType)` — free worker, optionally ranked by `GetSkillRating`
- [ ] 5C.3 Free/busy tracking driven by `WorkerServiceRunner` state

### 5D — ServiceSession gains the customer
- [ ] 5D.1 Add `Pet` (customer) field to `ServiceSession`; thread through `ServiceManager.StartService` signature
- [ ] 5D.2 On completion, session exposes the pet so it can be sent to Leaving + charged

> **Open design question to resolve when detailing 5B:** does the player still *manually* assign workers (Phase 1 click flow as an override), or does the dispatcher fully auto-assign (manager-mode)? The `ServiceDispatcher` owns the convergence handshake either way; this only changes who *initiates* the match. Needs a GDD check.

---

## Task 6 — Economy: Close the Loop `[TODO]`

### 6A — EconomyManager
- [ ] 6A.1 `EconomyManager : Singleton<EconomyManager>` in `Scripts/Economy/`
- [ ] 6A.2 Subscribe to `ServiceCompletedEvent`; revenue = `ServiceData.BasePrice` modulated by `quality` (the field already computed in `ServiceSession`)
- [ ] 6A.3 Track running balance; publish `BalanceChangedEvent`
- [ ] 6A.4 Pet pays on departure (or on completion); then transitions to Leaving

### 6B — First money readout
- [ ] 6B.1 Temporary debug HUD: current balance (full UI pass deferred to polish)
- [ ] 6B.2 Day-end revenue summary wired to `DayEndedEvent` once DayManager lands (Task 8)

---

## Task 7 — Customer Loop Validation `[TODO]`

- [ ] 7.1 Pet spawns, paths to reception, occupies a queue slot
- [ ] 7.2 Dispatcher matches pet + station + worker; both converge at the station
- [ ] 7.3 Service runs (progress bar fills), completes, pet pays, balance increases
- [ ] 7.4 Pet paths to exit and despawns; worker + station free up
- [ ] 7.5 Multiple pets queue and are serviced in order; patience timeout sends an unserved pet away
- [ ] 7.6 Full-chain playtest confirmed

---

## Later Phase 2 Milestones `[OUTLINE]`

Detailed once the Customer Loop is working. Sequenced by dependency:

- **Task 8 — DayManager + Day Cycle** (the deferred Phase 1 Task 11): `currentTime`, `timeScale`, pause, `currentDay`; `DayPhase` enum + boundaries; `DayPhaseChangedEvent`/`DayStartedEvent`/`DayEndedEvent`. Gates spawn pacing (3B.2) and the day-end revenue summary (6B.2).
- **Task 9 — Reputation:** driven by service quality, queue wait times, and turn-aways (patience timeouts). Consumes the Task 4B queue events.
- **Task 10 — Persistence:** save/load (balance, reputation, facility, staff) via Newtonsoft JSON; + persistent scene + additive scene loading (deferred Phase 1 2B.5–2B.7).
- **Task 11 — Staff Progression:** hiring flow, daily salary (`WorkerData.DailySalary` already exists), skill growth over time.
- **Task 12 — Polish pass:** Object Pooling for pets, real UI, temperament/size behavioral effects, audio/feedback.

---

## Phase 2 Definition of Done `[DRAFT]`

- [ ] Pets spawn, queue, and are serviced autonomously (no manual clicking required for the core loop)
- [ ] Pet ↔ station ↔ worker convergence works; `ServiceSession` tracks the customer
- [ ] Completed services produce revenue; a running balance is visible
- [ ] Unserved pets leave on patience timeout
- [ ] Day cycle progresses with phases and speed control; day-end revenue summary fires
- [ ] Reputation responds to service quality and wait times
- [ ] Game state persists across save/load
- [ ] All code follows CLAUDE.md conventions

---

## Architectural Notes (Phase 2)

- **ServiceDispatcher owns convergence.** Any flow needing multiple actors to rendezvous before a session starts goes through the dispatcher, not an individual actor's runner. `WorkerServiceRunner` is now "go where told, report arrival."
- **Pets are AI; Workers are player-controlled.** Pets never touch `AgentController`. Their behavior lives in `PetStateMachine`. This was anticipated in the Phase 1 architecture notes.
- **`ServiceStation` gains a `customerAnchor`** beside the existing `workerAnchor` — the worker and the pet stand at different spots.
- **Manager registries mirror `StationManager`.** `WorkerManager` (and the pet/queue trackers) follow the same self-register-on-spawn / query pattern for consistency.
- **DayManager is a dependency, not a blocker.** The Customer Loop runs on a simple spawn timer first; day-phase gating and day-end summaries slot in cleanly once Task 8 lands.
