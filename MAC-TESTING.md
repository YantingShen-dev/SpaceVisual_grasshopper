# Space Visual — Mac / Rhino 7

本包由仓库源码重新编译，目标为 **Rhino 7 for Mac / Mono / .NET Framework 4.8**。
编译所用 SDK：本机 Rhino **7.8**；平台：AnyCPU。版本和组件 GUID 保持 0.1.0 不变，方便旧 GH 文件识别。

**作者已确认在 Intel Mac / Rhino 7.8 上本地可用。** 这不等于下列全部功能均已逐项验收；编译成功和独立 Mono 测试也不能替代 Rhino 内的几何计算、图标及视口检查。尚未验证 Rhino 8、Apple Silicon 或 Rhino 7.0–7.7。对外发布前请完成下列测试；不要把此包标为全 Mac/Rhino 版本通用。

Windows 原构建保留在 `bin/Debug/SpaceVisual.gha`；Mac 构建保存在 `bin/Mac/Release/SpaceVisual.gha`。两个平台均直接从 `bin/` 分发，不再维护 `dist/`。

## 本机安装

1. 在 Grasshopper 中打开 `File > Special Folders > Components Folder`，以此位置为准。
2. 退出 Rhino。将包中的 `SpaceVisual.gha` 放入该目录。已有同名/旧版本时，把旧版移到 Libraries 之外备份，避免重复组件 GUID。
3. 重新启动 Rhino，执行 `Grasshopper`，查找 **Space Visual** 分类。

本机 Rhino 7 的 Libraries 目录为：

```text
~/Library/Application Support/McNeel/Rhinoceros/7.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries/
```

只安装 `SpaceVisual.gha`，不需要复制 RhinoCommon、Grasshopper、GH_IO、Mono 或编译器。

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
python3 scripts/build-mac.py --compiler /absolute/path/to/tasks/net472/csc.exe
python3 scripts/test-mac.py --compiler /absolute/path/to/tasks/net472/csc.exe
```

输出：`bin/Mac/Release/SpaceVisual.gha`。`build-info.json` 记录源码提交、编译环境和 SHA-256；`managed-test-results.txt` 记录独立 Mono 检查。构建脚本不联网、不自动安装、不上传。

本次适配：项目平台改为 AnyCPU；Windows 自动部署限定在 Windows 执行；新增直接引用本机 Mac Rhino SDK 的构建脚本。核心算法与组件 GUID 保持原样。
