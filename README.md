# Space Visual

**Upgrade to 0.2.3:** replace saved View Grid / View Grid Triangle, Build Graph 2D and Isovist 2D components with fresh instances, then reconnect them. See the [mesh and contact workflow](docs/WORKFLOW-0.2.3.md).

[Project layout](PROJECT-STRUCTURE.md) — current release files, development tools and generated outputs.

**Visibility and sightline analysis for Rhino + Grasshopper.**

Explore what a viewpoint can see, compare spatial visibility across a site, and identify which building surfaces are seen from a set of viewpoints. Space Visual brings **11 components** into one Grasshopper tab, from analysis grids to viewport heatmaps.

**English** · [简体中文](README.zh-CN.md)

**Version 0.2.3 for Windows:** Build Graph 2D and Isovist 2D output an aligned visualization Mesh first. Both support separate boundaries and a boolean `tol` switch for points on boundary or obstacle curves. [Release notes and upgrade guide](CHANGELOG.md).

[Download & install](#downloads-and-installation) · [Quick start](#quick-start) · [Components](#components) · [Build from source](#building-from-source)

<p align="center">
  <img src="docs/images/visibility-walkthrough.gif" alt="Animated urban visibility study with a moving viewpoint and colored building surfaces" width="560">
</p>

*An urban visibility study: a moving viewpoint and the surrounding building surfaces.*

## See the analysis

### From site visibility to building surfaces

Use **Isovist 3D** to study visibility from analysis points, and **Received Visibility 3D** to study how often individual mesh faces are visible from a viewpoint set. Feed the resulting values into **Colorize** to compare their spatial distribution.

| Site study | Building-surface study |
| --- | --- |
| ![Animated site heatmap around three building masses](docs/images/site-analysis.gif) | ![Animated building-surface heatmap](docs/images/surface-visibility.gif) |
| Inspect how values vary across the ground around obstacles. | Inspect how values vary across the faces of the building mesh. |

### Read the spatial pattern

| 3D analysis heatmap | Normalized display |
| --- | --- |
| ![Site and building mesh colored with a numeric legend](docs/images/site-heatmap.png) | ![Site and building mesh colored with a zero-to-one legend](docs/images/normalized-heatmap.png) |

*Example outputs supplied with the project. Read each result against its selected metric and legend; a 0–1 color scale alone does not identify the metric.*

### Keep presentation in the same workflow

![Two planar heatmap displays using color and grayscale palettes](docs/images/heatmap-palettes.png)

**Parameter** controls colors, value range, legend position, and label sizing. **Colorize** draws the mesh, legend, and labels directly in the Rhino viewport and supports baking. The image above shows color and grayscale display examples.

## Downloads and installation

| Platform | Download | Compatibility |
| --- | --- | --- |
| Windows 0.2.3 | [SpaceVisual_0.2.3.gha](bin/Release/SpaceVisual_0.2.3.gha) · [Release assets](https://github.com/YantingShen-dev/SpaceVisual_grasshopper/releases/tag/v0.2.3) | Production Release build; targets Rhino 7 (SR0+) and Rhino 8; automated Rhino runtime regression for 0.2.3 remains pending |
| Mac 0.2.3 — Rhino 7 | [SpaceVisual_0.2.3_Mac.gha](bin/Mac/Release/SpaceVisual_0.2.3_Mac.gha) | Built against Rhino 7.8 for Mac, AnyCPU / net48 / Mono; managed checks pass; author tested the point-conversion fix locally on Intel Mac / Rhino 7.8 |

The Mac download is a separate build of the production source. Rhino 8 for Mac, Apple Silicon, and Rhino 7.0–7.7 are not verified. The author confirmed this corrected 0.2.3 Mac build works locally; this is not a claim of exhaustive testing of every component.

Open the appropriate Windows or Mac `.gha` above and click GitHub's **Download raw file** button. Only the `.gha` is needed.

### Windows

1. Download the Windows build above.
2. Right-click the file → **Properties → Unblock**, if shown.
3. In Grasshopper, open **File → Special Folders → Components Folder**, normally `%APPDATA%\Grasshopper\Libraries\`.
4. Close Rhino, copy the file into that folder, and restart Rhino + Grasshopper.
5. Find the **Space Visual** tab.

Keep only one Space Visual installation. Close Rhino and remove older copies before installing.

For upgrades, also check Rhino PackageManager for another installed copy. Remove the temporary **Space Visual test** plugin after converting test definitions to production components; the two editions' custom Graph and Parameter types cannot be mixed. Current downloads are the Windows binary in `bin/Release/` and the clearly labeled Mac binary in `bin/Mac/Release/`. Install only one: both retain the same production assembly identity and component GUIDs. Old Debug, Mac 0.1.0 and test binaries are not distributed.

### Mac — Rhino 7

Download `SpaceVisual_0.2.3_Mac.gha`, open Grasshopper **File → Special Folders → Components Folder**, then quit Rhino and place the file there. Move older Space Visual copies outside the Libraries folder first. Restart Rhino and Grasshopper. See [MAC-TESTING.md](MAC-TESTING.md) for the path, build commands and validation checklist.

## Quick start

### Make a 2D visibility heatmap

1. Create a closed planar outer boundary and coplanar internal obstacle curves.
2. Connect the boundary to **View Grid** or **View Grid Triangle**. Connect its **Points directly** to **Build Graph 2D → Points** so source mesh information is preserved.
3. Connect the **outer boundary** to `_Boundary` and **internal obstacles** to `_Obstacles`. Closed obstacles exclude their interiors; open curves act as walls. Points outside the boundary are excluded. Grid Points do not carry boundary curves, so connect those explicitly.
4. Leave the final boolean **tol** input `false` to exclude points on boundary or obstacle curves. Set it to `true` to include contact points; solid interiors and points outside the boundary remain excluded. This is an inclusion switch, not a numeric tolerance.
5. Connect **Graph** to **VGA Metrics 2D**, then a metric such as Connectivity to **Colorize → Values 0**. Connect **Build Graph 2D → Mesh** (the first output) to **Colorize → Mesh**. No Grid Mesh input is needed.
6. For an isovist heatmap, connect the same grid Points, Boundary and Obstacles to **Isovist 2D**. Use its own first **Mesh** output with its Area, Perimeter or other metric. Set the same `tol` value when comparing both analyses.

**View Grid** outputs Mesh and Points; the interim Cells output has been removed. Its Mesh is the original grid preview. Use each analysis component's own Mesh for filtered heatmaps. Hide upstream grid/point previews to show only the filtered result.

Quad grids and triangle grids made from mesh/surface/Brep inputs use one face per retained sample. Triangle grids made from curves use one vertex per retained sample, with invalid triangles removed. Values follow the same retained order. Filtering removes whole cells/triangles; it does not precisely trim them along obstacle outlines. Converting custom grid samples into ordinary points can discard mesh information: connect grid Points directly to analysis Points.

**Upgrade:** replace saved View Grid / View Grid Triangle, Build Graph 2D and Isovist 2D nodes with fresh components. Ports and point data changed; reconnect by name. Move outer outlines from Obstacles to Boundary. All input points and curves must lie in the same XY plane. [Detailed workflow and limitations](docs/WORKFLOW-0.2.3.md).

### Try another question

| Question | Component workflow |
| --- | --- |
| What can I see from this point in plan? | Viewpoints + obstacle curves → **Isovist 2D** |
| How open is the sky above each point? | Viewpoints + obstacle meshes → **Isovist 3D** → `SVF` → **Colorize** |
| Which surfaces can be seen from these viewpoints? | Viewpoints + target/obstacle meshes → **Received Visibility 3D** → `Hit Count` → **Colorize** |
| What is the shortest route on the visibility graph? | **Build Graph 2D** → **Visual Path 2D**, with start/end points |
| How many visibility steps separate these locations? | **Build Graph 2D** → **From Viewpoint 2D** |

**Working with the outputs**

- For 3D analysis, mesh obstacles are preferred; supported Brep/Surface inputs are meshed internally. Use model units consistently when choosing sight radius and mesh density.
- Isovist 3D defaults to upper-hemisphere sampling. SVF is a cosine-weighted estimate of unobstructed sky **within the specified radius**; sampling and minimum elevation affect the result.
- Received Visibility values follow the concatenated face order of the obstacle meshes. Color the corresponding mesh faces in that same order.
- Colorize accepts one value per face or per vertex. Multiple Values inputs are averaged equally per index; normalize unlike metrics before blending. It has no data outputs—use viewport preview or Bake.
- Begin with a coarse grid. Visibility-graph construction tests pairs of points, so denser grids increase computation quickly.

## Components

### 1 · Build

| Component | Purpose |
| --- | --- |
| **View Grid Triangle** | Triangulated mesh with vertex samples for curves and face-center samples for Surface / Brep / Mesh; points retain source topology. |
| **View Grid** | Quad analysis mesh and face-center points from a closed planar Curve, Surface, or single-face Brep; trim-aware boundary handling. |

### 2 · Analyze 2D

| Component | Purpose |
| --- | --- |
| **Build Graph 2D** | Aligned Mesh, visibility graph and visible lines from filtered points, boundaries and obstacles; boolean tol controls contact points. |
| **Isovist 2D** | Aligned Mesh, visible polygons, Area, Perimeter, Compactness, Max Radial and Drift; Boundary and tol filter viewpoints. |
| **VGA Metrics 2D** | Integration (inverse mean depth), Entropy, Control, Clustering, and Connectivity. |
| **From Viewpoint 2D** | Graph step depth, straight-line distance, and bearing from starting viewpoints; unreachable nodes return NaN. |

### 3 · Analyze 3D

| Component | Purpose |
| --- | --- |
| **Isovist 3D** | Sampled sight lines, estimated Volume / Surface Area, Max / Mean Radial, Drift, and Sky View Factor (SVF). |
| **Received Visibility 3D** | Per-face Hit Count, Avg / Min Distance, Normal Alignment, and Face Centers for a viewpoint set. |

### 4 · Visualize

| Component | Purpose |
| --- | --- |
| **Visual Path 2D** | A* shortest path on the visibility graph; start/end points snap to their nearest graph nodes. |
| **Parameter** | Color range, palette, legend placement, and label/legend scale settings. |
| **Colorize** | Viewport heatmap, gradient legend, and value labels, with Bake support and optional equal-weight blending. |

## Building from source

### Windows — .NET SDK

With the .NET SDK installed, run:

```powershell
dotnet build -c Release
```

The compiler produces the internal `bin/Release/SpaceVisual.gha`. Package it with `scripts/release/package-release.ps1 -YakPath "C:\Program Files\Rhino 8\System\Yak.exe"`; distribution files use `SpaceVisual_<version>.gha`. Only the current versioned GHA is checked in.

On Windows, Debug builds automatically copy the plugin to the existing `%APPDATA%\Grasshopper\Libraries\` directory. To disable this:

```powershell
dotnet build -c Debug -p:AutoDeployToGrasshopper=false
```

### Mac — Rhino 7's bundled Mono

Requires Python 3, Rhino 7 for Mac, and **Microsoft.Net.Compilers.Toolset 4.8.0**. Download the compiler package from [NuGet](https://www.nuget.org/packages/Microsoft.Net.Compilers.Toolset/4.8.0), extract the `.nupkg` as a ZIP, and locate `tasks/net472/csc.exe`.

From the repository root:

```sh
python3 scripts/mac/build-mac.py --compiler /absolute/path/to/tasks/net472/csc.exe
python3 scripts/mac/test-mac.py --compiler /absolute/path/to/tasks/net472/csc.exe
```

Both scripts default to `/Applications/Rhino 7.app`; use `--rhino "/path/to/Rhino 7.app"` for another location. No system .NET installation is required. The build script uses the installed Mac Rhino/Grasshopper SDK; compatibility with older service releases must be tested separately.

Local candidate: **`artifacts/mac/Release/SpaceVisual.gha`** with `build-info.json`; test reports are under `artifacts/mac/tests/`. These files are ignored by Git. The checked-in Mac copy is **`bin/Mac/Release/SpaceVisual_0.2.3_Mac.gha`**. The filename carries the platform label; its internal assembly name stays `SpaceVisual`. The scripts do not install the plugin or publish it.

### Validation

Mac 0.2.3 compiles against the installed Rhino 7.8 SDK. Managed checks cover the production assembly identity, version, all 11 component types, 12 icon resources, graph traversal, priority queue and gradient values. Regression checks also verify conversion of grid points through `IGH_Goo` and acceptance by the native Point parameter with unchanged coordinates. The author tested the corrected build in Rhino on Intel Mac / Rhino 7.8 and confirmed it works. The [Rhino test checklist](MAC-TESTING.md) remains available for broader coverage.

## Distribution

Use `bin/Release/SpaceVisual_0.2.3.gha` for Windows and `bin/Mac/Release/SpaceVisual_0.2.3_Mac.gha` for the Mac build. The Mac binary is available in the repository; existing release assets have not been changed. Publishing to food4Rhino remains a manual step.

---

## License

[MIT](LICENSE) © 2026 POLY LAB

## Author

POLY LAB — [Yanting Shen](https://github.com/YantingShen-dev)

---
