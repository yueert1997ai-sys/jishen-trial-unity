# 项目存储约定

用户于 2026-09-06 明确要求：D 盘作为存储盘，之后的输出、导出和工程文件全部放在 D 盘。

- 项目实际根目录为 `D:\project-mecha-design\MECH ROUGE`。执行制作、渲染、导出和构建时优先使用这个路径。
- 本项目的 Blender 工程、参考素材、预览图、渲染图、模型导出、游戏构建、交付文件、中间文件和工作日志，均保存在 D 盘项目内对应目录。
- 不属于特定项目的通用产出放在 `D:\CodexOutputs`。Blender 通用工程、渲染和导出目录分别为其下的 `Blender\Projects`、`Blender\Renders`、`Blender\Exports`。
- Blender 临时文件、渲染缓存、纹理缓存和资源库位于 `D:\Tools\BlenderUserData`。
- `C:\Users\yue\Documents\MECH ROUGE` 是指向 D 盘的兼容入口。不要在 C 盘重建实际项目副本或新增交付目录。
- 本规则约束存储与产出位置。现有软件、系统字体等读取依赖可继续使用其安装路径。
