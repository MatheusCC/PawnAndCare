# Paws & Care — Phase 4: The Chaos System

> **Renumbered:** formerly Phase 3. The Progression & Expansion slice (now `Phase3_Tasks.md`) was pulled ahead of Chaos — it activates reputation/grid/economy as a growth loop and gives the §8.4 chaos-prevention upgrades a purchase/building system to hook into.

**Phase goal:** Give the smoothly-running Phase 2 business its *personality*. Add the **Chaos system** — the signature "controlled chaos" that makes this "Two Point Hospital with pets" — with the **3 MVP events** (Dog Escape, Wet Shake, Bark Chain), an evaluate → trigger → escalate → resolve lifecycle, player response via 3D interaction + staff dispatch, and reputation/economy consequences.

**Why this phase:** Phase 2 ends with a competent but characterless loop — pets arrive, are serviced, pay, and leave with no friction. Per the GDD, *"the chaos system is what gives Paws & Care its personality... chaos is never random punishment — it's an opportunity for skilled play."* This phase creates the moment-to-moment reactive gameplay the game is built around.

**First vertical slice:** **Dog Escape** end-to-end — a bathing dog breaks free, the player is alerted, it escalates if ignored, and it resolves when the player/staff clicks the dog. Once the lifecycle works for one event, Wet Shake and Bark Chain reuse the framework.

**Maps to:** TDD §6 (Chaos Event System) and TDD §18.1 Phase 5. Sources: GDD §6, TDD §6 + §3 (ChaosEventDefinition).

---

## Prerequisites (from Phase 2)

Chaos leans on systems from the back half of Phase 2. Ideally complete these first; where one isn't ready, the dependency is wired through `EventBus` so chaos degrades gracefully rather than blocking:

- **Task 8 — DayManager:** chaos evaluation cadence and frequency scaling want game-time + day phase. Without it, use a fixed real-time tick.
- **Task 9 — Reputation:** chaos outcomes feed reputation. Chaos publishes resolution events; `ReputationManager` consumes them when present.
- **Task 11 — Staff:** "assign staff to handle it" reuses the existing `Worker` / `WorkerServiceRunner`; full hiring isn't required for the MVP slice.

---

## Carried-over context from Phase 2

Systems already in place that Phase 4 builds on:

- `EventBus`, `Singleton<T>` — chaos stays decoupled via events (TDD §4: *"the Chaos system doesn't need a direct reference to other systems"*).
- `PetStateMachine` (`ARRIVING`→…→`DESPAWNING`) + `Pet` (`PetDefinition`: species, size, **temperament**, desired service, patience).
- `Worker` (NavMesh wrapper) + `WorkerServiceRunner` ("go to anchor, report arrival") + `WorkerManager` (free/busy registry).
- `ServiceDispatcher` / `ServiceSession` / `ServiceManager` (active services, `ServiceCompletedEvent`).
- `EconomyManager.ApplyDelta` (the single balance chokepoint — chaos costs are negative deltas) + `BalanceChangedEvent`.
- `StationManager`, `ReceptionQueue`, interaction layer (`IInteractable`, hover/select on `ServiceStation`).

---

## Task 1 — Chaos Data & Pet Foundations `[TODO]`

The chaos specs reference pet state the codebase doesn't have yet (mood, chaos-prone behavior, escape/panic states).

### 1A — Pet runtime stats
- [ ] 1A.1 Add a **mood** stat to `Pet` (0–100; `GetMood()`), since Bark Chain triggers on `mood < 30`. Seed from temperament; drifts with events later
- [ ] 1A.2 Add **chaos weight** (likelihood this pet causes chaos) — derive from `Temperament` (CALM/ANXIOUS/AGGRESSIVE) for now, or add `chaosWeight` to `PetDefinition` (TDD §3 `chaosWeight: 0.0–1.0`)
- [ ] 1A.3 Expose what the ChaosManager needs to test conditions (e.g. `Species`, `Size`, current `PetState`, mood) via small passthroughs — keep ChaosManager decoupled from `Pet` internals

### 1B — Chaos pet states (enum append-only)
- [ ] 1B.1 Append to `PetState` **after `DESPAWNING`** (never reorder existing values per CLAUDE.md): `ESCAPING`, `CAPTURED`, `PANICKING`, `CALMING`
- [ ] 1B.2 `PetStateMachine` primitives to drive them: `BeginEscaping(path)`, `BeCaptured()`, `BeginPanicking()`, `CalmDown()` — steered by ChaosManager, same primitive-driven style as the dispatcher
- [ ] 1B.3 `ESCAPING` ticks a random NavMesh path; `PANICKING`/`CALMING` are mostly passive (Update-gated like `QUEUING`)

