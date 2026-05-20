# Helicopter Parenting — Design Document
*RimWorld 1.6 · Harmony 2.4.x · `net48` · Biotech DLC required*

---

## Problem Statement

Children's learning activities in RimWorld frequently place them in dangerous situations. The vanilla code uses minimal safety heuristics — primarily `Region.DangerFor != Deadly` — which misses many practical dangers. Players consistently report three categories of harm:

1. **Floor Drawing in Freezers** — The #1 complaint. Children finish hauling into a freezer, then immediately pick that freezer as a drawing location because `TryFindFloordrawingSpots` uses BFS from the pawn's current position and picks the first valid cell. Freezer rooms are `Danger.Some` (not `Deadly`), so vanilla doesn't reject them. The child draws for 2500-tick intervals, easily long enough for frostbite, especially on small bodies with narrower comfortable temperature ranges.

2. **Nature Running Near Map Edges / Predators** — `NatureRunningUtility.TryFindNatureInterestTarget` deliberately seeks cells far from colony buildings (min 15 tiles from any building region). It checks `DangerFor != Deadly` and `!avoidWander`, but has zero awareness of map edge proximity, predator locations, or ongoing hostile events. Children sprint to these remote locations at `LocomotionUrgency.Sprint`, making rescue harder.

3. **Sky Dreaming Far From Base** — Uses `RCellFinder.TryFindAllowedUnroofedSpotOutsideColony` with `minDistanceFromColonyThing = 35`, which pushes children far from safety with no regard for map edge, predators, or temperature.

### Other Activities (Not Dangerous)

The remaining learning activities are inherently safe: **Lessontaking** (at a school desk indoors), **Radiotalking** (at a comms console), **Reading** (at a bookcase), and **Workwatching** (follows an adult worker). These don't need patching.

---

## Source Code Analysis

### `LearningGiver_Floordrawing.TryFindFloordrawingSpots`

**Selection logic:** BFS outward from pawn's current position using `RegionTraverser.BreadthFirstTraverse`. The region filter rejects `Danger.Deadly` and entirely-forbidden regions, then tries `TryFindRandomCellInRegion` with a cell validator.

**Cell requirements (`CanFloorDrawFrom`):** `InBounds`, `Standable`, not forbidden, inside Home area, not a door, reservable.

**`CanDrawOn`:** Can make filth, no items/buildings/filth (non-desperate) or just filth-placeable (desperate). Two-pass: first tries 40 regions non-desperate, then 200 regions desperate.

**The gap:** No temperature check whatsoever. No preference for the pawn's current room or any "comfortable" room. `Danger.Some` (which includes freezers at -9°C to -20°C) is accepted. BFS from current position means a child who just hauled into a freezer will find the freezer as the closest valid spot.

### `NatureRunningUtility.TryFindNatureInterestTarget`

**Selection logic:** BFS up to 300 regions from pawn position. Skips forbidden regions, `Danger.Deadly` regions, and regions within sqrt(225)=15 tiles of the pawn. Then does a *nested* BFS to check if any nearby region (within 15 tiles) contains faction-owned `BuildingArtificial` — if so, skips that region (this is the "must be away from base" requirement). Prefers the closest valid cell found.

**Cell requirements:** Not forbidden, `terrain.natural`, `!terrain.avoidWander`.

**The gap:** No map edge awareness. No predator awareness. No awareness of ongoing hostile events (raids, mech clusters). The 15-tile building exclusion zone pushes children toward the map periphery by design. The `terrain.natural` requirement means they run to wild terrain — exactly where predators roam.

### `LearningGiver_Skydreaming` → `RCellFinder.TryFindAllowedUnroofedSpotOutsideColony`

**Selection logic:** BFS up to 45 regions. Collects player-faction pawns and buildings. Cell must be unroofed, not `avoidWander`, `IsGoodDestinationFor(careAboutDanger: true)`, and at least 35 tiles from any player-owned thing.

