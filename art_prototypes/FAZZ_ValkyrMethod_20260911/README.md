# FAZZ 整机：沿用 VALKYR V3 构造经验

主文件：FAZZ_V3_METHOD.blend。交换文件：exports/FAZZ_COMPLETE.glb。

最新用户决定：头部使用之前的 FAZZ_HEAD 成品。本版已追加该头部的完整 63 个原网格，只做整体 0.5 等比缩放及颈部安装定位。原网格哈希集合与来源完全一致，见 work/head_identity.json；未改写原头部文件。

身体与装备：独立重建胸部附加装甲、可见导弹舱、倾斜下进气口、肩部立柱及平展翼片、前臂装甲、双管步枪、腰部与长裙甲、白色重装小腿、背包四喷口和两支长燃料罐；重炮包含长炮身、肩部承载支架、能源接口及线缆。炮体依据 MG FAZZ 外形重新构造，未将另一个独立炮设计冒充原版武器。

方法来源为真实 VALKYR V3 工程 body_common.py 中的任意倾斜面板、包覆壳体、带内壁凹腔、活塞与关节构造。没有使用或修改 FAZZ skill。工作脚本 v3_geometry.py 保留该几何构造方法；FAZZ 的外形参数独立建立，不是瓦尔基里换色。

参考：references/user_primary.png 是此前 MG FAZZ Ver.Ka 主参考，orthographic.png 是同版本说明书正侧视；来源文件从既有参考库复制。身体配色、隐藏结构和细小安装件仍有推断。

状态：这是可继续迭代的完整装配研究版，未宣称准确复刻原厂模型。胸肩衔接、小腿外罩截面、重炮外壳与背包后部仍有简化，下一轮应重点对照这几处；不能以面数和零几何报错代替外形验收。尚未制作蒙皮、动画、LOD 或接入 Unity。

验证：最终主体 781 个网格（含 13 个文字标记对象）；约 21.4 万三角形。几何审查无无效坐标或退化三角形，装甲实体无边界/非流形问题；文字标记作为薄表面单列。GLB 回读 779 个网格，三角形总数一致，保留整机根；导出器合并/省略了部分对象节点，见 work/glb_readback.json。

原头部来源：C:/Users/yue/Documents/Codex/2026-09-08/new-chat-2/outputs/FAZZ_HEAD/FAZZ_HEAD.blend。
当前 Blender 保存文件复开后渲染的 final_hero.png、final_head.png、final_rear.png 为最终检查图。

成本：本轮未调用 Hyper3D 或其他收费生成服务；使用本地 Blender 构造、渲染和验证。未统计 Codex 订阅额度与电费。
