# Space Visual

**Visibility and sightline analysis for Rhino + Grasshopper.**

Explore what a viewpoint can see, compare spatial visibility across a site, and identify which building surfaces are seen from a set of viewpoints. Space Visual brings **11 components** into one Grasshopper tab, from analysis grids to viewport heatmaps.

**English** · [简体中文](README.zh-CN.md)

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
| Windows | [SpaceVisual.gha](bin/Debug/SpaceVisual.gha) | Original Windows Debug build; targets Rhino 7 (SR0+) and Rhino 8 |
| Mac | [SpaceVisual.gha](bin/Mac/Release/SpaceVisual.gha) | Separate Rhino 7 build; local use confirmed on Intel Mac / Rhino 7.8 |

**Mac compatibility:** Rhino 8 for Mac, Apple Silicon, and Rhino 7.0–7.7 have not been verified.

Open the file for your platform and click GitHub's **Download raw file** button. Both files are named `SpaceVisual.gha`; only the `.gha` is needed.

### Windows

1. Download the Windows build above.
2. Right-click the file → **Properties → Unblock**, if shown.
3. In Grasshopper, open **File → Special Folders → Components Folder**, normally `%APPDATA%\Grasshopper\Libraries\`.
4. Close Rhino, copy the file into that folder, and restart Rhino + Grasshopper.
5. Find the **Space Visual** tab.

### Mac — Rhino 7

1. Download the Mac build above.
2. In Grasshopper, open **File → Special Folders → Components Folder**.
3. Quit Rhino, copy the file into that folder, then restart Rhino and run `Grasshopper`.
4. Find the **Space Visual** tab.

The usual Rhino 7 for Mac components folder is:

```text
~/Library/Application Support/McNeel/Rhinoceros/7.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries/
```

Keep only one Space Visual installation in Libraries. Move older versions outside the folder before installing: the Windows and Mac builds share component GUIDs.

See [MAC-TESTING.md](MAC-TESTING.md) for the Chinese Mac installation guide and functional test checklist.

## Quick start

### Make a 2D visibility heatmap

1. Create a closed planar boundary and coplanar obstacle curves.
2. Connect the boundary to **View Grid**. Its `M` output is the analysis mesh; `P` contains one point per mesh face.
3. Connect `P` to **Build Graph 2D → Points**, and the obstacle curves to `_Obstacles`.
4. Connect the resulting `Graph` to **VGA Metrics 2D**. Start with `Connectivity`, the number of directly visible neighbors.
5. Connect the grid mesh to **Colorize → Mesh** and the metric values to `Values 0`. Optionally connect **Parameter** to set the palette and legend.

Preserve point/value order so each result maps to the correct mesh face. For an interior with solid obstacles, exclude sample points inside those obstacles before building the graph, and keep the display mesh aligned with the remaining points.

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
| **View Grid Triangle** | Triangulated analysis mesh and face-center points from Curve / Surface / Brep / Mesh inputs; spacing controls mesh density. |
| **View Grid** | Quad analysis mesh and face-center points from a closed planar Curve, Surface, or single-face Brep; trim-aware boundary handling. |

### 2 · Analyze 2D

| Component | Purpose |
| --- | --- |
| **Build Graph 2D** | Visibility graph and visible lines from points and curve obstacles. |
| **Isovist 2D** | Visible polygon, Area, Perimeter, Compactness, Max Radial, and Drift. |
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

Output: `bin/Release/SpaceVisual.gha`. This does not replace the original checked-in `bin/Debug` build.

On Windows, Debug builds automatically copy the plugin to the existing `%APPDATA%\Grasshopper\Libraries\` directory. To disable this:

```powershell
dotnet build -c Debug -p:AutoDeployToGrasshopper=false
```

### Mac — Rhino 7's bundled Mono

Requires Python 3, Rhino 7 for Mac, and **Microsoft.Net.Compilers.Toolset 4.8.0**. Download the compiler package from [NuGet](https://www.nuget.org/packages/Microsoft.Net.Compilers.Toolset/4.8.0), extract the `.nupkg` as a ZIP, and locate `tasks/net472/csc.exe`.

From the repository root:

```sh
python3 scripts/build-mac.py --compiler /absolute/path/to/tasks/net472/csc.exe
python3 scripts/test-mac.py --compiler /absolute/path/to/tasks/net472/csc.exe
```

Both scripts default to `/Applications/Rhino 7.app`; use `--rhino "/path/to/Rhino 7.app"` for another location. No system .NET installation is required. The build script uses the installed Mac Rhino/Grasshopper SDK; compatibility with older service releases must be tested separately.

Output: **`bin/Mac/Release/SpaceVisual.gha`**. Local build metadata and managed test results are generated alongside it but are not committed. The scripts do not install the plugin or publish it.

### Validation

The Mac managed checks pass for all 11 component types, 12 embedded PNG resources, serial and parallel graph traversal, priority queue ordering, and gradient values. The author has confirmed local use on Intel Mac / Rhino 7.8. This is not a complete test of every component or viewport behavior; use the [Rhino test checklist](MAC-TESTING.md) for further verification.

## Distribution

Use the `.gha` from the appropriate `bin/` folder for manual distribution or food4Rhino uploads. Label the Mac download **Rhino 7 for Mac — tested locally on Intel / Rhino 7.8**. Publishing to food4Rhino remains a manual step.

---

## License

[MIT](LICENSE) © 2026 POLY LAB

## Author

POLY LAB — [Yanting Shen](https://github.com/YantingShen-dev)

---
