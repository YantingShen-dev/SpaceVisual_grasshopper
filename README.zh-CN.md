# Space Visual · 视线分析工具集

**升级到 0.2.3：** 请重新放置 View Grid / View Grid Triangle、Build Graph 2D 和 Isovist 2D，并按名称重新接线。见 [网格与接触点规则](docs/WORKFLOW-0.2.3.md)。

[项目目录说明](PROJECT-STRUCTURE.md) — 正式版本、开发工具与生成文件的存放位置。

**为 Rhino + Grasshopper 提供从分析网格、视线计算到热力图展示的一套工作流。**

从这里能看见什么？哪些位置更开敞？建筑的哪些表面更容易被看见？Space Visual 将这些问题整合进 **11 个组件、4 个功能分组**，用于建筑、场地与城市空间的视线研究。

[English](README.md) · **简体中文**

[下载与安装](#下载与安装) · [快速开始](#快速开始) · [组件一览](#组件一览) · [源码构建](README.md#building-from-source)

<p align="center">
  <img src="docs/images/visibility-walkthrough.gif" alt="沿路径移动观察点，查看周边建筑表面的可见性着色" width="560">
</p>

*城市视线研究示例：沿路径移动的观察点与周边建筑表面。*

## 分析效果

### 从场地到建筑表面

**Isovist 3D** 从观察点出发研究可见空间；**Received Visibility 3D** 则统计一组观察点对各个网格面的可见情况。分析结果接入 **Colorize**，即可在 Rhino 视口中查看空间分布。

| 场地分析 | 建筑表面分析 |
| --- | --- |
| ![建筑体量周围的场地热力图动图](docs/images/site-analysis.gif) | ![建筑表面的可见性热力图动图](docs/images/surface-visibility.gif) |
| 查看遮挡物周围地面上的数值分布。 | 查看不同建筑表面上的数值分布。 |

### 用热力图阅读空间差异

| 三维分析热力图 | 归一化显示 |
| --- | --- |
| ![场地与建筑网格的分析着色及数值图例](docs/images/site-heatmap.png) | ![使用零到一图例显示的场地与建筑网格](docs/images/normalized-heatmap.png) |

*以上为项目提供的效果素材。解读时请结合所选指标和图例；仅凭 0–1 色标不能判断具体指标。*

### 分析与表达放在同一个工作流

![平面热力图的彩色与灰度显示示例](docs/images/heatmap-palettes.png)

通过 **Parameter** 设置配色、数值范围、图例位置与文字大小，再由 **Colorize** 直接在视口绘制网格、图例和标签。需要将结果保存在 Rhino 文档中时，可使用 Bake。

## 下载与安装

| 平台 | 下载 | 兼容说明 |
| --- | --- | --- |
| Windows 0.2.3 | [SpaceVisual_0.2.3.gha](bin/Release/SpaceVisual_0.2.3.gha) · [发布下载](https://github.com/YantingShen-dev/SpaceVisual_grasshopper/releases/tag/v0.2.3) | 正式 Release 构建，目标支持 Rhino 7（SR0+）与 Rhino 8；0.2.3 的自动化 Rhino 运行回归尚未完成 |
| Mac 0.2.3 — Rhino 7 | [SpaceVisual_0.2.3_Mac.gha](bin/Mac/Release/SpaceVisual_0.2.3_Mac.gha) | 使用 Mac Rhino 7.8 SDK 编译；AnyCPU / net48 / Mono；托管检查通过；作者已在 Intel Mac / Rhino 7.8 中测试 Points 修复版可用 |

Mac 文件名带有 `_Mac` 标记。Rhino 8 for Mac、Apple Silicon 和 Rhino 7.0–7.7 尚未验证，当前确认的是作者本地测试可用，并非所有组件和场景均已逐项验收。

1. 点击对应平台的文件链接，再点击 GitHub 的 **Download raw file**。
2. Windows 用户右键文件 → **属性 → 解除锁定**（若显示此选项）。
3. 在 Grasshopper 中打开 **File → Special Folders → Components Folder**。
4. 退出 Rhino，把对应平台的 `.gha` 复制到该文件夹（Mac 文件为 `SpaceVisual_0.2.3_Mac.gha`）。
5. 重启 Rhino 与 Grasshopper，找到 **Space Visual** 标签页。

Windows 常用目录为 `%APPDATA%\Grasshopper\Libraries\`。Mac 请以 Grasshopper 打开的目录为准，详细路径与验证清单见 [Mac 安装说明](MAC-TESTING.md)。

只保留一份 Space Visual；关闭 Rhino 后移除旧版，避免重复组件。

本次 Windows 正式版为 **0.2.3**，升级步骤见 [更新记录](CHANGELOG.md)。同时检查 Rhino PackageManager 中是否另有旧版。临时测试版定义需要换成正式组件，再移除 **Space Visual test** 插件；两个版本的自定义 Graph、Parameter 类型不能混接。Windows 下载为 `bin/Release/SpaceVisual_0.2.3.gha`，Mac 下载为 `bin/Mac/Release/SpaceVisual_0.2.3_Mac.gha`。两者内部程序集名称和组件 GUID 相同，只能安装一份；不再提供 Debug、旧 Mac 0.1.0 或测试版二进制。

## 快速开始

### 做一张二维可视性热力图

1. 准备闭合外边界与同平面内部障碍曲线，边界接到 **View Grid** 或 **View Grid Triangle**。
2. 将网格组件的 **Points 直接连接**到 **Build Graph 2D → Points**，保留网格信息；外边界接 `_Boundary`，内部障碍接 `_Obstacles`。边界曲线需要单独连接。
3. 最后一个布尔输入 **tol** 默认为 `false`，剔除落在边界或障碍曲线上的点；设为 `true` 则接纳接触点。闭合障碍内部、外边界之外的点始终剔除。tol 不是数值距离容差。
4. **Graph** 接 **VGA Metrics 2D**，指标接 **Colorize → Values 0**；**Build Graph 2D 的第一个输出 Mesh** 接 **Colorize → Mesh**。不再需要 Grid Mesh 输入。
5. **Isovist 2D** 同样连接 Points、Boundary、Obstacles，使用它自己的第一个 **Mesh** 输出配合 Area 等数值。两个分析组件使用相同输入和 tol 设置时，过滤点的顺序一致。

View Grid 保留 **Mesh、Points** 输出，取消中间版本的 Cells。原始 Mesh 用于预览；热力图使用分析组件自己的 Mesh。关闭上游网格和点预览，才能只显示过滤后的结果。

普通网格及由 Mesh/Surface/Brep 生成的三角网格按面对应数值；曲线生成的三角网格按顶点对应数值，并删除无效三角面。过滤删除完整网格单元，不沿障碍轮廓精确裁剪。将自定义采样点转换为普通点可能丢失网格信息，请直接连接 Points。

**旧定义升级：** 重新放置 View Grid / View Grid Triangle、Build Graph 2D、Isovist 2D，按新版端口名称接线；外轮廓接 Boundary，内部障碍接 Obstacles。所有点与曲线必须位于同一 XY 平面。[详细说明](docs/WORKFLOW-0.2.3.md)。

### 换一个分析问题

| 想回答的问题 | 接线方向 |
| --- | --- |
| 在平面中，从这里能看见什么？ | 观察点 + 遮挡曲线 → **Isovist 2D** |
| 每个位置的天空有多开敞？ | 观察点 + 遮挡网格 → **Isovist 3D** → `SVF` → **Colorize** |
| 哪些表面能被更多观察点看见？ | 观察点 + 目标/遮挡网格 → **Received Visibility 3D** → `Hit Count` → **Colorize** |
| 可视图上的最短路径是什么？ | **Build Graph 2D** → **Visual Path 2D**，另接起终点 |
| 到目标位置需要几次视线转换？ | **Build Graph 2D** → **From Viewpoint 2D** |

### 数据与参数

- 三维分析优先使用 Mesh；组件支持的 Brep/Surface 输入会在内部转换为网格。视距与网格密度应结合模型单位设置。
- Isovist 3D 默认采样上半球。SVF 是**给定视距内**未受遮挡天空的余弦加权估计，受采样数与最低仰角影响。
- Received Visibility 的结果按输入障碍网格的面顺序依次排列；着色时应使用相同顺序的网格面。
- Colorize 的数值数量必须对应网格面数或顶点数。多个 Values 输入按相同索引等权平均，混合不同量纲指标前应先归一化。该组件直接预览、不输出数据，可通过 Bake 固化结果。
- 建议先用粗网格检查接线与几何。可视图构建需要成对检查观察点，点数增加会明显增加计算量。

## 组件一览

| 分组 | 组件 | 作用 |
| --- | --- | --- |
| Build | **View Grid Triangle** | Curve / Surface / Brep / Mesh 转三角分析网格与面中心点；通过间距控制密度 |
| Build | **View Grid** | 闭合平面 Curve、Surface 或单面 Brep 转四边分析网格与面中心点 |
| Analyze 2D | **Build Graph 2D** | 根据观察点与遮挡曲线构建可视图，输出可见连线 |
| Analyze 2D | **Isovist 2D** | 输出可视多边形、面积、周长、紧凑度、最大半径与重心偏移 |
| Analyze 2D | **VGA Metrics 2D** | 整合度（平均深度的倒数）、熵、控制度、聚类系数与连通度 |
| Analyze 2D | **From Viewpoint 2D** | 输出可视图步数、直线距离与方位角；不可达节点返回 NaN |
| Analyze 3D | **Isovist 3D** | 采样视线、估计体积/表面积、最大/平均半径、重心偏移与 SVF |
| Analyze 3D | **Received Visibility 3D** | 每个面的可见观察点数量、平均/最小距离、法线对齐程度与面中心 |
| Visualize | **Visual Path 2D** | 可视图上的 A* 最短路径；起终点吸附到最近的图节点 |
| Visualize | **Parameter** | 设置配色、数值范围、图例位置与图例/文字缩放 |
| Visualize | **Colorize** | 视口热力图、图例和标签，支持 Bake 与多组数值等权混合 |

## 源码与参与

Windows 与 Mac 的构建命令见 [英文 README 的源码构建说明](README.md#building-from-source)。Mac 的功能检查见 [MAC-TESTING.md](MAC-TESTING.md)。

欢迎通过 [Issues](https://github.com/YantingShen-dev/SpaceVisual_grasshopper/issues) 提交问题与使用反馈；请附上平台、Rhino 版本、复现步骤，以及可分享的最小示例。

[MIT](LICENSE) © 2026 POLY LAB · [Yanting Shen](https://github.com/YantingShen-dev)
