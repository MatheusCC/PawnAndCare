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

## Task 5 — Service Dispatch & Convergence `[DONE]` `(THE CRUX)`

**The agreed refactor lives here.** In Phase 1, `WorkerServiceRunner` owned "walk to station → start service" because the worker was the only actor. With a pet as a second actor, the "start once everyone has arrived" handshake moved to the neutral `ServiceDispatcher`.

**Decision (was the open question):** **auto-assign (manager mode)** — no clicking for the core loop, per the Phase 2 DoD. Manual worker control is intentionally deferred to a later milestone. Match model is **seat-first**: a pet is seated at a free station on arrival (reserving it) and only waits in the queue when no station is free; a free worker is sent to any station that has a pet but no worker.

### 5A — WorkerServiceRunner refactor
- [x] 5A.1 Slimmed to `GoToStation` / `Release`; **removed** the `ServiceManager.StartService` call, progress mirroring, session, and occupancy (dispatcher owns them)
- [x] 5A.2 Polled arrival signal `HasArrivedAtStation` (latched); plus `IsBusy` and a `Worker` accessor for the dispatcher/WorkerManager
- [x] 5A.3 Phase 1 manual-click flow **intentionally superseded** — `AgentController` reduced to worker selection-only; assign/move return with the manual milestone

### 5B — ServiceDispatcher
- [x] 5B.1 `ServiceDispatcher : Singleton<ServiceDispatcher>` in `Scripts/Services/`
- [x] 5B.2 `AdmitPet`: seat at `GetAvailableStation(type)`, else `ReceptionQueue.TryEnqueue`; freed stations pull `PeekNextForService(type)` from the queue
- [x] 5B.3 On seat: reserve station, send pet to `customerAnchor`; free worker sent to `workerAnchor` (parallel convergence, not a single atomic match)
- [x] 5B.4 Both-arrived handshake: `runner.HasArrivedAtStation` AND `pet.CurrentState == BEING_SERVICED` → `ServiceManager.StartService(station, service, worker, pet)`
- [x] 5B.5 `customerAnchor` Transform added to `ServiceStation` (+ missing-anchor warning)

### 5C — Worker availability
- [x] 5C.1 `WorkerManager : Singleton<WorkerManager>` in `Scripts/Workers/`, mirrors `StationManager`; runners self-register on `Start`
- [x] 5C.2 `GetAvailableWorker(ServiceType)` — free worker ranked by `GetSkillRating` (best-skill idle worker, one-pass max scan)
- [x] 5C.3 Free/busy via `WorkerServiceRunner.IsBusy` (committed from `GoToStation` until `Release`)

### 5D — ServiceSession gains the customer
- [x] 5D.1 `Pet` (customer) field added to `ServiceSession`; threaded through `ServiceManager.StartService`
- [x] 5D.2 On completion the dispatcher reads the job's pet → `CompleteService` (leaves); `session.Pet` exposed for charging (Task 6)

**Note:** a seated pet does **not** time out while waiting for a worker (patience lives only in the reception queue for now); seated-patience can be added with reputation/polish.

---

## Task 6 — Economy: Close the Loop `[DONE]`

