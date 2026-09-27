# Space Visual

> Visibility and sightline analysis components for Grasshopper.
> 2D / 3D Isovist · VGA space-syntax metrics · Sky View Factor · Visibility graphs · A* visual paths · Inverse surface visibility.

**Windows:** the original build targets Rhino 7 (SR0+) and Rhino 8.
**Mac:** a separate build is available for Rhino 7 for Mac. Local use has been confirmed on an Intel Mac with Rhino 7.8; Rhino 8 for Mac, Apple Silicon, and Rhino 7.0–7.7 have not been verified.

---

## Components (11 total, 4 subcategories)

### 1 · Build
| Component | Purpose |
|---|---|
| **View Grid Triangle** | Triangulated grid (Curve/Surface/Brep/Mesh → Mesh + Points). Curve input is Delaunay-clipped to boundary; Surface/Brep use Rhino's mesher with `Spacing` driving edge length. |
| **View Grid** | Quad UV-grid (Curve/Surface/Brep). For trimmed Brep faces, 4-corner trim test culls cells outside the trim (matching native Mesh Surface behaviour). |

### 2 · Analyze 2D
| Component | Purpose |
|---|---|
| **Build Graph 2D** | Visibility graph from points + curve obstacles. Multithreaded N² pair tests. |
| **Isovist 2D** | 2D Isovist polygon + Area / Perimeter / Compactness / MaxRadial / Drift. |
| **VGA Metrics 2D** | 5 space-syntax metrics: Integration / Entropy / Control / Clustering / Connectivity. Single parallel all-pairs BFS. |
| **From Viewpoint 2D** | Step depth + straight-line Distance + bearing Angle to every reachable node (NaN for blocked). |

### 3 · Analyze 3D
| Component | Purpose |
|---|---|
| **Isovist 3D** | Upper-hemisphere Fibonacci sphere sampling → Sight Lines + Volume / SurfArea / MaxRadial / MeanRadial / Drift / **SVF (Sky View Factor)**. |
| **Received Visibility 3D** | Inverse visibility: for each face of obstacle mesh(es), report Hit Count / Avg/Min Distance / Normal Alignment over a viewpoint set. |

### 4 · Visualize
| Component | Purpose |
|---|---|
| **Visual Path 2D** | A* shortest visibility path between two viewpoints on a graph. |
| **Parameter** | Bundles colorization + legend layout config (gradient, min/max, plane-point anchor, scale multipliers) for Colorize. |
| **Colorize** | Renders a colored mesh heatmap + gradient legend + value labels **directly in the viewport** (no data outputs — bake to commit). Variable Values inputs (zoom to +/-) for blended indicators. |

---

## Downloads and installation

The compiled plugins are kept directly in `bin/`; there is no separate `dist/` folder.

| Platform | Plugin | Build details |
|---|---|---|
| Windows | [SpaceVisual.gha](bin/Debug/SpaceVisual.gha) | Original Windows Debug build, retained unchanged |
| Mac — Rhino 7 | [SpaceVisual.gha](bin/Mac/Release/SpaceVisual.gha) | Release build against Rhino 7.8 for Mac, AnyCPU / net48 / Mono |

Open the appropriate file above and use GitHub's **Download raw file** button. Both downloads are named `SpaceVisual.gha`; select the correct platform folder. Only the `.gha` is needed; do not install the `.pdb`, test programs, or Rhino SDK assemblies.

### Windows

1. Download the Windows plugin.
2. Right-click the downloaded file → Properties → **Unblock**, if that option is present.
3. In Grasshopper, open **File → Special Folders → Components Folder** (normally `%APPDATA%\Grasshopper\Libraries\`).
4. Close Rhino, copy `SpaceVisual.gha` into that folder, then restart Rhino and Grasshopper.

### Mac — Rhino 7

1. Download the Mac plugin from `bin/Mac/Release/`.
2. In Grasshopper, open **File → Special Folders → Components Folder**.
3. Quit Rhino, copy `SpaceVisual.gha` into that folder, then restart Rhino and run `Grasshopper`.
4. Look for the **Space Visual** category.

The usual Rhino 7 for Mac components folder is:

```text
~/Library/Application Support/McNeel/Rhinoceros/7.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries/
```

**Keep only one Space Visual installation.** Move any previous version outside the Libraries folder before installing. The Windows and Mac builds retain the same component GUIDs so existing GH definitions can identify them; installing both creates duplicates.

The Mac build uses the same component algorithms as the Windows source. It adds a Mac build workflow, AnyCPU targeting, and a Windows-only guard for automatic Windows deployment. See [MAC-TESTING.md](MAC-TESTING.md) for the Chinese installation guide and functional test checklist.

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