### 1C — ChaosEventDefinition (ScriptableObject)
- [ ] 1C.1 `ChaosEventDefinition : ScriptableObject` in `Scripts/Chaos/`: `eventId`, `displayName`, `category`, `severity`, `baseProbability`, `cooldownSeconds`, `escalationTime`, `escalatesInto` (nullable), `reputationImpact`, `costToResolve`, `resolutionType`, `uiIcon`
- [ ] 1C.2 Enums (own files, UPPER_SNAKE, append-only): `ChaosCategory` (PET_MISBEHAVIOR, FACILITY, CUSTOMER, STAFF, OUTBREAK, RARE), `ChaosSeverity` (MINOR, MODERATE, MAJOR, CRITICAL), `ChaosResolutionType` (CHASE, CLEANUP, CALM, AUTOMATIC)
- [ ] 1C.3 `[CreateAssetMenu]` (menu: `PawsAndCare/Chaos/Chaos Event Definition`)

### 1D — Event assets
- [ ] 1D.1 Create assets: `Chaos_DogEscape`, `Chaos_WetShake`, `Chaos_BarkChain` with the spec values from Task 3

---

## Task 2 — ChaosManager & Event Lifecycle `[TODO]`

### 2A — ChaosManager
- [ ] 2A.1 `ChaosManager : Singleton<ChaosManager>` in `Scripts/Chaos/`: `registeredEvents`, `activeEvents`, `cooldowns` (`Dictionary<string,float>`), `evaluationInterval` (5–10s), `globalChaosMultiplier` (0.5–2.0 difficulty), `maxSimultaneousEvents`
- [ ] 2A.2 Evaluation tick on `evaluationInterval`: iterate registered events, check conditions, roll probability; **only one event may trigger per tick** (TDD §6.1) and respect cooldown + `maxSimultaneousEvents`

### 2B — Probability (MVP subset)
- [ ] 2B.1 `FinalProbability = BaseProbability × GlobalChaosMultiplier × PetCountModifier × PetTemperamentModifier`
- [ ] 2B.2 Defer the rest of the TDD §6.2 modifiers (facility size/condition, weather, time-of-day, prevention upgrades) — they need Phase 2/building systems; stub them as `1.0`

