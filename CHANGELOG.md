# Changelog

## 0.2.3 — 2026-10-20

- Restored View Grid outputs to Mesh and Points; removed Cells. Points retain per-cell geometry for downstream meshes.
- Isovist 2D adds Mesh as its first output and appends `_Boundary` before a final boolean `tol`. Radius/Count remain in their existing input positions. Its mesh, polygons and metrics use the same filtered viewpoints.
- Graph2D also appends `tol` (default false). Both components now exclude points on boundaries and obstacle curves by default. Setting true includes contour contacts, while solid obstacle interiors and points outside the outer boundary remain excluded. Geometric contact tolerance is fixed at 1e-6 model units; this input is a boolean policy, not a distance.
- View Grid Triangle curve samples carry triangle topology. Filtered meshes preserve one vertex per retained analysis value and cull triangles with missing vertices or blocked edges. Triangle mesh/surface inputs continue to use one face per retained face-center sample.
- Contact viewpoints cannot see through a solid by ignoring an origin intersection: Isovist checks initial ray direction, and Graph2D checks chords between contact points.
- Build Graph 2D removes Grid Mesh input and moves Mesh to the first output. Connect grid Points directly to preserve source topology; replace saved components and reconnect by name.
- Downloads now use `SpaceVisual_0.2.3.gha`; future packages derive the filename from the project version.
- Release binary is byte-identical to the installed 0.2.3 candidate. Compilation passed with zero warnings/errors. Automated Rhino runtime regressions remain pending; no new runtime pass is claimed.
- 0.2.2 was an unpublished intermediate prototype and is superseded by this version.


## 0.2.1 — 2026-10-19

- Build Graph 2D accepts an optional original face-sampled `_Grid Mesh` (`_M`) and outputs `Filtered Mesh` (`M`). Retained faces follow the same indices as filtered graph points and VGA values. Existing input/output positions are unchanged.
- Connect View Grid Mesh → Graph2D `_M` → filtered `M` → Colorize Mesh. If the new ports are absent in a saved definition, place a fresh Graph2D component. Hide the original grid preview.
- Colorize now rejects blended Values lists with different lengths instead of partially averaging misaligned data; count errors explain the filtered-mesh connection.
- Verified in an existing Windows Rhino 8 session against the reported case: 372 source faces / 417 vertices → 318 retained faces and values. All five VGA heatmaps render successfully. Regression checks cover source-mesh preservation, face order, all/none excluded, graph-only compatibility and mismatched blend lengths.
- Point filtering removes complete sampled cells; it does not precisely trim cells along obstacle outlines. `_M` supports one sample per face, not vertex-sampled grids.

## 0.2.0 — 2026-10-19

### Fixed

- 2D visibility now intersects finite sightline segments instead of infinite lines. Closed room boundaries no longer reject valid interior sightlines.
- Isovist 2D ignores hits behind the viewpoint and beyond the requested radius; interior wall overlaps still block visibility.
- Build Graph 2D removes candidate points inside/on closed solid obstacles before calculating visibility and graph metrics.

### Changed

- Build Graph 2D distinguishes `_Boundary` (`_B`, outer analysis extent) from `_Obstacles` (`_O`, internal solids or open walls). The new input is appended so existing Points/Obstacles input positions remain stable.
- Added `Filtered Points` (`P`), `Kept Indices` (`I`) and `Removed Indices` (`R`) outputs. Existing Graph/Visible Lines output positions are unchanged.
- Graph nodes and downstream VGA values use the filtered point order. For View Grid heatmaps, select mesh faces with `I` before Colorize.
- Production assembly, category and component GUIDs retain the original Space Visual identities. Test builds are development-only and are not part of the release downloads.

### Upgrade from 0.1.0

1. Close Rhino and replace the previous production installation. Keep one installed copy; check both the Grasshopper Libraries folder and Rhino PackageManager.
2. Move outer boundary curves from `_O` to `_B`. Closed curves remaining in `_O` now exclude their interior. Open curves only block sightlines.
3. If an old saved component does not expose the new ports, place a fresh Build Graph 2D and reconnect it.
4. Preview filtered output `P`, and hide the original grid points. Keep display-mesh faces aligned with the compact metric list using `I`.
5. Definitions made with the temporary **Space Visual test** edition use different component GUIDs: replace those nodes with production components. Do not mix test/production custom Graph or Parameter data.

### Validation and platform scope

- Windows Rhino 8: 11 finite-segment/ray checks plus 50 graph filtering, index-order and downstream checks passed against the production candidate.
- Reported concave-room case: 358 candidate points, 30 excluded solid-obstacle points, 328 retained nodes, 32,779 edges; no unexpected or missing edges against the independent reference.
- Heatmap pipeline: 328 graph points, five 328-value metric lists and 328 mesh faces, with matching spatial order.
- Windows production build targets the Rhino 7 SR0 .NET Framework 4.8 SDK. Runtime verification of this update was performed in Rhino 8.
- The previous Mac **0.1.0** binary has been removed from current downloads. Mac 0.2.0 requires a Mac rebuild and runtime validation.
