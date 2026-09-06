# 液态 Boss 与首页入口 · V8

日期：2026-09-07。当前游戏目录：`D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`。

GitHub：`https://github.com/yueert1997ai-sys/jishen-trial-unity`，当前可玩游戏代码在 `codex/equipment-loop-v1` 分支。仓库原有的 `codex/industrial-dash-polish` 分支包含另一个美术制作工作区，不要将其旧游戏版本当作当前 V8。

## 当前行为

启动 `Builds/ValkyrLiquidBossV8/MECH_TRIAL_Valkyr.exe`，首页点红色“挑战液态 Boss”即可跳过前六场。角色携带当前永久仓库装备，并获得六项本局随机强化；战斗结束强化清空。正常挑战没有无敌或自动战斗。WASD 移动、Q / 右键逐次三连斩、左键收刀射击、空格冲刺 / 推进、E 炮击、Esc 暂停。

普通出击仍从第一场开始，最终 Boss 改为 E-01 液态金属侵蚀体。首页保留“苍钢卫士 · 试战”和“武器试场”。使用当前 V7 主角、放大的三连斩、新斩舰刀音效和白色步枪兵；本次没有重新修改主角模型或斩舰刀几何。

## 接入资产

- 独立 Boss 预制体：`Assets/Prefabs/Enemies/LiquidE01Boss.prefab`。
- 冻结的 FBX、材质及液态金属 Shader：`Assets/Art/Enemies/TypeE01Elite/`。
- 86 根骨骼，两个已有 LOD，约 54.6 万 / 22.9 万三角面；沿用原 PC 资产。
- `E01ElitePoseDriver` 按真实战斗状态控制步态、触须、巨爪、核心开合与半血二阶段。
- 保留原有扇形射击、轰击、冲锋、召唤及生命值，合并核心特效和死亡姿态。核心暴露沿用全身受伤倍率窗口，不是独立弱点碰撞体。

资产从 D 盘主工程的已有游戏导出复制，主工程与 Blender 原文件保持只读。`tools/import_liquid_boss_v8.py` 用于本机首次冻结复制；已克隆当前分支时无需再次运行，它遇到不同的现有文件会停止，避免覆盖后续修改。构建使用已经入库的 FBX / Prefab，不依赖另一个工作区或本机 Blender。

## 验证与边界

Windows 构建成功，0 errors / 0 warnings；实际 Player 检查返回 `LIQUID_BOSS_CHECK code=0 errors=0`。检查了首页按钮回调直达 Boss、六项临时强化、实际骨架与材质、V7 刀刃扫掠命中 Boss、核心暴露、二阶段、暂停、死亡到胜利结算、返回首页，以及苍钢卫士与正常出击入口的独立性。

本轮查看了实际 Player 的首页、战斗镜头和 Boss 近景。computer-use 已打开 V8，但用户在暂停菜单处终止了电脑操作，因此没有完成通过桌面鼠标点击挑战按钮的验证。程序输入检查调用的是同一首页按钮回调。没有把这个过程当成完整实机手感验收。

专项检查为了快速验证阶段与结算，启用了仅测试使用的玩家保护、Boss 半血及死亡设置；这些只在 `-liquidBossCheck` 启用，不属于普通游戏逻辑，也不代表自然击败 Boss。没有重复完整战役或移动端性能验收。

可随仓库查看的证据：

- `docs/audit-evidence/2026-09-07/liquid-boss-v8/build-result.txt`
- `docs/audit-evidence/2026-09-07/liquid-boss-v8/quick-check.txt`
- `docs/audit-evidence/2026-09-07/liquid-boss-v8/01_homepage.png`
- `docs/audit-evidence/2026-09-07/liquid-boss-v8/02_boss_arena.png`
- `docs/audit-evidence/2026-09-07/liquid-boss-v8/03_boss_close.png`

完整本地证据位于 `AuditEvidence/liquid-boss-v8/`。V7 的有声动作短片继续保存在 `AuditEvidence/power-combo-v7/`。

## 打开与构建

使用 Unity **6000.3.18f1** 打开仓库目录，等待资源导入，打开 `Assets/Scenes/Demo_Main.unity` 后进入 Play Mode。Windows 本机可在游戏目录执行：

```powershell
# 使用已入库的当前资源重新构建，不恢复动作姿态默认值。
& 'C:/Python313/python.exe' tools/run_liquid_boss_v8.py rebuild

# 可选：独立 Player 的快速接入检查，使用隔离的装备存档。
& 'C:/Python313/python.exe' tools/run_liquid_boss_v8.py check
```

也可使用 Unity `-batchmode -quit -projectPath <仓库路径> -executeMethod LiquidBossIntegration.Build -logFile <日志路径>`，替换为本机 Unity 的实际路径。`Build` 目前输出 Windows64；不是 Mac 原生程序。`build` 模式会重新设置液态 Boss 的场景引用，常规复现使用 `rebuild` 即可。

Git 保存范围包含当前代码、Unity 配置、`.meta`、冻结游戏模型、材质、音频和脚本。`Builds/`、Unity 缓存与完整 `AuditEvidence/` 按项目约定留在本地，不上传到 Git 对象中；旧试玩仍保留。