### 2C — ActiveChaosEvent lifecycle
- [ ] 2C.1 `ActiveChaosEvent` (plain C# runtime class): `definition`, `status` (TRIGGERED, ACTIVE, ESCALATED, RESOLVING, RESOLVED), `timeElapsed`, `involvedPets`/`involvedStaff`/`affectedStations`, `responseQuality`, `damageAccumulated`
- [ ] 2C.2 ChaosManager advances active events each frame: escalation timer, resolution checks, completion
- [ ] 2C.3 **Escalation:** when `timeElapsed >= escalationTime` and unresolved → escalate (apply effect, or swap to `escalatesInto`)

### 2D — Chaos events on EventBus
- [ ] 2D.1 `ChaosEventTriggeredEvent`, `ChaosEventEscalatedEvent`, `ChaosEventResolvedEvent` (readonly structs in `Core/Events/ChaosEvents.cs`) carrying the active event + outcome — reputation, economy, and UI consume these without referencing ChaosManager

---

## Task 3 — The Three MVP Events `[TODO]`

Per-event condition checks + visuals + resolution wired into the lifecycle. (Specs verbatim from TDD §6.4.)

### 3A — Dog Escape
- [ ] 3A.1 **Condition:** a dog is `BEING_SERVICED` at a bathing station
- [ ] 3A.2 **Behavior:** pet → `ESCAPING`, runs a randomized NavMesh path (water particle effects)
- [ ] 3A.3 **Escalation (15s):** dog reaches the lobby, upsets waiting/queued customers (satisfaction/reputation hit)
- [ ] 3A.4 **Resolution:** player/staff clicks the dog → chase interaction → `CAPTURED`; a staff member assigned to chase has their current service paused
- [ ] 3A.5 **Reward:** Excellent +3 reputation; Failed −5 reputation (+ affected-customer penalty)

### 3B — Wet Shake
- [ ] 3B.1 **Condition:** a **large** dog just finished bathing and a customer is within 3 m
- [ ] 3B.2 **Behavior:** shake animation + water spray; affected customer reacts; products get a wet overlay (instant event, no escalation)
- [ ] 3B.3 **Resolution:** automatic; affected customer −10 satisfaction, reduced to −5 if a janitor cleans within 30 s
- [ ] 3B.4 **Reward:** quick cleanup avoids product-damage cost

### 3C — Bark Chain
- [ ] 3C.1 **Condition:** a dog with `mood < 30` is in a room with 2+ other dogs
- [ ] 3C.2 **Behavior:** source dog barks; after 3 s adjacent dogs join; nearby cats → `PANICKING`
- [ ] 3C.3 **Escalation (20s):** all facility pets affected; satisfaction drops over time
- [ ] 3C.4 **Resolution:** player/staff calms the source dog (5 s `CALMING` interaction); soundproofing upgrade (post-MVP) would contain it
- [ ] 3C.5 **Reward:** Excellent +2 reputation; Slow/Failed −8 reputation

---

## Task 4 — Player Response & Resolution `[TODO]`

### 4A — 3D interaction
- [ ] 4A.1 Click an active-chaos entity (escaped/barking pet) to respond — extend the existing interaction layer (`IInteractable` / raycast in `AgentController`)
- [ ] 4A.2 Worldspace alert marker over the event so the player can find it

### 4B — Staff dispatch to chaos
- [ ] 4B.1 Assign a free `Worker` to handle an event (new runner state `RESPONDING_TO_CHAOS`), pausing its current service
- [ ] 4B.2 Apply `ChaosInterruptPenalty` (×0.9 quality) to a service interrupted by a chaos response (TDD §5)
- [ ] 4B.3 Worker walks to the event and performs the resolution interaction (chase / cleanup / calm) by `ChaosResolutionType`

### 4C — Response quality
- [ ] 4C.1 `ResponseQuality` (EXCELLENT, GOOD, SLOW, FAILED) from response time + method → drives reputation/cost outcomes

---

## Task 5 — Consequences & Feedback `[TODO]`

### 5A — Reputation hooks
- [ ] 5A.1 On `ChaosEventResolvedEvent`, apply `reputationImpact` by `responseQuality` (consumed by `ReputationManager`, Phase 2 Task 9)

### 5B — Economy hooks
- [ ] 5B.1 Deduct `costToResolve` / accumulated damage via `EconomyManager.ApplyDelta` (negative); publish through `BalanceChangedEvent`

### 5C — Alert UI (MVP)
- [ ] 5C.1 Minimal alert readout (event icon + name + location) on `ChaosEventTriggeredEvent` — debug/OnGUI-level for now; full UIManager pass deferred
- [ ] 5C.2 Clear/update it on escalate + resolve

### 5D — Chaos log
- [ ] 5D.1 `chaosLog` history entries (event, response quality, outcome) for future UI + save (Phase 2 Task 10 persistence)

---

## Task 6 — Chaos Validation `[TODO]`

- [ ] 6.1 Each event triggers from its condition; only one per tick; cooldown respected
- [ ] 6.2 Escalation fires when ignored (Dog Escape 15s, Bark Chain 20s); Wet Shake is instant
- [ ] 6.3 Player click + staff dispatch resolves the event; interrupted service takes the quality penalty
- [ ] 6.4 Response quality drives the right reputation/economy outcome
- [ ] 6.5 Frequency scales with pet count + difficulty multiplier; difficulty slider tunes it
- [ ] 6.6 Full chaos playtest across a day confirmed

---

## Later / Post-MVP (out of scope for Phase 4)

Per TDD §18.2 + GDD §6: facility/weather/time/prevention probability modifiers, soundproofing & other chaos-prevention upgrades, the remaining event categories (Facility, Customer, Staff, **Outbreak**, Rare — 10+ events total), staff stress → staff-triggered chaos, the "Crisis Expert" skill branch, and the full alert/UI pass.

---

## Architectural Notes (Phase 4)

- **Chaos is decoupled via EventBus.** ChaosManager never holds direct references to reputation/economy/UI — it publishes `ChaosEventTriggered/Escalated/Resolved`; consumers subscribe. Mirrors the Phase 2 manager pattern.
- **Pets stay primitive-driven.** New chaos states (`ESCAPING`/`CAPTURED`/`PANICKING`/`CALMING`) are appended to `PetState` (never reordered) and steered by ChaosManager through `PetStateMachine` primitives — same contract the dispatcher uses.
- **Reuse workers as responders.** No new actor type; a `Worker` handling chaos is just a `WorkerServiceRunner` in a `RESPONDING_TO_CHAOS` state, freed back to `WorkerManager` afterward.
- **One trigger per tick** keeps chaos an "opportunity," not a punishment (GDD §6 design pillar). `globalChaosMultiplier` is the difficulty knob.
- **Graceful degradation:** events publish regardless of whether DayManager/ReputationManager exist yet, so Phase 4 isn't hard-blocked on the tail of Phase 2.

---

## Phase 4 Definition of Done `[DRAFT]`

- [ ] All three MVP chaos events trigger from their conditions, escalate when ignored, and resolve via player/staff response
- [ ] Resolution produces the correct reputation + economy outcome, scaled by response quality
- [ ] Chaos frequency scales with pets/difficulty; a difficulty multiplier tunes the experience
- [ ] Chaos stays decoupled (EventBus only); pets remain primitive-driven with append-only states
- [ ] Chaos events are logged for future UI/save
- [ ] All code follows CLAUDE.md conventions
