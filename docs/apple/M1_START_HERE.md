# M1 Mac 接续：机神试炼 iPhone

本次交付为 2026-09-24 当前源码开发快照，位于私有仓库的 `codex/iphone-testflight-handoff-20260924` 分支。最新已发布桌面版为 CombatContact_R1；源码还含后续开发改动。历史桌面测试不代表此快照已通过 iOS 编译或真机验收。

## 今晚先在 Mac 打开工程

1. 安装 Git LFS（已有 Homebrew 时可用 `brew install git-lfs`），然后运行：

```bash
git lfs install
git clone --branch codex/iphone-testflight-handoff-20260924 --single-branch \
  https://github.com/yueert1997ai-sys/jishen-trial-unity.git
cd jishen-trial-unity
git lfs pull
git lfs fsck
```

这是私有仓库，使用你有权限的 GitHub 账号登录。GitHub HTTPS 认证使用令牌或凭据管理器，不是 GitHub 网页登录密码。不要把令牌发到聊天。

2. 在 Unity Hub 安装 **Unity 6000.3.18f1 / Apple silicon**，同时勾选 **iOS Build Support**。工程不会从 Git 携带 Library 缓存，首次导入需要时间。
3. Unity Hub 添加刚克隆的 `jishen-trial-unity` 文件夹，打开 `Assets/Scenes/Demo_Main.unity`。先确认 Console 没有编译错误且机库能运行。不要执行历史 Windows 资产重新生成工具；相关模型、动作、贴图已经随本次 Git 快照提供。
4. 安装当前 macOS 支持的正式版 Xcode，并首次打开完成组件安装及许可确认。此 Mac 的具体 macOS 版本、Xcode 兼容性尚未远程核验；M1 型号本身不证明当前系统已经满足要求。

## 准备自己的 iPhone 试玩

先装自己的手机不需要付费开发者会员；使用 Xcode 的 Personal Team，免费签名到期后重新构建安装。正式 TestFlight 分发再使用 Apple Developer Program 团队。

使用 Python 3（复制工具兼容 Python 3.9+）生成独立构建副本：

```bash
python3 tools/apple/prepare_testflight.py \
  --stage ../jishen-iphone-build \
  --report ../jishen-iphone-source-manifest.json
```

不要提前创建 `../jishen-iphone-build`，工具要求它是一个新目录。副本带有导出所需的 `.testflight-staging.json`；Git 主工程不应直接调用导出入口。

下面的 `dev.local.jishentrial.personal` 仅是个人真机调试示例标识，不是已注册的正式 Bundle ID。按你的 Xcode 账号要求换成唯一标识。第三个参数空字符串表示稍后在 Xcode 选择 Team：

```bash
bash ../jishen-iphone-build/tools/apple/export_iphone.sh \
  ../jishen-iphone-build dev.local.jishentrial.personal "" 0.1.0 1
```

脚本按后续 TestFlight 的工具链要求检查 Xcode 26+ / iOS SDK 26+。导出成功后打开：

```text
../jishen-iphone-build/Builds/iPhone/0.1.0-1/Unity-iPhone.xcodeproj
```

在 Xcode 登录自己的 Apple 账号，在 Signing & Capabilities 选择 Personal Team 并启用自动签名。连接并信任 iPhone，按系统提示开启开发者模式；选择这台手机作为运行目标后 Run。具体签名错误以 Xcode 实际结果为准。

## 给 Mac 上的 Codex 的接续指令

> 请在此仓库继续准备机神试炼 iPhone 版。先读 docs/apple/M1_START_HERE.md 和 TESTFLIGHT_PREPARATION.md。核验 Unity 6000.3.18f1 Apple silicon、iOS 模块、macOS、Xcode 和 Git LFS 资源。先完成 Xcode 导出与我自己的 iPhone 真机安装，记录编译和运行问题。付费会员状态尚未确认，不要宣称已经上传 TestFlight。保持现有游戏资源和玩法，不要调用带 Windows 绝对路径的历史资产重建脚本。

## 已知交接边界

- 三个原本指向 E 盘的资产目录已展开为 Git 文件；Mac 不需要 D/E 盘或另找解包目录。
- Windows 端无法从当前中国镜像下载对应 iOS 模块，收到 404；这不等于 Mac 安装器也不可用。应在 Mac 上核验。
- 最终图标、隐私清单、审核联系人和分发授权事项见 `TESTFLIGHT_PREPARATION.md`；目前未生成可提交的 IPA、未上传 TestFlight。
- 运行 14 项 `device-smoke.csv`，记录 iPhone 型号、系统、构建号与实际结果。
