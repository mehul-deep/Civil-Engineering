# Spheroid Tank Module Integration

**Date:** 2026-07-27

Today we accomplished the complete end-to-end integration of the Spheroid Tank configuration into the application, fulfilling all 4 planned phases:

## Phase 1: Output follows the company template
- Upgraded `ExportFabricationOutput.cs` to load the company's official `SHEROID TANK PARAMETERS.xlsx` from the `Templates/` directory.
- Re-engineered the output dictionary system (`FabricationOutputRowBuilder.cs`) to dynamically map 198 specific column headers (like `DWSTFQ`, `T1OR`, `KKLR`) across all the sub-tabs in the template spreadsheet.

## Phase 2: Missing C# Entity classes
- Created the SQLite database models for Spheroid parts inside the `Entity/` folder:
  - `TransitionsEntity.cs` (Handles T1 through T6)
  - `KnuckleEntity.cs` (Handles Top/Bottom Knuckles and Knuckle-to-Knuckle)
  - `RoofFingerEntity.cs`
  - `ReducerConeEntity.cs` (Includes Top and Bottom Compression Rings)
  - `DrywellEntity.cs` (Includes Upper/Lower dimensions and Stiffeners)
- Hooked them up via EF Core into `WaterTankDbContext.cs`.
- Added a non-destructive `UpdateSpheroidSchema()` migration command to ensure older project database files automatically get these new tables when loaded.

## Phase 3: Missing UI inputs
- Updated `StartupForm.cs` and `TankTypeSelectionForm.cs` to add the "Spheroid Tank 🔵" as a primary selectable tank type.
- Built `SpheroidGeometryForm.cs`, an advanced 5-tab dialog window using `.ToBindingList()` data-binding with `DataGridView`s (for grouped transitions/knuckles) and `PropertyGrid`s (for standalone roof/reducer/drywell structures).
- Wired `WaterTank.cs` so the "Define Segments" buttons open this Spheroid-specific dialog when the Spheroid tank type is selected.

## Phase 4: Output connected to Entity values
- Finalized `FabricationOutputRowBuilder.cs` by ripping out the temporary `0` placeholders and writing the EF Core database queries to pull live data.
- The workflow (User Interface -> SQLite Database -> Exporter -> Multi-Tabbed Excel Workbook) is now 100% complete and fully operational for Spheroid Tanks.

### July 29 Update: Excel Output Logic & Unit Fixes
- **Unit Conversions (Feet to Inches)**: Updated `FabricationOutputRowBuilder.cs` to correctly output inches for `BPOR` (Base Plate Outside Radius), `BPIR` (Inside Radius), and `CHT` (Total Column Height) by applying `* 12` multipliers.
- **Accurate Cylinder Heights**: Refactored the `colHeight` total length calculation to subtract the absolute minimum `HeightInitial` from the maximum `HeightFinal`. Changed `C1HT`-`C18HT` to output individual segment lengths in inches rather than just printing raw top elevations.
- **Side Chairs & Shims Logic**: Updated the output ratio of Side Chairs (`SC`) and Base Plate Shims (`BPSQ`) to be exactly 2x the number of Anchor Bolts.
- **Template Rebuild**: Corrected MSBuild caching behavior (`PreserveNewest`) which was preventing manual Excel template edits from showing up. Executed a `dotnet clean` & `dotnet build` to push the updated "Base Plate Diameter (in)" column header into the final build.
- **Data Mapping Walkthrough**: Confirmed with the user that `Transitions` (T1-T6) output as `-` when not defined, `B1LR/B1UR` correctly convert diameter to radius (`* 6`), and the main spherical water bowl ("Tanks") is intentionally omitted from the column-support Excel template.

### July 27 Update: Fabrication Output Exporter
- **Phase 1 Complete**: Integrated end-to-end Excel Export logic for both Single Column and Spheroid tanks into the 198-column layout.
- **Data Mapping**: Developed `FabricationOutputRowBuilder.cs` to properly calculate and map software inputs/results (Base Plate, Segments, Anchor Bolts) to template keys (e.g. BPDIA, BPTHK, ABHQ).
- **Template Overhaul**: Modified `ExportFabricationOutput.cs` to abandon destructive deletion. The engine now creates a 100% fresh, blank `XLWorkbook` from scratch, dynamically recreates the 14 tabs from the company template, copies strictly the column headers, and injects ONLY the calculated data from the application (preventing template bloating).
- **Compiler Fixes**: Resolved XML `<ItemGroup>` mismatches in .csproj and resolved entity namespace references. Application .exe successfully rebuilt.
