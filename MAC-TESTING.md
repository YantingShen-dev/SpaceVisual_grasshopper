# Space Visual — Mac / Rhino 7

**Mac 0.2.3 下载：[SpaceVisual_0.2.3_Mac.gha](bin/Mac/Release/SpaceVisual_0.2.3_Mac.gha)**

构建目标为 **Rhino 7 for Mac / Mono / .NET Framework 4.8**，使用 Intel Mac 上 Rhino 7.8 的 SDK 编译，平台为 AnyCPU。内部程序集名称为 `SpaceVisual`，版本为 `0.2.3.0`，正式组件 GUID 保持稳定。

编译和独立 Mono 托管检查通过；**作者已在 Intel Mac / Rhino 7.8 中亲自测试本次 Points 修复版，并确认可用。** 这不等于下列所有组件与场景均已逐项验收。Rhino 8 for Mac、Apple Silicon 和 Rhino 7.0–7.7 尚未验证。

Windows 文件为 `bin/Release/SpaceVisual_0.2.3.gha`；Mac 文件为 `bin/Mac/Release/SpaceVisual_0.2.3_Mac.gha`。只安装一个版本。本地候选和验证输出放在忽略的 `artifacts/mac/`，不替换已安装插件。

## 本机安装

1. 在 Grasshopper 中打开 `File > Special Folders > Components Folder`，以此位置为准。
2. 退出 Rhino。将 `SpaceVisual_0.2.3_Mac.gha` 放入该目录。已有同名/旧版本时，把旧版移到 Libraries 之外备份，避免重复组件 GUID。
3. 重新启动 Rhino，执行 `Grasshopper`，查找 **Space Visual** 分类。

本机 Rhino 7 的 Libraries 目录为：

```text
~/Library/Application Support/McNeel/Rhinoceros/7.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries/
```

只安装 `SpaceVisual_0.2.3_Mac.gha`，不需要复制 RhinoCommon、Grasshopper、GH_IO、Mono 或编译器。

## Points 转换修复

修复了附带网格信息的 Points 输出无法转换为 Grasshopper 原生 Point 的问题。回归检查覆盖 `IGH_Goo` 到 `GH_Point` / `Point3d` 的转换，以及原生 Point 参数接收数据并保持坐标。Graph2D / Isovist2D 建议直接连接网格组件的 Points，以保留附带的单元和拓扑信息；普通 Point 中转会丢失这部分信息。

## 0.2.3 升级

旧 GH 文件中的 View Grid / View Grid Triangle、Build Graph 2D、Isovist 2D 请换成新组件并按端口名称重新连接。外轮廓接 Boundary，内部障碍接 Obstacles；`tol` 是接触点是否参与计算的布尔开关，并非距离。新版 Mesh 输出与筛选后的数值对应，详情见 [0.2.3 工作流](docs/WORKFLOW-0.2.3.md)。

## Rhino 内验收

- **加载**：分类和图标正常，11 个组件可放入画布，无加载异常。
- **2D 基准**：四个点 `(0,0,0)`、`(10,0,0)`、`(10,10,0)`、`(0,10,0)` 接入 Build Graph 2D，不接障碍，预期 6 条可视线；VGA Metrics 2D 的 Connectivity 全为 3；From Viewpoint 2D 从首点出发，Step 为 0、1、1、1；Visual Path 2D 从首点到对角点，长度约 14.1421。
- **遮挡**：增加一条从 `(5,-1,0)` 到 `(5,11,0)` 的障碍线，图应分成左右两组，剩 2 条可视线。左右两组之间应不可达。
- **网格**：闭合平面曲线分别接入 View Grid、View Grid Triangle；检查网格、点及边界裁切，再测试 Surface/Brep 输入。
- **2D Isovist**：在边长 10 的封闭方形中心测试可视域，确保射线长度覆盖边界；面积应接近 100，精度随采样变化。
- **3D**：Isovist 3D 无障碍时检查 SVF 接近 1；增加遮挡网格后应下降。Received Visibility 3D 的输出数量应与障碍网格面数量一致。
- **显示**：Parameter 接 Colorize；检查渐变、图例、文字、缩放以及 Bake。此项必须在 Mac 视口人工观察。
- **旧文件**：用已有 Windows `.gh` 文件确认组件识别、连线、计算结果及保存重开。遇到缺失组件先检查依赖的其他插件是否也已安装。

完成后才手动上传 food4Rhino，并写明实测的 Rhino/macOS/CPU 版本。

## 从源码重新编译

需要 Python 3、Rhino 7 for Mac 和微软 `Microsoft.Net.Compilers.Toolset` 4.8.0（从 NuGet 下载 `.nupkg`，作为 ZIP 解压）。使用包内 `tasks/net472/csc.exe`：

```sh
python3 scripts/mac/build-mac.py --compiler /absolute/path/to/tasks/net472/csc.exe
python3 scripts/mac/test-mac.py --compiler /absolute/path/to/tasks/net472/csc.exe
```

输出：`artifacts/mac/Release/SpaceVisual.gha`。同目录下 `build-info.json` 记录源码提交、编译环境和 SHA-256；`artifacts/mac/tests/managed-test-results.txt` 记录独立 Mono 检查。分发时复制并命名为 `bin/Mac/Release/SpaceVisual_0.2.3_Mac.gha`，不改变内部程序集名称。构建脚本不联网、不自动安装、不上传。

本次适配：项目平台改为 AnyCPU；Windows 自动部署限定在 Windows 执行；新增直接引用本机 Mac Rhino SDK 的构建脚本。核心算法与组件 GUID 保持原样。
