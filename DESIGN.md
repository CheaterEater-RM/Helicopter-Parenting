# Helicopter Parenting - Design Document

## Status

- Source design: `SAFER_CHILDREN_DESIGN.md`
- Engineering status: feasible with one required revision before code scaffolding
- Required revision: the sky-dreaming replacement search cannot rely only on `RCellFinder.TryFindAllowedUnroofedSpotOutsideColony(..., extraRegionValidator)` for edge safety, because that hook filters regions, not candidate cells

## Identity

- Display name: Helicopter Parenting
- Package ID: `CheaterEater.HelicopterParenting`
- Harmony ID: `com.cheatereater.helicopterparenting`
- Assembly name: `HelicopterParenting`
- Namespace: `HelicopterParenting`
- Target: RimWorld 1.6, `net48`, Harmony 2.4.x
- DLC requirement: Biotech

## Scaffold Baseline

| Template file | Destination | Notes |
|---|---|---|
| `Rimworld mod template files/About.xml` | `About/About.xml` | Add Harmony dependency and Biotech requirement text |
| `Rimworld mod template files/ModTemplate.csproj` | `Source/HelicopterParenting.csproj` | Keep `net48`, `EnableDefaultCompileItems=true`, output to `..\1.6\Assemblies\` |
| `Rimworld mod template files/ModTemplate.sln` | `Source/HelicopterParenting.sln` | Replace placeholders and normalize all configs to `AnyCPU` |
| `Rimworld mod template files/Setup.cs` | `Source/Setup.cs` | Dedicated `[StaticConstructorOnStartup]` Harmony init |
| `Rimworld mod template files/Settings.cs` | `Source/Settings.cs` | Host shared settings and settings UI |
| `Rimworld mod template files/ExamplePatch.cs` | `Source/Patches/*.cs` | Replace with one patch class per target |
| `Rimworld mod template files/README.md` | `README.md` | Fill after behavior is implemented and verified |

## Architecture

### Harmony patches

| Patch target | Type | Purpose |
|---|---|---|
| `LearningGiver_Floordrawing.TryFindFloordrawingSpots` | Postfix | Replace unsafe freezer or forge selections with safer indoor draw spots when available |
| `NatureRunningUtility.TryFindNatureInterestTarget` | Postfix | Reject unsafe outdoor nature-running targets and optionally block the activity during threats |
| `LearningGiver_Skydreaming.TryGiveJob` | Postfix | Validate or replace sky-dream job targets with safe outdoor cells, or suppress the job when outdoor joy is not appropriate |

### Shared helpers expected during implementation

| Helper | Purpose |
|---|---|
| Indoor draw spot evaluator | Score or accept comfortable/safe floor-drawing rooms and produce a valid draw pair |
| Outdoor safety validator | Check edge distance, optional predator radius, and hostile-threat gating |
| Sky-dream outdoor search helper | Perform a cell-level replacement search for unroofed outdoor spots because vanilla's shared utility does not expose a cell validator |

## Settings

| Setting | Default | Description |
|---|---|---|
| `mapEdgeBuffer` | `15` | Minimum tiles from map edge for outdoor child activities |
| `avoidPredators` | `false` | Reject outdoor spots with nearby predators |
| `predatorScanRadius` | `15` | Radius for predator rejection when enabled |
| `rejectDuringThreats` | `true` | Reject outdoor child activities during active hostile threats |
| `enableFloorDrawFix` | `true` | Toggle freezer-safe floor drawing |
| `enableNatureRunFix` | `true` | Toggle safer nature running |
| `enableSkyDreamFix` | `true` | Toggle safer sky dreaming |

## Verified vanilla constraints

- `LearningGiver_Floordrawing.TryFindFloordrawingSpots` only rejects `Danger.Deadly`, so freezer rooms at `Danger.Some` remain valid
- `LearningGiver_Floordrawing.CanFloorDrawFrom` and `TryFloorDrawCellPair` are public and reusable; `CanDrawOn` is private, so its logic must be inlined or reflected if exact vanilla semantics are needed
- `NatureRunningUtility.TryFindNatureInterestTarget` already calls `JoyUtility.EnjoyableOutsideNow(searcher)`
- `LearningGiver_Skydreaming.TryGiveJob` does not call `JoyUtility.EnjoyableOutsideNow(pawn)`
- `RCellFinder.TryFindAllowedUnroofedSpotOutsideColony` exposes only a region-level `extraRegionValidator`; it cannot guarantee cell-level edge filtering by itself
- `GenHostility.AnyHostileActiveThreatToPlayer(Map)` exists and is a valid threat gate for outdoor activity rejection

## Save compatibility

- No new `Scribe_*` state planned
- No custom `Zone`, `WorldObject`, `ThingComp`, or map component required for the current design
- Mid-save add: safe
- Mid-save removal: safe
- User-facing caveat: if a future revision adds cached state or persistent components, save compatibility must be reassessed before implementation

## Compatibility

### Hard dependencies

- Harmony
- Biotech

### Known interaction surface

- Learning-giver method replacements by other mods can bypass these postfixes entirely
- Shared utility patching on `RCellFinder` is intentionally avoided for sky dreaming to reduce cross-mod blast radius

## In Progress

The current design is ready for scaffold work once two implementation assumptions are carried forward explicitly:

1. The scaffold should include a dedicated outdoor safety helper and a dedicated sky-dream replacement helper, not just three isolated patch files.
2. The template solution file must be corrected from `Any CPU` to `AnyCPU` everywhere before the first build, or Visual Studio can silently skip the project.
