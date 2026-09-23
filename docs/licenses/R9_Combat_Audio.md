# R9 战斗音源

新银行：`Assets/Resources/Audio/Combat/R9`，47 个 48 kHz 单声道 PCM 文件。Unity 导入关闭自动归一化。旧音库保留供旧模式回归，切片运行时改用 R9。

## 实录来源

- **mnslugger20 — M4 Assault rifle firing.wav**：[作者与 CC0 授权页面](https://freesound.org/people/mnslugger20/sounds/259758/)。使用项目已经保存的 `AuditEvidence/p0-audio-v3/sources/M4_recorded.mp3`。三个分离的单发起音截取均短于原素材连射间隔，加入低频重量和单独机构声；没有把完整连射片段作为一发播放。这是机甲 M7 的重新设计，不宣称为真实 M7 枪械录音。
- **Kenney — Impact Sounds**：[作者页面，CC0](https://kenney.nl/assets/impact-sounds)。项目现有金属撞击录音经过分层、变速、包络、滤波用于承压、接触、贯穿、机构、脚步及碎片。
- **Kenney — Sci-fi Sounds**：使用项目内现有且已有授权记录的喷口、引擎、能量素材。原始授权见同目录 `Kenney_sci-fi-sounds.txt`。结合重新制作的推进包络、周期发动机、气流及电磁层，不直接复用为多个状态的同一声响。

原始合成包括受控气流、短能量边缘、模态金属尾响、压力塌落和机构提示。AC6 只用于设计参考，新音库未采样 AC6 或其他游戏实机视频。制作工具不会打开系统播放设备。

`tools/p0/author_slice_audio_r9.py` 可重建银行；数值、SHA-256、原音库包络对照保存在 `AuditEvidence/combat-slice-r9/audio-bank.json`。制作依赖安装在 D 盘独立环境 `D:/Tools/AudioAuthoring`。
