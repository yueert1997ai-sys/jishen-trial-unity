# HEAD_V2 — 头部一级造型验收

本轮依据 `USER_HEAD_BRIEF.txt` 从零重建头部。停在 PRIMARY FORM 灰模审查；没有进入二级结构细化、三级细节，也没有替换游戏角色。当前建议审查的是 ITER05，尚未取得用户造型通过。

## 先打开这些文件

- `HEAD_V2.blend`：可编辑验收文件，源自 ITER05。22 个主要网格，单一灰色材质，独立面罩、双目、外脸甲、天线及颈部部件；头部根节点和天线挂点设有可用旋转中心。
- `HEAD_FRONT.png`、`HEAD_SIDE.png`、`HEAD_3Q.png`：重新打开验收文件后，由本机 Blender 实际渲染的 1440 × 1440 PNG。
- `HEAD_V2_LANDMARK_COMPARISON.jpg`：参考与实模按 TOP / CHIN 等比对齐，标注检查基准；没有拉宽或压扁图片。
- `iterations/HEAD_V2_ITER05/REFERENCE_COMPARISON.jpg`：参考的正面、侧面、HEAD DETAIL 与本轮实模并排对照。侧面仅在此对照页水平翻转以统一朝向，原始渲染未改动。
- `iterations/HEAD_V2_ITER05/THREE_VIEWS.jpg`：三张实际渲染组成的概览。
- `HEAD_REJECTED_V1.blend`：原先被否决的完整场景原样备份，包含旧头部；原始文件未覆盖。SHA256：`524303D668D8B317D86F2B58A5C1C50AC794930450689623C053606577A60E9C`。
- `references/HEAD_REJECTED_V1_CLAY_3Q.png`：额外重渲染的旧头部灰模，用于检查旧版圆顶和悬浮分件问题。

## 实际迭代经过

每轮均先保存对应 `.blend`，用另一个 Blender 进程重新打开，渲染 FRONT / SIDE / 3Q，再实际打开图片对照参考。每轮目录中保存模型、三张渲染、参数、执行脚本快照、运行报告和 `VISUAL_REVIEW.md`。

| 轮次 | 根据实图进行的工作 | 实际检查后发现的下一步问题 |
| --- | --- | --- |
| ITER01 | 从零建立硬表面头壳、窄面罩、短双目、两根天线和颈接口；去掉旧版圆顶及全部小细节 | 头顶连接缺口、眼镜式外圈、下半脸过长 |
| ITER02 | 补顶甲连接、整理额头和眉甲平面、下半脸缩短 26%、面罩收窄 10%、眼框内移 | 单纯内移小眼框仍像眼镜；下颌内架仍偏方 |
| ITER03 | 重建带凹槽的整块眼窝承载面、收束内侧脸架、补内衬、改为连续八边形颈甲圈 | 内衬穿出额头；眼窝承载面仍越出面罩、脸甲层级 |
| ITER04 | 按额甲实际平面限制内衬位置；让眼窝下部和外侧向后收；补下颌支承 | TOP / CHIN 对齐后确认整体头宽和面罩长度仍偏大 |
| ITER05 | 头宽缩小 14%；面罩高度围绕上连接线缩短 22%；天线根部向内收 0.060 个设计单位；抬高下颌承托上缘 | 停在当前一级造型审查，后续审美取舍待用户确认 |

ITER05 最后还核对了颈关节：缩宽时俯仰轴的 Y/Z 同比缩放，保持真正圆截面；偏航环的 X/Y 同比缩放。该检查前的 ITER05 预览保存在 `iterations/HEAD_V2_ITER05/pre_neck_radius_check/`。

## 与参考的取舍与未完成内容

