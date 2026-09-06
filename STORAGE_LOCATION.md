# MECH ROUGE 存储位置

2026-09-06 已完成项目迁移与 C 盘旧副本清理。

- 项目实际位置：`D:\project-mecha-design\MECH ROUGE`
- 旧入口：`C:\Users\yue\Documents\MECH ROUGE`，现为指向 D 盘项目的目录连接，不再保存重复项目文件。
- 全部 Blender 工程、历史版本、参考素材、渲染、游戏构建、交付和 Git 历史已随项目迁移。
- Blender 用户配置与资源缓存：`D:\Tools\BlenderUserData`；原配置路径保留兼容连接。
- Blender 软件本体仍位于 `C:\Program Files\Blender Foundation\Blender 5.2`，本次没有迁移或卸载软件本体。

迁移前逐个比较了 15,837 个项目文件的 SHA-256。切换后检查文件大小、修改时间、目录结构与 6 个外部渲染目录连接，确认未发生遗漏或改动，再删除 C 盘重复暂存目录。主角、小兵和武器的 Blender 工程均从 D 盘成功读取，未发现缺失外部资源；Git 主项目与工作副本可读取，原有未提交修改保持一致。

两份此前打开、含未保存状态的 Blender 会话已单独保存恢复副本：

- `D:\project-mecha-design\_migration_20260906\OPEN_SESSION_41896.blend`
- `D:\project-mecha-design\_migration_20260906\OPEN_SESSION_47404.blend`

核验记录位于 `D:\project-mecha-design\_migration_20260906`。C 盘旧重复暂存目录已经移除；保留的目录连接用于兼容已有工程引用。

## E-01 当前进度

当前小兵模型为 `art_prototypes\Type_E01_20260906\stage_04_TYPE01\TYPE_E01_TYPE01_RIFLE_MASTER.blend`。用户已确认第三阶段机体造型，随后要求装入已完成的 TYPE-01 激光步枪；本工程已完成换装及垂持、双手持枪适配，更新后的真实四视图和预览均在 stage_04_TYPE01 内。第三阶段机体与独立步枪源工程保留。

## 后续默认存储位置

用户明确指定 D 盘作为后续存储盘。已于 2026-09-06 补齐 Blender 默认设置：

- 通用产出：`D:\CodexOutputs`。
- 通用 Blender 工程、渲染、导出：`D:\CodexOutputs\Blender\Projects`、`Renders`、`Exports`；文件浏览器已添加 D 盘目录书签，近期工程入口已改为 D 盘。
- 新建工程默认渲染：`D:\CodexOutputs\Blender\Renders`。
- Blender 临时文件、渲染缓存、纹理缓存、资源库：`D:\Tools\BlenderUserData` 下的 `Temp`、`RenderCache`、`TextureCache`、`Assets`。
- 三个当前模型工程的默认渲染目录均改为各自 D 盘工程中的 `renders`。小兵制作脚本也已固定默认输出位置。
- 后续制作的存储规则已写入项目 `AGENTS.md`。

已重新启动 Blender 核对偏好设置及实际临时目录，使用已保存的默认输出位置完成一次 64×64 渲染，确认图片实际写入 D 盘；测试图片已移除。三个当前模型重新打开后对象和网格数量保持一致，未发现缺失贴图。

核验记录：`D:\project-mecha-design\_migration_20260906\blender_d_defaults_verified.json`。修改前设置和三个模型的恢复副本位于同目录的 `settings_before_d_defaults`。

软件本体仍安装在 C 盘。本次只设置存储与输出位置，未改动 Windows 或其他软件的全局临时目录。
