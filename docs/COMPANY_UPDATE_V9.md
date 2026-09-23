# V9：公司更新接入当前 Windows 试玩

2026-09-08。以 `codex/equipment-loop-v1` 的 V8 提交 `e941bfb` 为基础，接入公司分支 `codex/industrial-dash-polish` 提交 `5251340` 的资源。没有整体覆盖旧版战斗代码。

当前启动 `PLAY_V9.cmd`，入口指向 `Builds/ValkyrHangarV9_Type08Starter/MECH_TRIAL_Valkyr.exe`。实际目录位于 `D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`，既有构建保留。

- 新格纳库、V3 机身与配色、全身/近景查看、拖动环绕和滚轮缩放。
- 进入机库默认装备 TYPE-08 等身长重型加农炮，右手持用，左键发射白粉亮芯与粉色光晕的米诺夫斯基粒子光束；直接出击即可使用，返回机库恢复默认装备。可切换 M7、RAIKEN 或 M14，或手动卸下。M7 连射，M14 重型穿透射击，使用真实武器枪口。
- RAIKEN 沿用 V7 已调过的 Mk-II 实体刀、活动手指、反手低位准备、三连斩、真实刀身扫掠、蓝色光刃和原有音效。没有用公司分支较早的单次 42 点近战替代它。左键、Q 或右键逐次按下连击。
- 步枪配装不产生无形刀伤，巨剑配装不发射无形步枪。E 保留肩炮技能与收刀状态切换。WASD 移动，空格点按冲刺、按住推进。
- 收藏仓库中的旧武器/背包继续可选。选择收藏武器会进入原有枪刀自动切换模式；白兵掉落和永久仓库保留，局内 buff 不存入仓库。修正了七件收藏品的分栏与选中显示。
- MARE-07 月面场景完成 Blender 静态导出、14 组网格、Unity 基础材质、网格碰撞和导航烘焙，替换前三场战区。后续反应堆战区、液态 Boss、旧机体试战和武器试场保留。月面隐藏旧甲板，避免地面重叠。

源机体/武器来源见 `Assets/Art/HangarLoadout/sources.json`。月面主文件为主工作区 `art_prototypes/LunarBase_Sector01_20260908/MARE07_LUNAR_BASE_MASTER.blend`；本轮只读导出，没有改写建模工程。程序微纹理没有烘焙，Unity 采用基础配色与粗糙度。

## 实际执行与检查

当前含 TYPE-08 的构建与验证使用 `C:/Python313/python.exe tools/run_type08_v9.py build` 和 `C:/Python313/python.exe tools/run_type08_v9.py check`。该脚本会注册炮的独立预制体并设置起始装备，再生成当前启动入口使用的构建。它会保留 V9 现有主角、收藏仓库和其他武器。`Collection` 序列化编号仍为 4，新增 `Type08` 为 5。最新证据在 `AuditEvidence/type08-starter-v9/`，独立回放结果为 `check/quick-check.txt`。

2026-09-08 22:01 构建版本 `hangar-lunar-v9-20260908.140123`：Windows 构建 0 错误、0 警告；新开局默认持炮、直接出击、聚光后命中、粉色光束材质与炮口方向、冷却、余辉清理、返回机库、卸下、换武器、收藏仓库、M7/M14、三连斩、月面白兵导航、液态 Boss 和持炮推进全部通过，运行日志 0 错误。主工程原有启动快捷方式也指向该构建。

以下命令记录首次 V9 资源导入流程：

```powershell
# 在此 gameplay 工作区运行
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --python tools/export_lunar_v9.py
C:/Python313/python.exe tools/run_company_v9.py integrate
C:/Python313/python.exe tools/run_company_v9.py build
C:/Python313/python.exe tools/run_company_v9.py check
```

`integrate` 生成 V9 独立主角预制体、月面导航并打包；后续代码改动使用 `build`。测试使用独立临时仓库文件，正常启动不会加载测试脚本。原始 Blender 场景位于相邻主工作区，公司资源已随本工作区保留，可直接重新构建 Windows 试玩。

Unity 6000.3.18f1，Windows x64，构建 0 错误、0 警告。实际独立 Player 回放检查通过，运行日志 0 错误：空手出击限制、M7/M14 实际命中、战斗期间配装锁定、双手握点、三连斩命中、仓库旧装备选择、液态 Boss 与六项强化、月面白兵导航以及持枪推进。

首次预览发现步枪左手接触偏差约 40 cm，调整枪位后两把枪的双手误差均小于 0.001 cm。月面旧公共甲板已单独隐藏。已实际打开查看游戏生成的机库、持枪、三连斩、Boss、仓库和月面截图。

证据：`AuditEvidence/company-update-v9/build-result.txt`、`check/quick-check.txt`、`check/*.png`。这是短流程预览，不代表完整通关、所有帧率与全部动画质量验收。

## 保存状态

当前改动保存在 `codex/equipment-loop-v1` 工作区，未自动提交或推送。本机建模主工作区的既有未提交改动与旧构建均保留。
