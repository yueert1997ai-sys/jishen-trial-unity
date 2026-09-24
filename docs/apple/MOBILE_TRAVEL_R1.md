# iPhone 旅行试玩适配 R1

目标设备：用户的 iPhone 17 Pro Max。分发选择：香港区、免费下载，暂不接内购。此改动位于 `codex/iphone-testflight-handoff-20260924`，继续按 [M1_START_HERE.md](M1_START_HERE.md) 在 M1 Mac 导出并安装。

## 已实现

- 移动端禁止键鼠兜底输入：右摇杆松手后停止射击，不再跳到鼠标瞄准点。
- 多指分工保留；摇杆和推进按钮在取消、失焦、后台切换时释放指针及持续输入。
- 切后台保存设置、暂停战斗及音频；返回后点击继续，避免一回游戏就被打。
- 战斗期间保持亮屏；暂停与机库恢复系统休眠策略。
- 默认均衡画质、60 帧目标；设置页可切换并保存 30 帧省电模式。目标帧率不是实测帧率或续航承诺。
- 增加“回收并安装”触屏按钮，手机无需 F 键。装备沿用先保存所有权再安装的本地存储规则。
- 血量、能量、弹药和技能状态移至上方，避开底部双摇杆；暂停按钮增高，移除相关键鼠提示。
- 手机菜单随安全区缩放，设置页保留全部按钮；修正音量滑块手柄过高的问题。

## 验证

同一份 Windows Player 构建运行：mobile 35、foundation 63、regression 205、weaponhud 7，共 **310 项 PASS，四组均 errors=0**。

Assembly-CSharp.dll SHA-256：`3b446c6c77cc3eec41ad698b65964ae00ece682ce2bddb67317cc591f16d7722`。

`MobilePlatform` 仅在真实移动平台或明确带 `-batchmode -mobileTravelCheck` 的测试进程启用触控路径。测试使用真实生产组件、双指事件、系统回调模拟、实际回收按钮和装备文件读回；模拟左右安全区，并检查血条和弹药面板不与摇杆重叠。测试 Player 使用独立产品名，装备存档使用独立路径。

Windows 重跑命令：

```powershell
python tools/p0/run_mobile_travel.py build
python tools/p0/run_mobile_travel.py mobile
python tools/p0/run_mobile_travel.py foundation
python tools/p0/run_mobile_travel.py regression
python tools/p0/run_mobile_travel.py weaponhud
```

本机日志与截图位于 `AuditEvidence/mobile-travel-r1/`，构建在 `Builds/MobileTravel_R1/`，不上传这些大体积运行产物。Windows 模拟截图不是 iPhone 实机截图。

## Mac 接续与边界

尚未完成 iOS/IL2CPP 编译、Metal 渲染、签名安装、真实触控、音频中断及连续 20 分钟温度/耗电测试，不能据此称已上 TestFlight。用 [device-smoke.csv](device-smoke.csv) 记录真机结果。

装备与设置已有本地保存，但当前战斗没有新增跨进程续玩：切后台不被系统终止时保持暂停；被系统终止后需重新进入战斗。真机仍需测试飞行模式冷启动及存档读回。

此分支保留手机交付快照。D 盘 `GameplayLoop_V1` 同期存在另一轮界面和战斗开发改动；本轮未覆盖该工作区或更换桌面启动器。后续合并时保留两侧改动，重点复核 CombatHUD、SettingsUI、RuntimeUIFactory 和 HangarDeploymentUI。