### 6A — EconomyManager
- [x] 6A.1 `EconomyManager : Singleton<EconomyManager>` in `Scripts/Economy/`
- [x] 6A.2 Subscribes to `ServiceCompletedEvent`; revenue = `BasePrice × Quality` (full-quality price scaled by the session's achieved quality)
- [x] 6A.3 Tracks running balance (`startingBalance` in inspector); publishes `BalanceChangedEvent` (`NewBalance` + `Delta`) via the `ApplyDelta` chokepoint (reused by future spending)
- [x] 6A.4 Charges **on completion** (event already carries the session); pet→Leaving was already handled by `ServiceDispatcher.CompleteJob`

### 6B — First money readout
- [x] 6B.1 `EconomyDebugHud` — OnGUI overlay drawing the current balance (32pt gold), reading `EconomyManager.Instance` directly. Full UI pass deferred to polish
- [x] 6B.2 Day-end revenue summary wired to `DayEndedEvent` (Task 8): `EconomyManager` logs the day's revenue/balance on `DayEnded`; full-screen summary UI deferred to polish

**Note:** revenue is charged on completion via `ServiceCompletedEvent`; `BalanceChangedEvent` is published for the real UI / floaters later. The debug HUD pulls the balance each frame (robust against event ordering, shows the starting balance immediately).

---

## Task 7 — Customer Loop Validation `[DONE]`

- [x] 7.1 Pet spawns and is seated at a free station (or occupies a queue slot when all stations are busy — seat-first model)
- [x] 7.2 Dispatcher matches pet + station + worker; both converge at the station
- [x] 7.3 Service runs (progress bar fills), completes, pet pays, balance increases
- [x] 7.4 Pet paths to exit and despawns; worker + station free up
- [x] 7.5 Multiple pets queue and are serviced in order; patience timeout sends an unserved (queued) pet away
- [x] 7.6 Full-chain playtest confirmed

**First vertical slice (Tasks 1–7) complete:** pets spawn → seat/queue → converge with a worker → get serviced → pay → leave, with money on the line.

---

## Later Phase 2 Milestones `[OUTLINE]`

Detailed once the Customer Loop is working. Sequenced by dependency:

- **Task 8 — DayManager + Day Cycle** `[DONE]` (the deferred Phase 1 Task 11): `DayManager` Singleton with `currentTime`/`timeScale`/`SetPaused`/`currentDay`; `DayPhase` enum + serialized hour boundaries; auto-cycling days (PreOpen/Closed beats); `DayPhaseChangedEvent`/`DayStartedEvent`/`DayEndedEvent`. Spawn pacing gated by day phase (3B.2) and day-end revenue summary firing (6B.2) are now closed. Full wave-curve (TDD §9.2) + end-of-day UI (TDD §9.3) deferred to polish.
- **Task 9 — Reputation** `[DONE]`: `ReputationManager` (Singleton, 0–100 additive score) reacts to `ServiceCompletedEvent` (quality→1–5★ review delta) and `PetLeftQueueEvent`/`ABANDONED` turn-aways (penalty); clamps and publishes `ReputationChangedEvent`. `ReputationDebugHud` readout. Reputation-driven gameplay (customer tier, unlock tree — TDD §18.2) deferred. Note: with skill-only quality, neutral service is 3★ (flat); skilled staff drive gains.
- **Task 10 — Persistence:** save/load (balance, reputation, facility, staff) via Newtonsoft JSON; + persistent scene + additive scene loading (deferred Phase 1 2B.5–2B.7).
- **Task 11 — Staff Progression** `[DONE]`: `Worker` gains runtime per-service skill (seeded from `WorkerData`); `StaffDirector` (non-singleton component) grows skill on `ServiceCompletedEvent`, pays daily salary on `DayEndedEvent` via `ExpenseIncurredEvent`, and offers a debug hire panel. `ExpenseType` + `ExpenseIncurredEvent`; `EconomyManager` deducts via the `ApplyDelta` chokepoint. Morale/stress and the real hiring UI deferred. (Also fixed a convergence deadlock: `HasReachedDestination` no longer requires `navMeshAgent.hasPath`, which Unity clears on arrival — see Task 5.)
- **Task 12 — Polish pass:** Object Pooling for pets, real UI, temperament/size behavioral effects, audio/feedback.

---

## Phase 2 Definition of Done `[DRAFT]`

- [x] Pets spawn, queue, and are serviced autonomously (no manual clicking required for the core loop)
- [x] Pet ↔ station ↔ worker convergence works; `ServiceSession` tracks the customer
- [x] Completed services produce revenue; a running balance is visible
- [x] Unserved pets leave on patience timeout
- [x] Day cycle progresses with phases and speed control; day-end revenue summary fires (speed/pause via API; player-facing controls deferred to UI polish)
- [x] Reputation responds to service quality and wait times
- [ ] Game state persists across save/load
- [ ] All code follows CLAUDE.md conventions

---

## Architectural Notes (Phase 2)

- **ServiceDispatcher owns convergence.** Any flow needing multiple actors to rendezvous before a session starts goes through the dispatcher, not an individual actor's runner. `WorkerServiceRunner` is now "go where told, report arrival."
- **Pets are AI; Workers are player-controlled.** Pets never touch `AgentController`. Their behavior lives in `PetStateMachine`. This was anticipated in the Phase 1 architecture notes.
- **`ServiceStation` gains a `customerAnchor`** beside the existing `workerAnchor` — the worker and the pet stand at different spots.
- **Manager registries mirror `StationManager`.** `WorkerManager` (and the pet/queue trackers) follow the same self-register-on-spawn / query pattern for consistency.
- **DayManager is a dependency, not a blocker.** The Customer Loop runs on a simple spawn timer first; day-phase gating and day-end summaries slot in cleanly once Task 8 lands.
