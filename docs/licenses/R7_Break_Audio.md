# R7 破甲与斩杀音效

`Assets/Resources/Audio/Combat/R7/armor_break.wav` 和 `armor_finish.wav` 为本项目生成的分层效果：复用已有 `RaikenV7/armor_break_heavy.wav` 金属录音，叠加原创瞬态噪声、频率滑变、分时碎裂音和低频声体。原录音的既有许可继续适用。没有从 Apex 或其他商业游戏提取音频。

可重复生成工具：`tools/p0/make_break_audio.py`。固定随机种子，48 kHz、16 bit、单声道。源文件与输出峰值、长度、削波样本统计保存在 `AuditEvidence/combat-slice-r7/audio-assets.json`。