**The gap:** Same as nature running — no edge/predator/event awareness. The 35-tile exclusion is even more aggressive than nature running's 15-tile version.

### `Region.DangerFor` — Why Freezers Slip Through

Temperature danger is classified as:
- `Danger.None` — within `SafeTemperatureRange()`
- `Danger.Some` — within `SafeTemperatureRange().ExpandedBy(80f)` (i.e., 80°C grace on each side)
- `Danger.Deadly` — outside that expanded range

A freezer at -18°C is merely `Danger.Some` for a child pawn, since it's well within 80° of their safe range. The floor drawing code only rejects `Danger.Deadly`. This is the root cause of the freezer problem.

---

## Design Goals

1. **Vanilla-first.** Postfix patches on spot-finding methods. No new UI, zones, buildings, or research.
2. **Minimal footprint.** Patch the *selection* logic, not the job drivers. If children pick safe locations, the existing jobs work fine.
3. **Configurable.** Mod settings for tuning thresholds (edge buffer, temperature strictness, predator radius).
4. **Savegame-safe.** No custom components, no `ExposeData`, no scribed state. Pure behavior patches. Add/remove freely on any save.
5. **Compatible.** Should work with any mod that doesn't wholesale replace the LearningGiver classes.

---

## Proposed Architecture

### Mod Name: **Helicopter Parenting** (package `CheaterEater.HelicopterParenting`, Harmony ID `com.cheatereater.helicopterparenting`)

### Patch Strategy

Three postfix patches, one per dangerous activity. Each patches the spot-finding method to filter or re-score candidate locations.

#### 1. Floor Drawing — Postfix on `LearningGiver_Floordrawing.TryFindFloordrawingSpots`

**Approach:** Bool-return postfix. If vanilla found a spot, validate it against our safety criteria. If it fails, find a better one. If we can't, fall through to vanilla's result (drawing in a mildly cold room beats not learning at all).

**Safety checks to add:**
- **Temperature comfort:** Reject cells in rooms where `room.Temperature` is outside the pawn's `ComfortableTemperatureRange()` (not `SafeTemperatureRange` — that's too permissive). This catches freezers at -9°C, hot forges, etc.
- **Prefer pawn's current room** (if comfortable) as a tie-breaker, since BFS from current position already biases toward nearby cells.
- **Prefer rooms with other colonists** (social spaces, dining rooms, rec rooms) — makes the behavior feel more natural ("drawing in the living room").

**Implementation detail:** We can't easily re-run the BFS inside a postfix. Instead:
- If the vanilla result is in a comfortable room, accept it.
- If not, do our own lighter-weight search: iterate rooms in the Home area, find one that's comfortable and has standable cells, and pick a random valid cell pair. Fall back to vanilla result if nothing better found.
- The "desperate" pass semantics remain: our override only applies when we can find a better option.

**Alternative (cleaner but more invasive):** Cancelling prefix that replaces `TryFindFloordrawingSpots` entirely with a version that checks temperature in the region filter. This is more surgical but means we own the full method and must track vanilla changes across versions. *Recommendation: start with postfix, switch to prefix only if the postfix proves too limiting.*

#### 2. Nature Running — Postfix on `NatureRunningUtility.TryFindNatureInterestTarget`

**Approach:** Bool-return postfix. If vanilla found a target, validate it. If it fails safety checks, try to find an alternative. If we can't, accept the vanilla result (nature running far away beats not learning).

**Safety checks to add:**
- **Map edge buffer:** Reject cells within N tiles of any map edge (configurable, default 15). This is the single biggest win — keeps children away from raid spawn points.
- **Predator proximity:** Scan for wild animals with `RaceProps.predator` within M tiles of the candidate cell (configurable, default 25). Reject if any predator is too close.
- **Active threat check:** If `GenHostility.AnyHostileActiveThreatToPlayer(map)` is true, reject nature running entirely (postfix returns false, `interestTarget = Invalid`). Children shouldn't be running outside during a raid.
- **Prefer closer-to-base targets:** If multiple valid nature targets exist, prefer the one closest to the pawn's home base position.