- 正面用于头顶、眼线、脸宽和下颌的主要比例；侧面补足前后体积。概念视图并非严格一致的工程图，未使用其中的高度、重量、武器长度和宣传文字。
- 按新要求只保留两根短天线；参考中的额外侧翼、第三传感器及密集小件本轮没有制作。
- 眼部保留眉甲遮挡、真实凹槽和独立内侧光学件。当前光学件也是同一灰材质，没有蓝色、发光或贴图帮助塑形。
- 头顶、侧后甲采用更明确的平面和倒角。参考的复杂叠甲、侧后机械分层、中央传感器舱尚未进入细化；这些不视为已完成的高精度还原。
- 颈部为可编辑接口和分离机械件，未做完整绑定，也未验证完整转头运动范围。没有制作身体、动画、游戏导出或任何角色替换。
- 当前是提交造型验收的模型，不是用户已通过的美术定稿；不得自动开始添加螺丝、刻线或纹理。

## 环境与验证

实际调用 `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`，版本 **Blender 5.2.1 LTS**，Cycles / RTX 4070 Ti / OPTIX。项目说明读取了 `tools/hero/README.md`、`docs/HERO_MODEL_HANDOFF.md`、`Assets/UserContent/PlayerMech/README_CustomMechImport.md`。采用现有资产 3.25 m 头顶基准，Z 向上、-Y 向前、-X 为机体右侧；没有照搬概念图标注尺寸。

四个固定正交相机：`HEAD_FRONT`、`HEAD_SIDE`、`HEAD_BACK`、`HEAD_3Q`。本轮只渲染所要求的正面、侧面、3/4。参考图已打包在 Blender 的 REFERENCE 集合和图像数据中。

`geometry_inspection.json` 记录保存文件的网格与射线检查：22 个原始网格及其倒角后网格，非流形边为 0；额头抽样首先命中正确外甲，双眼内外抽样首先命中独立光学件。该检查不代替图片审美验收，也不是全运动范围无碰撞保证。

`logs/review_copy_pivot_check.json` 验证设置编辑中心后，所有世界空间顶点保持不变（浮点误差范围内）。保存文件重新打开并渲染检查。

渲染日志中的 OptiX 缓存路径提示使用本资产目录的 `optix7cache.db`；它不是建模失败。没有因此改动系统设置。

## 实际执行过的建模脚本与运行方式

在 PowerShell 中进入本目录后运行以下命令。输出仅位于本目录；命令会重建对应生成文件。

```powershell
Set-Location -LiteralPath 'C:\Users\yue\Documents\MECH ROUGE\art_prototypes\JishenTrial_HEAD_V2'
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --threads 4 --python-exit-code 1 --python 'build_head.py' -- --config 'iterations/ITER05.json'
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --threads 4 'iterations\HEAD_V2_ITER05\HEAD_V2_ITER05.blend' --python-exit-code 1 --python 'prepare_review_file.py'
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --threads 4 'HEAD_V2.blend' --python-exit-code 1 --python 'render_review.py'
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --threads 4 'HEAD_V2.blend' --python-exit-code 1 --python 'inspect_geometry.py'
```

`build_head.py` 为实际执行的建模脚本；其控制点、面、厚度和倒角均可编辑。`prepare_review_file.py` 设置审查文件和编辑中心。`render_review.py` 从重新打开的模型生成实图。`inspect_geometry.py` 做几何抽样检查。`prepare_reference.py`、`make_comparisons.py` 和 `make_landmark_comparison.py` 仅裁剪/排版既有参考和实际渲染；没有用 AI 生图代替三维模型。

## Git 与工作范围

本轮全部写入 `art_prototypes/JishenTrial_HEAD_V2/`，未修改游戏代码、场景、现有主角或旧模型。该目录属于未跟踪的 `art_prototypes/`，未 commit、merge 或 push。

开始时分支为 `codex/rigged-hero-20260905`，HEAD 为 `bb7018e7b773600f3bacc99d7dc1268e080e38d8`，已跟踪文件无差异。工作过程中同一工作区出现了本轮以外的场景、玩法代码及 IndustrialModules 等变更，随后分支变为 `codex/industrial-dash-polish`，HEAD 为 `5159dd646248c450cdc870a479c02961ca4bbad4`；本轮没有执行这些修改、分支切换或提交。交付检查时已跟踪文件无差异，但保留 `art_prototypes/` 和原有其他未跟踪文件。实际 Git 状态保存在 `logs/git_status_delivery.txt`，不能把整个工作区称为干净。
