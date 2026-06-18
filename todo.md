# Mystic River – Updated Implementation Plan

This plan reflects the current repo state as of May 2026. It **ticks items already implemented** and lists the **next concrete steps**.

---

## 0) Current status (summary)
- ✅ Domain supports **multiple move types** (`DamageMove`, `HealMove`, `ShieldMove`, `Mana*`, `StatusDamageMove`, `StatusEffectMove`, `SelfStatusMove`, `CrowdControlMove`, `LifestealMove`).
- ✅ **Status effects** (poison/burn/toxic/paralysis/sleep/freeze/bleed/haste/slow) and **crowd control** (silence/stun) are implemented with tests.
- ✅ API + client are connected for a real-time battle slice (**HTTP commands + SignalR updates**) with ability catalog, action log, and reconnect resync.
- ✅ Token-based identity/ownership checks are implemented (`X-Player-Token`) with token cleanup sweeper.
- ⚠️ Current flow is still effectively **single-player battle creation** (no create/join lobby flow for two human clients).
- ⚠️ Persistence is still **in-memory** (single-instance only).

---

## 1) Combat semantics in the Domain (core is done ✅)

### Completed
- [x] Implement status effects + end-of-turn ticking.
- [x] Implement status skip logic (paralysis/sleep/freeze) with deterministic tests.
- [x] Implement crowd control (silence/stun) with duration + ticking.
- [x] Enforce silence (blocks mana moves) and stun (skips entire move).
- [x] Expand `TurnResult` with status/CC snapshot.
- [x] Add unit tests for status + crowd control behavior.

### Completed (domain refinements)
- [x] Define **stacking/refresh rules** for status + crowd control (overwrite vs extend vs stack).
- [x] Add **new effects** needed by design (e.g., bleed, haste/slow, lifesteal).
- [x] Formalize **move metadata** (cost, target type, effect tags) to drive UI/API without hardcoding.

### Next steps (domain architecture)
- [x] Introduce a **move-resolution abstraction** (e.g., `IMoveResolver` / visitor) to reduce switch coupling in `Battle.ExecuteTurn`.

---

## 2) Expand API + Contracts beyond basic attack (next priority)

### Tasks
- [x] **Immediate next steps:** Emit **richer SignalR payloads** (action summary + applied effects).
- [x] Expand `BattleStateDto` to include **mana, shield, status, CC**, and current effect durations.
- [x] Include **state version/round identifiers** in DTOs so clients can detect stale updates.
- [x] Add **generic action/ability contracts** (not only `ExecuteBasicAttackRequest`).
- [x] Expose **ability metadata** (id, name, target, tags, mana cost) from the server for the GUI.
- [x] Consolidate move execution into the **generic ExecuteAbility** endpoint (remove per-action requests/endpoints).
- [x] Add a **state snapshot endpoint / hub method** for resync on reconnect.
- [x] Move `BattleSession` + `BattleService` into an **application layer** so HttpApi stays thin.
- [x] Emit richer SignalR payloads (action summary + applied effects).
- [ ] Add integration tests for new action endpoints and invalid action rules.

---

## 2.5) Architecture cleanup and fixes (post-refactor)

### Priority Improvements (from layer review)
- [x] Cap healing at MaxHp in Creature (HIGH - game balance). ✅ Already implemented
- [x] Implement session cleanup/TTL in InMemoryBattleSessionStore (HIGH - memory leak prevention).
- [x] Add structured logging to BattlesController (MEDIUM - ops & debugging).
- [x] De-duplicate validation in BattleService (MEDIUM - maintainability).
- [x] Clarify basic-attack vs ability endpoint (MEDIUM - API clarity).
- [x] Remove WeatherForecast.cs (LOW - cleanup).

### Files to touch
- `src/MysticRiver.Domain/Creature.cs` (healing cap)
- `src/MysticRiver.Application/Battles/InMemoryBattleSessionStore.cs` (session cleanup)
- `src/MysticRiver.HttpApi/Controllers/BattlesController.cs` (logging)
- `src/MysticRiver.Application/Battles/BattleService.cs` (de-duplication)
- `src/MysticRiver.HttpApi/WeatherForecast.cs` (removal)

---

## 3) Upgrade WPF battle UI from placeholders to real actions

### Tasks
- [x] Replace placeholder ability list with **server-provided abilities**.
- [x] Support **target selection** for targeted moves.
- [x] Render **mana, shield, status, CC** in HUD.
- [x] Render **future turn-order** from backend-calculated initiative/effects.
- [x] Add **action log** (damage, heal, blocked by silence, stunned, etc.).
- [x] Handle **reconnect/resync** from SignalR.

### Files to touch
- `src/MysticRiver.Client/Views/BattleView.xaml`
- `src/MysticRiver.Client/Views/BattleView.xaml.cs`
- `src/MysticRiver.Client/Services/BattleApiClient.cs`
- `src/MysticRiver.Client/Services/BattleRealtimeClient.cs`