**Performance note:** The predator scan is the expensive part. We can cache the predator position set per-tick or per-250-ticks using a MapComponent and check distance against that cached set, rather than scanning all map pawns per candidate cell. However, since `TryFindNatureInterestTarget` is called infrequently (only when a child needs a new nature running job), the scan cost is negligible — just iterate `map.mapPawns.AllPawnsSpawned` once, filter predators, done.

#### 3. Sky Dreaming — Postfix on `RCellFinder.TryFindAllowedUnroofedSpotOutsideColony`

**Problem:** This method is shared — it's not sky-dreaming-specific. Other callers use it for different purposes. We need to apply our safety filters *only* when called for a child's sky dreaming.

**Approach:** We can't safely postfix a shared utility method. Instead, postfix `LearningGiver_Skydreaming.TryGiveJob` — after vanilla creates the job, check whether the target cell is safe. If not, try to find a better unroofed spot with our constraints. If we can't, let vanilla's result stand.

**Safety checks (same as nature running):**
- Map edge buffer
- Predator proximity
- Active threat rejection

**Additional:** Reject sky dreaming spots in temperature-dangerous areas (a child lying on the ground in a toxic fallout zone, extreme heat, or blizzard). Check `GenTemperature.GetTemperatureForCell(cell, map)` against the pawn's `ComfortableTemperatureRange()`.

---

## Settings (ModSettings)

| Setting | Default | Range | Description |
|---|---|---|---|
| `mapEdgeBuffer` | 15 | 5–50 | Minimum tiles from any map edge for nature running / sky dreaming |
| `avoidPredators` | false | bool | Reject outdoor spots with predators nearby (off by default — predator danger is a gameplay feature, not a bug; incentivizes clearing them) |
| `predatorScanRadius` | 15 | 5–50 | If `avoidPredators` is on, reject outdoor spots with predators within this radius |
| `rejectDuringThreats` | true | bool | Block outdoor activities during active hostile threats |
| `enableFloorDrawFix` | true | bool | Enable/disable the floor drawing safety patch |
| `enableNatureRunFix` | true | bool | Enable/disable the nature running safety patch |
| `enableSkyDreamFix` | true | bool | Enable/disable the sky dreaming safety patch |

Per-fix toggles let users disable individual patches if they conflict with other mods or prefer vanilla behavior for specific activities. `avoidPredators` is a single global toggle that applies to both nature running and sky dreaming.

---

## Patch Details

### Patch 1: `Patch_FloorDrawing_SaferSpots`

```
Target: LearningGiver_Floordrawing.TryFindFloordrawingSpots
Type: Postfix
Parameters: Pawn pawn, ref IntVec3 drawFrom, ref IntVec3 drawOn, ref bool __result
```

