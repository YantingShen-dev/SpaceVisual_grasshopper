# Changelog

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
