# 0.2.3 mesh and contact workflow / 网格与接触点规则

Windows 0.2.3 uses the same binary installed locally for Rhino 7 and Rhino 8. Automated Rhino runtime regression remains pending.

## Ports

| Component | Inputs, top to bottom | Outputs, top to bottom |
| --- | --- | --- |
| View Grid | Region, U Count, V Count, Overhang | Mesh, Points |
| Build Graph 2D | Points, Obstacles, Boundary, **tol** | **Mesh**, Graph, Visible Lines, Filtered Points, Kept Indices, Removed Indices |
| Isovist 2D | Points, Obstacles, Radius, Count, **Boundary**, **tol** | **Mesh**, Polygon, Area, Perimeter, Compactness, Max Radial, Drift |

Connect grid Points directly to the analysis Points input. Connect each analysis component's own Mesh to Colorize with that component's values. For matching Graph2D and Isovist results, use the same source points, boundary, obstacles and tol setting. Outer outlines belong in Boundary, not Obstacles.

View Grid 已恢复 Mesh，Cells 取消。可视化请用各分析组件自己的第一个输出 Mesh，并搭配该组件的数值。输入采用同一组 Points、Boundary、Obstacles 和 tol 时，两个分析组件使用相同的点过滤规则。

## Boolean tol

- `false` (default): exclude points on outer boundaries, closed obstacle outlines and open obstacle/wall curves.
- `true`: include those contact points. Points strictly inside solid obstacles or outside every boundary are still excluded.
- Contact classification uses **1e-6 model units**. `tol` is a boolean inclusion switch, not a numeric tolerance input.

`tol=false` 默认不接纳落在边界或障碍曲线上的点；`true` 接纳。闭合实体障碍内部的点始终剔除。接纳接触点不代表取消视线遮挡：朝实体内部或房间外部发出的 Isovist 射线会在起点停止。

## Mesh alignment and the reported warning

The earlier “Mesh is unavailable” warning indicates missing source mesh information. It is not a boundary-contact classification error.

- View Grid and face-sampled View Grid Triangle attach each original face's corners to its point. Analysis Mesh has one face per retained value.
- Curve-sampled View Grid Triangle attaches each vertex's incident triangles. Analysis Mesh has one vertex per retained value, in the same order. Faces with excluded vertices or obstructed edges are removed. Isolated retained vertices may have no renderable face; they remain in the mesh to preserve numeric indexing.
- Converting these samples into ordinary points can discard metadata. Connect directly from a fresh grid component. Plain points still calculate metrics/graphs, but cannot recreate unknown source cells.
- Face filtering removes entire cells/triangles; it does not precisely trim geometry along every obstacle outline.

截图中的提示来自三角网格的来源信息缺失。新版补充了 View Grid Triangle 的连接关系传递；普通网格按面对应值，曲线生成的三角网格按顶点对应值。请重新放置源网格组件并直接连接 Points。

## Migration and local validation

Replace saved View Grid / View Grid Triangle / Graph2D / Isovist components with fresh instances to update their point format and port layouts. Their old saved layouts display migration messages rather than silently wiring mismatched data.

Download: `bin/Release/SpaceVisual_0.2.3.gha`. Local candidate builds remain under `artifacts/candidate/`.

Build: `dotnet build SpaceVisual.csproj -c Release -p:AutoDeployToGrasshopper=false -o artifacts/candidate/0.2.3`.

Run `scripts/development/run-local-candidate.py2` manually in an existing Rhino session for isolated regression checks. The new `analysis-mesh-tol-regression.py2` suite covers exact contact selections, consistent Graph/Isovist counts, quad and triangle heatmaps, topology serialization, origin-contact rays, solid-crossing chords and all-excluded recomputation. These runtime checks have not yet been executed for this candidate.