**Logic:**
1. If `__result == false`, return (vanilla couldn't find anything, neither can we).
2. Check room temperature at `drawFrom`:
   a. If within `pawn.ComfortableTemperatureRange()`, accept — done.
   b. If within `pawn.SafeTemperatureRange()`, flag as "safe fallback" but keep searching for comfortable.
   c. If outside `SafeTemperatureRange()`, must replace.
3. Search for a better spot — iterate rooms in the Home area:
   - First pass: find a room where temperature is within `ComfortableTemperatureRange`.
   - Second pass (if no comfortable room): find a room within `SafeTemperatureRange`.
   - For each candidate room, try to find a valid floor-drawing cell pair using vanilla's own `CanFloorDrawFrom` + `CanDrawOn` + `TryFloorDrawCellPair`.
   - If found, overwrite `drawFrom` and `drawOn`.
4. If no better spot found at either level, keep vanilla result — drawing in a cold room beats not learning at all.

This gives us the three-tier fallback: Comfortable → Safe → Vanilla.

### Patch 2: `Patch_NatureRunning_SaferTarget`

```
Target: NatureRunningUtility.TryFindNatureInterestTarget
Type: Postfix  
Parameters: Pawn searcher, ref LocalTargetInfo interestTarget, ref bool __result
```

**Logic:**
1. If active threats exist and `rejectDuringThreats` is on, set `__result = false`, `interestTarget = Invalid`, return.
2. If `__result == false`, return.
3. Check edge distance: if `interestTarget.Cell` is within `mapEdgeBuffer` of any edge, flag as unsafe.
4. If `avoidPredators` is on: scan `map.mapPawns.AllPawnsSpawned` for wild predators within `predatorScanRadius` of `interestTarget.Cell`. Flag as unsafe if found.
5. If unsafe, attempt to find a replacement:
   - Re-run a simplified BFS similar to vanilla but with our additional cell validator checks (edge buffer + optional predator distance).
   - Use vanilla's same traversal constraints (natural terrain, away from buildings, etc.).
6. If replacement found, overwrite. If not, keep vanilla's result.

**Note on temperature:** No additional temperature check needed here. Vanilla already gates nature running behind `JoyUtility.EnjoyableOutsideNow(pawn)`, which checks `ComfortableTemperatureRange` against outdoor temp, rain, and game conditions (toxic fallout, etc.).

### Patch 3: `Patch_SkyDreaming_SaferSpot`

```
Target: LearningGiver_Skydreaming.TryGiveJob
Type: Postfix
Parameters: Pawn pawn, ref Job __result
```

**Logic:**
1. If `__result == null`, return.
2. **Temperature/weather gate:** Check `JoyUtility.EnjoyableOutsideNow(pawn)`. Vanilla sky dreaming does NOT call this — nature running does but sky dreaming skips it entirely. If it fails, set `__result = null`, return. This catches blizzards, toxic fallout, extreme heat, etc.
3. If active threats and `rejectDuringThreats`, set `__result = null`, return.
4. Validate `__result.targetA.Cell` against edge buffer and optional predator proximity (same checks as nature running, gated by same `avoidPredators` toggle).
5. If unsafe, call `RCellFinder.TryFindAllowedUnroofedSpotOutsideColony` ourselves with an `extraRegionValidator` that enforces our edge buffer constraint. If found, create new job with safe target. If not, null out the job (child will fall back to another learning activity).

**Note:** Unlike nature running, sky dreaming has no vanilla temperature check at all. The `EnjoyableOutsideNow` addition is arguably a vanilla bug fix — a child should not lie on the ground staring at the sky during a blizzard.

---

## Scope Questions & Decisions

### Floor drawing temperature fallback chain

Comfortable → Safe → Vanilla. The child tries for a comfortable room first, falls back to a merely-safe room, and only accepts vanilla's freezer pick as a last resort. This means an ice sheet colony where no room is comfortable but some are safe (heated to -5°C) will still get floor drawing — just not in the -40°C freezer.

### Should we add temperature checks to nature running and sky dreaming?

**Nature running:** No — vanilla already gates it behind `JoyUtility.EnjoyableOutsideNow(pawn)`, which checks `ComfortableTemperatureRange` against outdoor temp plus rain and game conditions. Confirmed in source: the `(Pawn)` overload calls `pawn.ComfortableTemperatureRange().Includes(map.mapTemperature.OutdoorTemp)`.

**Sky dreaming:** Yes — vanilla does NOT call `EnjoyableOutsideNow` for sky dreaming. This is arguably a vanilla bug. We add the check in our postfix. If weather/temperature is unenjoyable, the child falls back to another learning activity.

### Predator avoidance: off by default

Predators being dangerous is a gameplay feature. The mod's job is to keep children from running to map edges during raids (a bug in target selection), not to eliminate all outdoor risk. Players who want full protection can enable it. Single global toggle applies to both nature running and sky dreaming.

### What about fire / active combat zones?

`Region.DangerFor` catches fire (rooms on fire are `Danger.Deadly`). We don't need to duplicate that. Active combat is covered by `rejectDuringThreats` for outdoor activities. For indoor floor drawing, children already stay in Home area behind the defensive perimeter. No additional combat check needed.

### What about pollution?

Polluted terrain has `avoidWander = true`, which vanilla already respects for nature running and sky dreaming. Floor drawing happens on Home area cells, which shouldn't be polluted. No action needed.

---

## File Structure

```
Rimworld Mods\
└── HelicopterParenting\
    ├── About\
    │   └── About.xml
    ├── 1.6\
    │   └── Assemblies\
    ├── Source\
    │   ├── HelicopterParenting.sln
    │   ├── HelicopterParenting.csproj
    │   ├── Setup.cs
    │   ├── Settings.cs
    │   └── Patches\
    │       ├── Patch_FloorDrawing.cs
    │       ├── Patch_NatureRunning.cs
    │       └── Patch_SkyDreaming.cs
    ├── DESIGN.md (this file)
    └── MILESTONES.md
```

No Defs, no Patches (XML), no Textures. Pure C# mod.

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Our postfix finds no better spot, child still draws in freezer | Low — same as current vanilla behavior | Fall-through design means we never make things worse |
| Re-running BFS in postfix is expensive | Low — called only when child starts a new learning job, not per-tick | Profile with Dub's Performance Analyzer; BFS is region-limited |
| Other mods replace LearningGiver classes entirely | Patches don't fire | Per-fix `[HarmonyPrepare]` checks + graceful failure (mod just does nothing) |
| `ComfortableTemperatureRange` too strict in extreme biomes | Children can never floor-draw | Three-tier fallback (Comfortable → Safe → Vanilla) ensures drawing always works somewhere |
| `TryFindAllowedUnroofedSpotOutsideColony` changes signature | Patch breaks | Pin `[HarmonyPatch]` with arg types; `[HarmonyPrepare]` guards |

---

## Implementation Order

1. **M1 — Floor Drawing Safety** — Highest community demand. Patch + settings for temperature comfort.
2. **M2 — Nature Running Safety** — Edge buffer + predator scan + threat gate.
3. **M3 — Sky Dreaming Safety** — Same safety checks applied to sky dreaming targets.
4. **M4 — Polish & Publish** — Settings UI, descriptions, Steam Workshop listing.

Each milestone is a self-contained, testable commit.

---

## Test Plan

### Floor Drawing
- [ ] Child in colony with accessible freezer prefers non-freezer room for drawing
- [ ] Child who just hauled into freezer still picks a warm room to draw
- [ ] Child in ice sheet colony (all rooms cold) still draws somewhere (Safe fallback, then Vanilla fallback)
- [ ] Child in tropical colony with no freezer behaves identically to vanilla
- [ ] Settings toggle: disabling the fix restores vanilla behavior
- [ ] Fallback chain: Comfortable room chosen over Safe room when both available

### Nature Running
- [ ] Child does not run within 15 tiles of map edge
- [ ] Child does not start nature running during active raid
- [ ] With `avoidPredators` enabled: child does not run toward a cell with nearby predator
- [ ] With `avoidPredators` disabled (default): predator proximity is ignored
- [ ] Child in small map (limited space) still finds valid nature target
- [ ] No nature targets available → child falls back to other learning activities

### Sky Dreaming
- [ ] Child does not sky-dream within edge buffer of map boundary
- [ ] Child does not start sky dreaming during active raid
- [ ] Sky dreaming blocked during extreme temperature (blizzard / heat wave) — `EnjoyableOutsideNow` gate
- [ ] Sky dreaming blocked during toxic fallout / heavy rain
- [ ] With `avoidPredators` enabled: child does not sky-dream near predators

### Compatibility
- [ ] Works with Vanilla Expanded Framework
- [ ] Works with base game + Biotech only (no other DLC)
- [ ] Add to mid-game save — no errors
- [ ] Remove from mid-game save — no errors
- [ ] Works alongside "Children, school and learning" mod (if active)
