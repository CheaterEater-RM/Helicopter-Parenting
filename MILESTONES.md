# Helicopter Parenting - Milestones

## Planned

### M0 - Scaffold the mod project

- Create `About/`, `1.6/Assemblies/`, `Source/`, and `Source/Patches/`
- Materialize the template files with Helicopter Parenting identity values
- Fix the solution configuration string to `AnyCPU`
- Carry forward the dedicated outdoor helper surfaces identified in `DESIGN.md`

Dependencies: approved design identity, Biotech requirement, corrected solution template

### M1 - Floor drawing safety

- Validate vanilla floor-drawing result against comfortable and safe temperature ranges
- Replace unsafe indoor spots with better home-area options when possible
- Preserve the fallback chain `Comfortable -> Safe -> Vanilla`

Dependencies: scaffold complete, settings scaffold complete

### M2 - Nature running safety

- Add outdoor target validation for edge distance, optional predator radius, and hostile-threat rejection
- Re-run target selection when vanilla picks an unsafe destination

Dependencies: shared outdoor safety helper, settings values

### M3 - Sky dreaming safety

- Add missing `JoyUtility.EnjoyableOutsideNow(pawn)` gate
- Validate or replace unsafe sky-dream targets with a dedicated cell-level outdoor search
- Null the job when no safe replacement exists

Dependencies: shared outdoor safety helper, dedicated sky-dream search helper

### M4 - Polish and publish prep

- Finalize settings text and README
- Build cleanly and validate in RimWorld
- Test mid-save add and removal

Dependencies: M1-M3 complete, startup log clean
