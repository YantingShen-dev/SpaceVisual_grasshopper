# Space Visual

> Visibility and sightline analysis components for Grasshopper.
> 2D / 3D Isovist · VGA space-syntax metrics · Sky View Factor · Visibility graphs · A* visual paths · Inverse surface visibility.

**Compatibility**: Rhino 7 (SR0 +) and Rhino 8.

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

## Installation

### Option A — Yak Package Manager (recommended)

In Rhino, type `_PackageManager` (or `Tools > Package Manager`), search **"spacevisual"** and click Install.

### Option B — Manual

1. Download `SpaceVisual.gha` from the [Releases](https://github.com/YOUR_GITHUB_USER/SpaceVisual/releases) page.
2. Right-click → Properties → check **Unblock** (Windows MOTW).
3. Copy to `%APPDATA%\Grasshopper\Libraries\`.
4. Restart Rhino + Grasshopper.

---

## Building from Source

Requires .NET SDK (any version ≥ 7) and a checkout of this repo.

```powershell
dotnet build -c Release
```

Output: `bin\Release\SpaceVisual.gha`.

`Debug` builds auto-deploy to `%APPDATA%\Grasshopper\Libraries\` via a PostBuild target (close Rhino first or accept the lock-warning).

To skip auto-deploy:
```powershell
dotnet build -c Debug -p:AutoDeployToGrasshopper=false
```

---

## Packaging for Yak

```powershell
.\package-yak.ps1
```

Outputs `dist\spacevisual-X.Y.Z-rh7_0-any.yak`. To publish:

```powershell
& "C:\Program Files\Rhino 8\System\Yak.exe" login        # first time
& "C:\Program Files\Rhino 8\System\Yak.exe" push dist\spacevisual-*.yak
```

---

## License

[MIT](LICENSE) © 2026 POLY LAB

## Author

POLY LAB — Ting — *contact info / website goes here*

---
