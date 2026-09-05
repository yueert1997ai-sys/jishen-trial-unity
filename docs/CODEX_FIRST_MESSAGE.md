# 发送给 Codex

> 历史启动提示，已被 2026-09-05 手机优先作品 Demo 请求取代。请先读 `MOBILE_SLICE_PLAN.md` 与 README，再按需查看审计基线；不要按下文重新安装包或重建模型。

请继续制作当前 Unity 6 机甲肉鸽上帝视角射击 Demo。

开始前先阅读：

- `README.md`
- `docs/PROJECT_CONTEXT.md`
- `docs/ACCEPTANCE_CHECKLIST.md`

当前临时主角机：

`Assets/UserContent/PlayerMech/Meshy_AI_Rose_Gold_Sentinel_0618172915_texture.glb`

这台机甲先作为客串主角机。不要继续生成机甲模型，也不要在当前阶段做装备外观模块化。

## 本轮目标

尽快交付一个从开始到结算都能玩的短流程 Demo：

`开始任务 → 第一关三波敌人 → 三选一强化 → 第二关 → Boss → 结算`

## 先做资产接入

1. 检查项目 Unity 版本、目录和已有代码。
2. 检查 Unity 能否直接导入该 GLB。
3. 如果不能，优先使用成熟方案 `glTFast`，不要自行写 GLB 解析器。
4. 创建：
   - `PlayerMechRoot`
   - `VisualRoot`
   - `Collider`
   - `WeaponMuzzle`
   - `CameraTarget`
   - `GroundCheck`
5. 将模型作为 `VisualRoot` 子对象。
6. 调整模型：
   - 高度约 2.8 米；
   - 脚底贴地；
   - 正面朝 +Z；
   - 保留材质和贴图。
7. 保存为：
   - `Assets/Prefabs/Player/PlayerMech_Guest.prefab`

模型约 94 万三角面。第一版允许先使用，不要立即卡在降面上。只有 Play Mode 出现明显性能问题时，再记录并执行降面方案。

## 基础玩法

玩家：

- WASD 移动；
- 鼠标瞄准；
- 左键射击；
- 空格冲刺；
- HP、受伤、死亡；
- 枪口从 `WeaponMuzzle` 发射；
- 相机保持 45 度斜俯视并跟随 `CameraTarget`。

敌人：

- 近战追击；
- 远程射击；
- 自爆或高速敌人；
- 一个精英；
- 一个重装 Boss。

流程：

1. 开始任务；
2. 第一关生成 3 波敌人；
3. 清完后暂停战斗并出现三选一强化；
4. 选择后进入第二关；
5. 第二关结束后进入 Boss；
6. Boss 死亡后显示胜利；
7. 玩家死亡后显示失败；
8. 支持重新开始。

强化第一版只做数值或弹道变化：

- 伤害提升；
- 射速提升；
- 多发子弹；
- 穿透；
- 爆炸；
- 最大生命提升；
- 冲刺冷却降低。

## 工作方式

- 先检查当前项目并复用已有可用内容；
- 分阶段执行，每阶段都进入 Play Mode 验证；
- 每次验证读取 Console 并修复阻断性错误；
- 不要一次生成大量未测试脚本；
- 不使用真实高达 IP 名称或素材；
- 不读取项目目录外文件；
- 不执行 `git add .`；
- 修改前后执行 `git status`；
- 所有重要决策和已知问题写入 `docs/HANDOFF.md`。

## 执行顺序

1. 项目和环境检查；
2. GLB 导入并生成玩家 Prefab；
3. 玩家移动、瞄准、射击、冲刺；
4. 敌人和伤害系统；
5. 第一关波次；
6. 三选一强化；
7. 第二关；
8. Boss；
9. 结算和重开；
10. Play Mode 全流程验收。

不要先写长篇计划。先给我简短的环境检查和执行列表，然后开始实际修改项目。