---

## Multiplayer MVP remaining steps (required for a working 2-player flow)

These are the remaining steps to make multiplayer actually playable between two clients on one running backend.

### A) Match/lobby flow (currently missing)
- [x] Add `CreateMatch` endpoint to create a pending battle room and return `battleId` (or short join code).
- [x] Add `JoinMatch` endpoint so a second player can join an existing pending room.
- [x] Add room state (`WaitingForOpponent`, `Ready`, `InProgress`, `Completed`) to prevent actions before both players join.
- [x] Add server-side assignment of each player to their canonical creature/side at join time (not client-claimed).

### B) Server-authoritative turn ownership (multiplayer-specific)
- [x] Replace/adjust auto-counter behavior for multiplayer so turns are submitted by the real opposing player, not simulated server counter-attacks.
- [x] Enforce turn ownership per action (`token -> player -> creature -> current turn`) in the application layer (not only controller checks).
- [x] Reject out-of-turn actions with explicit error responses and clear action summaries.
- [x] Add clear forfeit/disconnect winner resolution reason codes for UI messaging.

### C) SignalR session robustness
- [x] Validate `JoinBattle` against actual battle membership (do not allow arbitrary battleId/playerId join claims).
- [x] On reconnect, rejoin battle group and refresh/reissue player token before new HTTP actions.
- [x] Add events for lifecycle updates: opponent joined, battle started, opponent disconnected, battle forfeited/ended.

### D) Client multiplayer UX
- [x] Add create/join match UI in main menu (host game + join by code/id).
- [x] Add waiting-room UX (show both players, ready/start state, connection status).
- [x] In battle view, disable action buttons when it is not the local player's turn.
- [x] Show opponent display name and clear turn indicator.

### E) Multiplayer verification (tests)
- [x] Add integration tests for create/join lifecycle and room-state transitions.
- [x] Add integration tests for turn-ownership enforcement (valid player succeeds, out-of-turn fails).
- [x] Add integration tests for reconnect flow (rejoin + resync + action continues).
- [x] Add integration tests for disconnect/abandon winner notification to the other player.

## Recently completed multiplayer foundations
- [x] Connection mapping + token-based ownership (`X-Player-Token`).
- [x] Token TTL cleanup via hosted sweeper.
- [x] Unit/integration tests for token behavior and abandon flow foundations.

---

## Production hardening after MVP multiplayer works
- [ ] Add server action-history endpoint with action IDs + timestamps for replay/deduplication.
- [ ] Replace in-memory sessions with Postgres event streams + snapshots.
- [ ] Add structured multiplayer audit logs, smoke tests, and basic metrics.

---

## 4) Introduce Postgres event sourcing (after multiplayer MVP is playable)

### Tasks
- [ ] Define event schema: `BattleStarted`, `MoveSubmitted`, `TurnResolved`, `EffectApplied`, `EffectExpired`, `BattleEnded`.
- [ ] Create event store tables (`stream_id`, `version`, `event_type`, `payload`, `created_at`).
- [ ] Implement **aggregate rehydration** from events.
- [ ] Replace in-memory battle store with event store.
- [ ] Add **snapshots** (optional first pass, required as streams grow).
- [ ] Keep SignalR publish step **after append/commit**.

### Tests
- [ ] Event append + rehydrate consistency.
- [ ] Concurrency conflicts.
- [ ] Recovery after restart.

---

## 5) Multiplayer orchestration + server authority hardening

### Tasks
- [ ] Add battle lifecycle: create / join / ready / start / submit.
- [ ] Map **player identity** to creature/side ownership.
- [ ] Enforce **ownership + turn legality** per command.
- [ ] Add anti-cheat validation (illegal target, out-of-turn action, invalid payload).
- [ ] Add disconnect/reconnect handling with state resync and token refresh.

---

## 6) Delivery + operations

### Completed
- [x] Environment-scoped `CLIENT_API_BASE_URL` injection in client release workflow.
- [x] Deploy workflow applies manifests **before** image override + rollout status check.

### Next steps
- [ ] Add **staging client release path** (so staging has its own client build).
- [ ] Separate **Docker-required integration tests** into dedicated CI job.
- [ ] Add **post-deploy smoke test** (e.g., `/api/battles/start`).
- [ ] Document branch/env deployment mapping in repo docs.

---

## Recommended execution order

1. **Multiplayer MVP remaining steps** (Sections A-E above)
2. **Postgres event sourcing** (Section 4)
3. **Ops tightening** (Section 6)

---

## Short milestone checklist

- [x] **Milestone A:** API/contracts expose full move + status model.
- [x] **Milestone B:** Client uses server-driven abilities + renders status/CC.
- [ ] **Milestone C:** Two-player battle flow with authoritative validation.
- [ ] **Milestone D:** Battle persistence via Postgres event streams.
