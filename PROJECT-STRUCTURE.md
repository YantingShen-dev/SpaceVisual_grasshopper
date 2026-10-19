# Project layout / 项目目录说明

The current Windows release is **0.2.1**. Obsolete compiled plugins and binary backups have been removed. Development scripts and regression tests below work with the current source; they are not older plugin versions.

当前 Windows 正式版为 **0.2.1**。旧插件、旧测试二进制和备份已清除。`scripts` 和 `tests` 保留的是当前源码使用的工具，不是旧版本安装包。

| Location | Purpose / 用途 |
| --- | --- |
| `bin/Release/SpaceVisual.gha` | Current production plugin / 当前正式插件 |
| `bin/Release/SpaceVisual.pdb` | Local matching debug symbols; not another plugin / 配套调试符号，不是另一版插件 |
| `dist/0.2.1/` | Local release assets: GHA, ZIP, Yak, checksums and release notes / 当前发布文件 |
| `scripts/release/` | Package the production release / 正式版打包工具 |
| `scripts/development/` | Optional separate test build and Rhino example tools / 按需使用的测试版构建及示例工具 |
| `scripts/mac/` | Mac source-build and smoke-test tools; no Mac binary distributed / Mac 源码构建工具，无已发布二进制 |
| `tests/regression/` | Current 2D visibility and point-filter regression checks / 当前功能回归检查 |
| `tests/mac/` | Mac managed smoke-test source / Mac 基础测试源码 |
| `artifacts/validation/` | Generated reports and local test examples, created only when tools run / 运行工具时生成的报告和示例 |
| `Analyze2D`, `Analyze3D`, `Build`, `Core`, `Visualize` | Current plugin source / 当前插件源码 |
| `docs/images`, `icon` | Documentation images and component icons / 文档图片与组件图标 |
| `.git`, `.vs` | Git history and local Visual Studio settings / 版本历史及编辑器设置 |

## Generated files

`obj/` is disposable compiler cache. It has been cleared and will be recreated by a build. It is never an installation folder. `bin/Test/`, `bin/Debug/` and `bin/Mac/` are generated only when those configurations are explicitly built; they are ignored by Git. Test installation replaces the previous test binary directly without accumulating backups.

`obj/` 是已清理的编译缓存，重新构建时会自动生成。开发构建可能生成 `bin/Test`、`bin/Debug` 或 `bin/Mac`；这些路径与正式版 `bin/Release` 分开，且不上传 GitHub。测试安装会直接替换上一次测试二进制，不再累积备份。

Release packaging uses temporary files under `artifacts/packaging/` and removes its staging directory when finished. It refuses to overwrite existing release assets, preserving published checksums. `dist/` and `artifacts/` are local generated outputs and are ignored by Git; public packages are on [GitHub Releases](https://github.com/YantingShen-dev/SpaceVisual_grasshopper/releases).

See [development testing](TESTING.md) and [Mac build notes](MAC-TESTING.md) for explicit build commands. These tools do not launch Rhino automatically.
