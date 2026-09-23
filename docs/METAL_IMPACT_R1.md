# Metal impact R1, 2026-09-19

用户反馈：敌人受击像"塑料模型"，缺少金属机体打斗的质感。诊断：护甲兵（E01）已有火花+碎屑+划痕，但**无甲敌人**只有 7 颗无贴图小火花加单层低音量音效，且枪击的物理击退为零（`meleePush=Vector3.zero`），机体吃弹毫无反应。本轮补齐"金属感"的三个支柱：分层金属音效、受击点火花碎屑、机体后坐。

## 音效（新 cue：MetalHitLight / MetalHitHeavy）

- `tools/p0/author_metal_impact.py` 纯合成分层 PCM（48kHz 单声道）进 `Assets/Resources/Audio/Combat/ImpactR2/`：轻×3（~0.15s：2-7kHz 瞬态爆点 + 非谐金属余振双分音 + 132Hz 轻钝响）、重×2（~0.26s：更深 88Hz 钝响 + 更长余振 + 更亮瞬态）。清单与 SHA-256 在 `AuditEvidence/metal-impact-r1/audio-manifest.json`。
- GameAudio：cue 插在 PlayerHit 与 SalvagePull 之间（走冲击声部 6-9，不占优先声部），间距轻 .05s / 重 .075s；实验室 MinimalFeedback 抑制列表已收录。
- 接线：无甲敌人枪击命中 → MetalHitLight/Heavy（.42 音量，随机音高 .93-1.08），替代原先单层 Hit .27；护甲重击 → ArmorClash（R2 既有资产首次投入使用），护甲轻击保持 Hit/HullHit（音量 .36→.40）。

## 视觉（EnemyVfx.MetalHit）

- 新粒子系统：`hotSparks`（加法混合、速度拉伸渲染、重力 0.95——白热→橙红的钢铁火花流）与 `shrapnel`（alpha 混合、重力 1.25、持续翻滚的暗色金属碎屑，Kenney dirt_01 精灵）。
- 命中点按来弹方向反向锥形喷溅：无甲 8-13 颗火花 + 2-4 块碎屑 + 白热闪光（重击附加小冲击环）；护甲面 10-15 颗偏冷白火花。
- 死亡爆炸追加了 6 块带初速翻滚飞出的机体碎块。

## 物理与镜头

- `EnemyBase.ResolveHitReaction` 远程分支：轻击 .07s 硬直 + 远离射手 4.2 m/s 三角推离（约 15cm 可见后坐），重击 .18s + 2.4（约 22cm）。防机关枪无限硬直的 1.15s 门槛与"步枪不吞刀击硬直"契约不变；近战推离与护甲兵免硬直规则不变。
- 微震：真正把机体打晃的那一发（轻击且硬直>0）给 .05/.05 镜头微震，频率受硬直门槛约束，不与既有重击震动叠加；`CameraFollow.AddShake` 内置的 NoCameraShake/用户设置开关自动生效。

## 验证与发布

`Builds/EnemyAI_Vfx_R1` 重新构建（Succeeded errors=0 warnings=0），五个隐藏 Player 套件（foundation / tactics / impact / regression / fullplay）在最终构建上全部通过，`publish_playable.py --require` 五项校验后原子更新 `Launcher/current.json`（147 文件清单）。impact 套件的近战推离距离断言（.22-.30/.80-1.12）不涉及远程路径，未受影响。

## 限制

音效为信号合成，非真实金属录音；"像不像打铁"仍需真耳确认。火花/碎屑数量与后坐强度为首版拍板值，反馈后可调（数值都在 `EnemyVfx.MetalHit` 与 `ResolveHitReaction` 内联，未进 CombatRules）。
