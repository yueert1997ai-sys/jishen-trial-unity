# NEMESIS purple eyes R2

2026-09-23 最新本地发布：**NemesisEye_R2**。冥隼眼睛恢复饱和紫色，降低发光强度以避免泛蓝白；保留原头型、眼片贴合和三档 LOD 可见性修复。仅修改眼睛材质，网格几何不变。22 组最终 Player 套件、2156 项检查及静默启动验证通过，永久存档未变。桌面仍使用「机神试炼」，新源文件和构建在 E 盘。

The prior eye repair used emission strength 3.2 and appeared blue-white. R07 uses emission sRGB #9250FF, strength 1, and base #28104F. Both Blender and the Unity source manifest carry the same values. All EHM geometry is retained. The Blender mesh signature remains 70fcb61a67e932544ee616355d9ea0b4709245ac47f68e5ba2583b1d45a186f3.

Editable source: E:/SteamLibrary/GameStudyExports/MechaRefinement_R04/outputs/R07_PURPLE_EYES/NEMESIS_PURPLE_EYES_R07.blend
Source SHA256: d0a86ccc942a6edb18f0a2c24b3083db88e35daa701058667549675e5d33f8c9
R06 and earlier Blender files are retained.

Actual Player eye checks compare emission-on/off pixels and verify saturated violet RGB, rather than checking brightness alone. Both eyes have 54 visible emission pixels on each of the three LODs. Average RGB is approximately (0.522, 0.316, 0.896) and (0.510, 0.310, 0.876). These are technical checks; final visual acceptance remains with the user.

Builds/NemesisEye_R2 and AuditEvidence/nemesis-eye-r2 are junctions into E:/SteamLibrary/JishenBuildWork/NemesisEye_R2. Canonical Art still uses NemesisEye_R1/Art through Assets/Art/ExtractedHeroesR2. Old built Players remain intact. Permanent profile SHA256: 7DB4BF90BA758A4CF034DC2081969BC9DE413A41483F59C156D76BE7CDBF195D.

Final release evidence: AuditEvidence/nemesis-eye-r2/release/final-verification.json. The stable desktop shortcut continues through Launcher/launch.ps1; current.json selects NemesisEye_R2. Front and quarter-view runtime images were sent to the user's Feishu personal chat and are also in the chat outputs/Nemesis_Purple_Eyes_R2 folder. No commit or push was made.
