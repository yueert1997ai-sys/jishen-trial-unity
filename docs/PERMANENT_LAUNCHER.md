# 固定试玩入口

桌面一直使用「机神试炼」图标。它通过 `Launcher/launch.ps1` 读取当前已验证版本，默认最远 C 视野。

更新只切换 `Launcher/current.json`，不改桌面图标。存档固定在 `Launcher/UserData/profile.json`，不会因为换构建目录而换存档。首次设置时，若 R5 已有试玩存档，会复制到固定位置；原文件保留。

开发者完成独立构建和实际测试、生成构建文件清单后，使用 `tools/p0/publish_playable.py` 发布到该入口。工具核对文件哈希与指定测试的最新结果，验证通过才切换；此前的入口配置自动保留到 `Launcher/History`，旧构建也应保留。

当前开发进度：Combat Foundation V3 已通过实际 Player 验证，包含现有 F 吸收及短战斗对照，见 [重构记录](COMBAT_FOUNDATION_V3.md)。实际发布版本始终读取 `Launcher/current.json`。作品集 Demo 的整体手感仍待用户实际体验验收。
