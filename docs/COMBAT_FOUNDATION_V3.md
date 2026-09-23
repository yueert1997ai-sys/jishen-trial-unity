# Combat Foundation V3：战斗与遭遇底层重构

> 历史快照。2026-09-18 的 [Hades Rebuild V4](HADES_REBUILD_V4.md) 已按新批准方案取代这里的通用压力 / Break / 处决规则、六组试场和流程安排；本页保留原始证据，不能作为当前验收标准。

2026-09-17。依据用户本地 Hades 玩法脚本研究战斗、循环与核心表现，按机甲 Demo 的需要重构。工程为 `D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`，分支 `codex/equipment-loop-v1`，实际基线是 CombatLoop_V2_Lab_20260916。

参考分析、函数位置和适配理由见 [Hades 分析与迁移图](HADES_FOUNDATION_ANALYSIS.md)。索引覆盖 100 个 Lua 文件、2205 个顶层命名函数；这是静态词法索引，重点函数已逐段检查，不等于完整动态调用图或原生引擎源码。Hades 的程序、Lua 与美术音频不进入 Demo 构建。

## 实际落地

| 责任 | 新的归属与接入 | 可观察的变化 / 修复 |
| --- | --- | --- |
| 配置 | CombatRules + Resources/Foundation/CombatRules.json | 机动缓冲、武器冲击、敌人阶段、Boss、六组遭遇集中定义，版本和预算校验，只读运行值 |
| 动作请求 | CombatActionQueue 接入 PlayerController / Stance / Melee | 冲刺预输入保留按下时的方向；过期不晚触发；暂停清空请求；E 有短缓冲 |
| 伤害事务 | Damageable.ApplyDamage 返回 DamageResult；旧接口接入同一链 | 实际扣血/过量伤害分离；致命状态先提交；拒绝同目标回调递归伤害；一次死亡结算一次 |
| 反馈 | CombatFeedback 订阅已提交 OnResolved | Break/处决声光从数值计算移到结果消费；可变请求不污染历史命中 |
| 敌人动作 | EnemyAttackCycle：Ready → Windup → Commit → Recovery | 前摇结束才出手；取消代号阻止迟到攻击；普通近战/远程恢复 0.18/0.22 秒，精英 0.70 秒 |
| 遭遇循环 | EncounterFlow 管状态，CombatEncounterDirector 管场景，P0CombatDemo 管 HUD/热键 | 清场、F 等待、下一批、Boss、收尾与胜负分开；已击败 Boss 不会因收尾跨时间限制被判失败 |
| 生成生命周期 | EnemySpawner 管预留、预警、generation | 取消归还对应人数，不误扣活敌、不发击杀奖励；旧请求不在重开后生成 |

原 CharacterController 碰撞、真实弹丸/刀刃扫描、局部命中停顿继续参与生产链路。现有模型、音库与场地作为表现素材使用；本轮重构它们接受战斗结果和动作阶段的方式。永久装备收藏仍由 EquipmentLoop 保存。

## 编译与验证

- 构建：`Builds/CombatFoundation_V3_20260917/MECH_TRIAL_P0.exe`。
- Unity 6000.3.18f1，StrictMode，0 错误、0 警告。
- 游戏程序集 SHA-256：`743569d1fb8cd7a3abf4a78ca70cdb47ae465d16612298abb8662ced2c18e1e9`。
- 同一构建通过 20 组实际 Player 检查、1412 项 PASS，全部退出码 0；精确路径与每组数量见 `AuditEvidence/combat-foundation-v3/test-summary.json`。
- 新基础检查 63 项，覆盖 30/60/120 Hz 下的缓冲方向、过期、暂停、出生取消、旧 generation、伤害快照、递归回调、致命通知与遭遇终态。
- 原有 lab、tactics、loop、break、play、compare、check、gun、rhythm、absorption、contact、regression、terrain、audio、sound、impact、detail、replay、labreplay 全部保留并执行。
- 三组短战斗完成运行、保存、重开和退出恢复；永久收藏存档字节不变。

默认 C 镜头原生实战：1820 帧、60.667 秒，28 击破，剩余 HP 148.51，活敌峰值 4，胜利。基础装备、零强化，以正常 PlayerCommand 驱动，没有无敌、强制伤害或额外回血。Unity 原生混音约 60.651 秒，峰值 0.4521，无削波。

另一套接近并接刀的公开输入策略，M7 / M14 分别 36.30 / 33.58 秒完成固定流程；同起点 Boss 检查为 8.95 / 9.42 秒。两套策略走位与抓窗口方式不同，因此 60.7 秒不是所有玩家的固定时长，不能只凭整场耗时判断武器平衡。

7 秒近/远同帧接触录制包含真实枪击破防、首刀斩杀和连斩击杀，混音峰值 0.4106。34 类音频事件录制峰值 0.3136；空挥无命中音，受中断敌人无迟到枪声。视频帧数、完整解码与 PCM 回读见 `media-verification.json`。

已静默检查远景与接触近景截图，没有打开前台游戏或播放系统声音。自动检查证明接线、状态与回归；本轮玩家手感、听感与反复游玩的意愿尚未验收。

## 源码与回退

修改前保存 770 个文件，`before-foundation-source.zip` 逐文件哈希校验通过，包含真实未提交源码、场景、配置、文档和旧启动指针。V2 Lab 与旧构建保留；没有 Git reset、提交或推送。

`foundation-changes.patch` 对比本次实际工作树基线；`changed-files.json` 与 `combat-foundation-v3-source.zip` 包含本轮脚本与 JSON 配置。发布工具核对全部 20 组最新报告和包文件哈希后，更新原桌面入口。

```powershell
python tools/p0/run_foundation.py build
python tools/p0/run_foundation.py foundation
python tools/p0/validate_foundation.py
python tools/p0/run_foundation.py rhythm
python tools/p0/run_foundation.py tactics
python tools/p0/run_foundation.py sound
python tools/p0/run_foundation.py detail
python tools/p0/run_foundation.py replay
python tools/p0/run_foundation.py labreplay
python tools/p0/package_foundation_media.py
python tools/p0/package_foundation.py
```

后续代码或配置变更需使用新构建目录并重测。Resources JSON 已进入 Player 资源包。`freeze_foundation.py` 是本轮一次性备份，会拒绝覆盖既有基线。

## 已发布回读

2026-09-17 21:44（UTC+8）已发布到原桌面“机神试炼”。148 个构建文件核验通过，Launcher/current.json 指向 CombatFoundation_V3_20260917。按桌面快捷方式同样的 PowerShell 参数检查解析与静音 Player，`Launcher/Verification/20260917-214510` 通过。永久收藏 SHA-256 与修改前一致；35 个场景/动作配置/包和项目设置文件与基线一致；原 V2 Lab 构建保留。精确结果见 `release-readback.json`。
