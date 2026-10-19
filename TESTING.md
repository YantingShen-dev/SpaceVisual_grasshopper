# Development and regression testing

For 0.2.3, compilation and Python syntax checks passed. Automated Rhino runtime regression remains pending. Run the regression scripts manually inside an existing Rhino session; see the [workflow notes](docs/WORKFLOW-0.2.3.md).

These tools build or verify the current source. They are not archived plugin versions.
The current production version is Space Visual 0.2.3; the optional test build is created only on request.
See [Project layout](PROJECT-STRUCTURE.md) for binary, package and report locations.

To check the production binary in an already open Rhino host, run
`tests/regression/run-production-regression.py2`. Reports go to `artifacts/validation/`.

The `Test` configuration compiles the same source as production into a separate
`SpaceVisual.Test.gha`. It includes portable PDB symbols, disables optimization,
and adds a **Space Visual test** tab with `[test]` in component names.

The plugin, all 11 components, and both custom parameter types have separate,
stable test GUIDs. Normal Debug/Release builds retain the production GUIDs.
Production and test plugins can therefore be installed together.

## Build and install

Run from the repository root in PowerShell:

```powershell
.\scripts\development\build-test.ps1 -Install
```

Omit `-Install` to build only. If Grasshopper uses a custom components folder,
pass `-LibrariesPath 'C:\path\to\Libraries'`.

The script installs only `SpaceVisual.Test.gha` and `SpaceVisual.Test.pdb`.
An existing test build is replaced directly, without retaining old binaries; installation hashes
are recorded in `artifacts/validation/installation.json`. It never publishes to GitHub.

After subsequent rebuilds, save your work and restart the Rhino instance used for
testing. Closing only the Grasshopper window does not unload its assemblies.
If copying fails because the test DLL is locked, close the Rhino instances that
loaded it and run the install command again. The script never closes Rhino for you.

## Reproduce and fix a problem

1. Keep a copy of the user's `.gh` and `.3dm` files. Open the copy in Rhino 8.
2. Place components from **Space Visual test** and reconnect the relevant inputs.
   Existing components remain production components; they are not replaced automatically.
3. Keep graph producers/consumers and Parameter/Colorize chains within the same
   plugin edition. Their custom data types belong to separate assemblies.
4. Compare the production and test chains using the same geometry, parameters,
   document tolerance, and model units. Record the expected and actual result.
5. Edit the normal source files, rebuild with `-Install`, restart the test Rhino
   session, and repeat the failing case.

Test components retain test GUIDs when saved. Keep test definitions separate from
production definitions; promoting a code fix does not convert existing test nodes.

## Attach a C# debugger (optional)

Build/install the Test configuration, then attach Visual Studio to the Rhino.exe
process holding the test model. Select the managed debugger matching Rhino's
active .NET runtime. Load symbols from `bin/Test/SpaceVisual.Test.pdb`, set a
breakpoint in the component's `SolveInstance`, and recompute the test component.
Installing the debug build alone does not attach a debugger.

## Validate before publishing

- Confirm both tabs load, all 11 test components can be created, and custom
  parameters have no GUID collision with production.
- Reproduce the reported bug and verify the fix using the same inputs.
- Check a known working definition, viewport/legend output, and save/reopen.
- Build production with `dotnet build -c Release -p:AutoDeployToGrasshopper=false`;
  test it separately before updating any published binaries.
- Commit/push only after the test result has been reviewed. Do not ship the Test
  binary as the production plugin.

When adding a component or custom parameter, add its test GUID to
`Core/PluginIdentity.cs`. Never change existing mappings, because saved test
definitions use those identities.

## Concave boundary regression

The outer boundary must feed BOTH View Grid's `Region` input and Build Graph 2D's
`_Boundary` (`_B`) input. Internal obstacles belong in `_Obstacles` (`_O`). Grid
points alone carry no boundary information. Keep curves and points in the same XY plane.

Closed obstacles remove candidate points inside/on their outlines before graph
construction. Open walls only block sightlines. Multiple outer boundaries admit
points in their union; nested obstacles remain solid. Output `P` contains filtered
points; `I`/`R` contain kept/removed original indices. VGA values use the compact
graph order. For View Grid heatmaps, keep mesh faces at `I` in order before feeding
the compact values to Colorize. Hide upstream grid previews when checking holes.
Old definitions must move their outer boundary from `_O` to `_B`.

Run `tests/regression/graph-point-filter-regression.py2` for point containment, open walls,
curved and nested obstacles, multiple boundaries, empty/single-node graphs,
invalid geometry, recomputation and VGA ordering. The isolated candidate does not
replace the plugin already loaded in Rhino. Restart after installation to use the
new input/output interface in registered test components.

Run `scripts/development/create-solid-obstacle-test.py2` to save a new desktop `.gh`/`.3dm`
comparison. It contrasts separate open wall segments (no point exclusion) with
the same outline as a closed solid obstacle. Original input previews are hidden;
filtered point and line outputs are visible. If the registered test build is old,
the script saves a candidate definition without adding it to the active canvas;
install/restart before opening that GH file. It never opens over an existing Rhino
model. With the current build loaded, it reopens and recomputes the saved definition.

The local fix replaces infinite-line intersection with finite-segment intersection
in the shared 2D occluder. It prevents closed room boundaries from blocking valid
interior sightlines, and prevents Isovist rays from selecting hits behind the origin
or beyond the configured radius. Wall overlaps remain blocking.

Run `tests/regression/visibility-2d-regression.py2` in a selected Rhino test session through
RhinoCode. It writes `artifacts/validation/visibility-2d-regression.json`, comparing loaded
production/test assemblies with an isolated byte-loaded candidate from `bin/Test`.
The candidate is not registered in Grasshopper and does not update existing nodes.

After installing and restarting the test session, run
`scripts/development/create-concave-test.py2` to create a separate, correctly wired GH example.
It compares every edge of a 64-point L-shaped room against an independent analytic
wall-intersection calculation, repeats the graph solve three times, and checks
the downstream VGA/Colorize chain. Results and the timestamped example are saved
under `artifacts/validation/`. Existing GH documents are preserved.

## Filtered mesh regression

`tests/regression/analysis-mesh-tol-regression.py2` verifies the production face-grid → Graph2D → VGA → Colorize chain in temporary documents. It also reads any compatible open single-branch View Grid case without editing the active canvas. Reports go to `artifacts/validation/analysis-mesh-tol-regression.json`. Override `SPACEVISUAL_CANDIDATE_PATH` to check a staged build. No new Rhino process is started.
