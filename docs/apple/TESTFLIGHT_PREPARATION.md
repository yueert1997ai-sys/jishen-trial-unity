# 机神试炼 · iPhone TestFlight 准备

核验日期：2026-09-24。范围：仅 iPhone，横屏；不发布 iPad 或 Mac 版本。

## 当前结论

已准备独立 iPhone 导出入口、可移交 Mac 的工程复制工具、提交文案和真机验收表。尚未生成 Xcode 工程、IPA 或 TestFlight 邀请链接；没有上传或提交审核。

来源是 D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1 当前本地工程，桌面发布指针为 CombatContact_R1。工程包含大量尚未提交的修改，Git 远端不能代替当前工程。Unity 为 6000.3.18f1。当前 Windows 编辑器只安装 Windows Build Support。

Assets 中三个目录联接到 E 盘：CombatArsenalR2、ExtractedHeroesR1、ExtractedHeroesR2。复制工具逐文件展开它们并验证 SHA-256，不能仅拷贝联接或依赖 Git clone。复制包含当前工作区改动，不包含 Library、历史构建、个人存档和 Git 配置。交付目录中的 source-manifest.json 记录文件及哈希；这是工程源文件体积，不代表最终安装包大小。

## 已提供的工程入口

- Assets/Editor/TestFlightBuild.cs：只允许在带 .testflight-staging.json 的隔离副本里以 batchmode 执行。
- 配置 iPhoneOnly、真机 SDK、IL2CPP、Metal、双向横屏，最低 iOS 暂定 15.0，仍需真机验证。
- 数字版本建议 0.1.0，首次构建 1；同版本每次上传增加 build number。
- Bundle ID 必须使用开发者账号内实际注册的值；Team ID 不写死在工程。未创建苹果端标识或 App 记录。
- 工程设置只在副本内变更；导出不会重新生成场景或改桌面发布指针。

## Mac 接续

1. 使用已加入 Apple Developer Program 的团队。确认有创建 App、签名和上传所需权限；新协议需由账号持有人在苹果页面处理。
2. 在 Mac 安装与工程一致的 Unity 6000.3.18f1 和 iOS Build Support，完成 Unity 许可激活。安装苹果支持上传的正式版 Xcode；当前上传下限为 Xcode 26、iOS 26 SDK。SDK 下限与游戏最低运行系统是两回事。
3. 将完整 iPhoneProject 文件夹传到 Mac，保留所有 .meta 文件及隐藏的 .testflight-staging.json。不要运行历史 Windows 场景/资产再生成命令。
4. 苹果 Developer 后台注册 Explicit App ID；App Store Connect 新建 iOS App。名称建议「机神试炼」，主语言简体中文，SKU 建议 MECHTRIAL-IPHONE。名称可用性和最终 Bundle ID 尚未验证。
5. 在 Mac 运行以下命令，替换两个大写占位符：

```bash
bash /path/to/iPhoneProject/tools/apple/export_iphone.sh \
  /path/to/iPhoneProject REGISTERED_BUNDLE_ID APPLE_TEAM_ID 0.1.0 1
```

6. 打开 Builds/iPhone/0.1.0-1/Unity-iPhone.xcodeproj；核对 Unity-iPhone 和相关 target 的签名、Team、Bundle ID。真机选择自己的 iPhone，先运行并完成 device-smoke.csv。
7. 设置最终 App Icon（包含 1024×1024 商店图标）和启动画面，检查 Archive 隐私报告及导出的 PrivacyInfo.xcprivacy。现有应用使用 PlayerPrefs 与本地文件；不能把「无广告/无联网 SDK」等同于「不需要 required-reason API 声明」。按实际 Unity 导出及依赖清单填写，避免重复声明或随意填 reason code。
8. Xcode 选择 Any iOS Device，Product → Archive → Validate App。处理实际校验结果，包括图标、签名、隐私清单及加密出口问题。未核实前不要自动声明使用/不使用非豁免加密。
9. Organizer → Distribute App → App Store Connect 上传。需要以后邀请外部玩家时，不选 TestFlight Internal Only。
10. 处理完成后先加入内部测试组；邀请普通玩家用外部测试组，填写测试说明、反馈邮箱和审核联系人，提交首次 Beta App Review。只有构建可用并获准外测后才发送外部邀请或启用公开链接。

## 提交前还缺什么

| 项目 | 当前状态 |
| --- | --- |
| Apple Developer 付费会员、账号角色、Mac 可用性 | 待用户说明 |
| 正式 Bundle ID / Team ID / App 记录 | 待确定和注册 |
| 反馈邮箱、审核联系人姓名/邮箱/电话 | 待提供，不虚构 |
| App Icon / 启动画面 | 最终发布素材待定，现工程未发现专用 iOS 图标 |
| Xcode 导出、IL2CPP 编译、签名、Archive 校验 | 未执行，当前没有 iOS 模块或 Mac 执行端 |
| iPhone 性能、内存、发热、触屏流程 | 未验证；桌面测试不代表 iPhone 验收 |
| 资产分发授权 | 以下实际资源来源需要确认或替换 |

## 当前资源来源核查

这不是纯粹风格参考：docs/GB4_MOTION_R1.md 明确记录把提取动作转换为 Assets/Resources/GB4Motion/body.bytes；docs/COMBAT_VELOCITY_R1.md 明确记录 VelocityR1 的 36 段音效来自 GB4 音效的处理衍生物；docs/EXTRACTED_HEROES_R1.md 与当前 ExtractedHeroesR2/CombatArsenalR2 资产记录了提取模型链路。

现有 docs/ASSET_PROVENANCE.md 保留早期自制、CC0、CC BY、字体授权等说明，但不足以覆盖这些新增资源。对外分发前，需要取得对应分发授权，或换成有授权的模型、动作、音效；不要把这些资源在审核材料中描述为全部原创。该准备包供项目接续，未作可公开分发声明。也不要仅删除 Resources 文件而不修复引用和重新测试。

## 官方依据（本次已查）

- https://developer.apple.com/news/upcoming-requirements/?id=04282026a
- https://developer.apple.com/help/app-store-connect/test-a-beta-version/testflight-overview/
- https://developer.apple.com/help/app-store-connect/test-a-beta-version/provide-test-information
- https://developer.apple.com/help/app-store-connect/test-a-beta-version/invite-external-testers
- https://developer.apple.com/documentation/xcode/preparing-your-app-for-distribution
- https://developer.apple.com/help/app-store-connect/manage-app-information/add-an-app-icon
- https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/iphone/getting-started/ios-environment-setup

规则会更新；实际上传当天仍以 Xcode 验证及 App Store Connect 提示为准。
