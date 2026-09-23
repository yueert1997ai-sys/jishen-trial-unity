# P0.9 — 冲击失衡与 OVERDRIVE 粒子反馈

2026-09-10，版本 `p0.9-overdrive-20260910`。沿 P0.8 的五分钟战斗原型继续改进，工程为 `D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`，分支 `codex/equipment-loop-v1`。保留主角、实体 M7、真实刀路三连砍、月面碰撞与手动输入。

## 本轮玩法

- 敌人有独立于生命的冲击条：步枪默认产生伤害值 1.7 倍冲击，三刀分别产生 42 / 38 / 95 冲击。普通敌人阈值 85，精英 145。持续射击保留压力，停火 1.1 秒后以每秒 42 恢复。
- 达到阈值后进入 1.35 秒失衡，取消当前攻击协程、停止移动。造成破防的那一下仍是基础伤害；窗口内普通攻击 1.35 倍，重刀 1.85 倍。追击不延长窗口，恢复后有 0.65 秒冲击保护，避免无限硬直。暂停冻结窗口。
- 修正生成链的实际缺陷：场景近战、远程、精英槽共用步枪兵美术模板，旧生成代码没有应用请求的战斗类型。现在生成后分别应用突击兵、步枪兵、精英逻辑与冲击阈值。沿用外观，未声称制作三个新模型。
- P0 的生命下限为突击兵 110、步枪兵 86、精英 230，给压制与追击留下空间；突击兵靠近攻击，步枪兵维持远距，精英使用较高抗冲击和散射。35 秒后每四组中一组为单个精英，场内上限仍为 7。
- 敌人头顶显示角色、生命和冲击条；失衡时显示 `BREAK` 与青色条。
- 无敌、零实际伤害不积累冲击；被拒绝的刀伤不再继续产生刀击回调。三连砍的既有输入、碰撞、握持与推进距离保持原有验证要求。

## 视觉

- 真实命中触发橙金装甲火花；重击/直击使用蓝青爆光与扩散环。击杀增加热碎屑、余烬、冲击环和短时照明。
- 失衡窗口伴随装甲电弧。推进增加启动爆发、柔化尾焰、离子残光；效果沿真实推进器插槽发射。
- 新增选择性 Bloom，优先扩散高亮彩色发光，减少灰色地面和白装甲整体泛白。
- 默认 `OVERDRIVE`，F2 切换标准/高强度；此切换只影响表现。新增粒子共用四个发射器，上限 3904；电弧固定 24 条，动态光固定最多 3 盏，无阴影。逐帧发射预算有上限，复用材质和对象；退出战斗清理残留。

## 运行与复查

构建位于 `Builds/P0_9_Overdrive/MECH_TRIAL_P0.exe`，旧 `Builds/P0_CombatDemo` 保留。通过主目录 `PLAY_P0_DEMO.cmd` 启动；桌面「机神试炼 · P0试玩」指向此入口。F1 自由练习，R 重新开始，F2 调整特效。

在本工程运行：

```powershell
C:/Python313/python.exe tools/p0/run_overdrive.py build
C:/Python313/python.exe tools/p0/run_overdrive.py check
C:/Python313/python.exe tools/p0/run_overdrive.py terrain
```

`tools/p0/run_p0.py` 同样指向当前 P0.9，避免构建新版、检查旧版。所有回放使用隔离的临时装备存档与隐藏、静音的独立 Player。

验证文件：`AuditEvidence/p0-9-overdrive/build-result.txt`、`check/quick-check.txt`、`terrain/quick-check.txt`。新增专项验证覆盖实弹破防、真实连招重击追击、敌人起手中断、角色生成、暂停/恢复/复活、粒子上限及消散。原有验证覆盖 30/60/120 FPS 动作、握持、推进、掩体、枪声事件与 300 秒稳定性回放。画面来自真实游戏相机，`check/overdrive_*.png`，不是概念图。

最终构建 `Succeeded errors=0 warnings=0`；战斗及月面回归均 `P0_CHECK code=0 errors=0`，1084 条月面路径通过。最后一次七处爆炸离屏测试平均 11.58ms、P95 17.21ms（RTX 4070 Ti）。动态光由首轮 6 盏减到 3 盏，保留粒子密度；首轮同类测试为 13.70ms / 19.85ms，样本不是严格硬件隔离的对照实验。最终程序集哈希与交付入口记于 `AuditEvidence/p0-9-overdrive/delivery-manifest.json`。

压力测试是 1600×900、七处周期性爆炸的离屏相机渲染加 GPU 同步读回，具体数值记录在 `check/quick-check.txt`；不能等同屏幕 FPS 或全流程帧耗时。300 秒回放使用固定模拟步长和玩家无敌，不能算真人难度/自然通关或主观爽感验收。没有进行有声试听。

## 参考与范围

依据 [Bandai Namco 的 AC6 官方战斗指南](https://en.bandainamcoent.eu/armored-core/news/armored-core-6-fires-of-rubicon-beginners-guide-basic-strategy-and-combat-theory) 中的冲击积累、失衡追击，以及边机动边进攻的原则，做适配当前俯视原型的实现。本轮阅读官方文字资料；未声称观看完整实机视频。数值和效果为本项目设计。

玩家自身失衡、AC6 式完整三维空战、战场 E 抢武器和新 Boss 招式没有在本轮实现。仍保留 P0 既定范围，不扩展装备掉落、关卡或局内强化系统。

修改前代码备份为 `AuditEvidence/p0-9-overdrive/before/source-baseline.zip`，旧画面对照同目录；已有未提交内容保留，本轮没有提交或推送。
