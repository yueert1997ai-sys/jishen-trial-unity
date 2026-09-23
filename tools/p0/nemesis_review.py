"""Package untouched final Player captures next to the editable art source."""
from pathlib import Path
import hashlib, html, json, shutil

root = Path(__file__).resolve().parents[2]
art = root.parents[1] / 'art_prototypes/J01_NEMESIS_20260920'
out = art / 'animation/M01'
evidence = root / 'AuditEvidence/nemesis-m01/release'
release = json.loads((root / 'Launcher/current.json').read_text(encoding='utf-8-sig'))
assert release['version'] == 'Nemesis_M01', 'Only show a promoted, verified build'
run = root / release['tests']['nemesis']
pictures = out / 'review'
pictures.mkdir(exist_ok=True)
shots = [
    ('05-rifle-open-shoulder.png', '单手舒展持枪', '腰胸侧转，枪臂平伸，肘部轻弯。近景检查镜头。'),
    ('05-rifle-front.png', '枪口正面', '核对手掌握持、枪臂与胸肩的关系。近景检查镜头。'),
    ('blade-1-08.png', '第一斩 · 斜斩', '动作链中的实际帧。'),
    ('blade-2-08.png', '第二斩 · 回斩', '反向接续，不回到站姿后重来。'),
    ('blade-3-20.png', '第三斩 · 重斩', '加大肩胸转动与手臂摆幅。'),
    ('dash-10.png', '冲刺残影', '实际世界位置的暗紫机体残影。'),
    ('dash-wings-rear.png', '持续推进展翼', '双翼展开，六枚翼片扇形张开。'),
    ('06-launcher-aim.png', '火箭炮', '双手握持，支撑手落在炮身下方。'),
    ('07-support-deploy.png', '六枚浮游炮', '支援展开后回到各自停泊位置。'),
    ('03-hands-feet.png', '掌面与脚尖', '空手掌心朝内、脚尖向外。'),
    ('11-hangar-production.png', '返回机库', '实际机库界面与镜头。'),
]
cards = []
for filename, title, caption in shots:
    shutil.copy2(run / filename, pictures / filename)
    cards.append(f'<figure><a href="review/{filename}"><img loading="lazy" src="review/{filename}" alt="{title}"></a><figcaption><b>{title}</b><span>{caption}</span></figcaption></figure>')
natural = root / release['tests']['natural']
combat = natural / 'natural-Mixed-12s.png'
if combat.exists():
    shutil.copy2(combat, pictures / combat.name)
    cards.insert(2, f'<figure><a href="review/{combat.name}"><img src="review/{combat.name}" alt="实际战斗镜头"></a><figcaption><b>实际战斗镜头</b><span>基础装备、零强化的正常关卡截图。</span></figcaption></figure>')
summary = dict(release=release, motionMetrics=json.loads((run/'motion-metrics.json').read_text()),
               sourceBlend='J01_NEMESIS_M01.blend',
               sourceSha256=hashlib.sha256((out/'J01_NEMESIS_M01.blend').read_bytes()).hexdigest(),
               natural=(natural/'natural-results.txt').read_text(),
               fullrun=(root/release['tests']['fullrun']/'natural-results.txt').read_text())
(out/'VERIFICATION.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>J-01 NEMESIS · 动作接入 M01</title><style>
*{box-sizing:border-box}body{margin:0;background:#14151b;color:#eee;font:16px/1.65 "Microsoft YaHei",sans-serif}main{max-width:1440px;margin:auto;padding:36px 28px}header{border-bottom:1px solid #46424f;padding-bottom:24px;margin-bottom:24px}small{letter-spacing:.18em;color:#bb9ce6}h1{font-size:34px;margin:8px 0}p{max-width:860px;color:#bbb9c6}a{color:#d5b7ff}nav{display:flex;gap:20px;flex-wrap:wrap}section{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0;background:#202129;border:1px solid #33323d;border-radius:8px;overflow:hidden}img{display:block;width:100%;height:auto}figcaption{padding:16px}figcaption span{display:block;color:#bab6c6;font-size:14px}table{border-collapse:collapse;min-width:460px;margin:20px 0}td{border-bottom:1px solid #383541;padding:8px 20px 8px 0}details{margin-top:28px;color:#aaa6b7}footer{padding:28px 0;color:#aaa6b7}@media(max-width:850px){section{grid-template-columns:1fr}main{padding:24px 16px}h1{font-size:26px}table{min-width:0}}
</style><main><header><small>J-01 NEMESIS / M01</small><h1>冥隼 · 主角动作接入</h1><p>单手展臂、腰胸侧转的持枪姿态；幅度更大的三连斩；空格冲刺展翼与暗紫残影。下方图片来自本次游戏构建的实际运行截图，点击可查看原图。</p><nav><a href="#controls">操作</a><a href="J01_NEMESIS_M01.blend">可编辑 Blender 工程</a><a href="VERIFICATION.json">验证记录</a></nav></header>
<section>''' + '\n'.join(cards) + '''</section><h2 id="controls">从桌面“机神试炼”进入</h2><table><tr><td>WASD</td><td>移动</td></tr><tr><td>鼠标左键</td><td>射击</td></tr><tr><td>右键 / Q</td><td>三连斩</td></tr><tr><td>空格 / 按住空格</td><td>冲刺 / 持续推进，展开双翼并留下暗紫残影</td></tr><tr><td>E</td><td>六枚浮游炮支援</td></tr><tr><td>Backspace</td><td>返回机库，在配装中切换步枪或火箭炮</td></tr><tr><td>R / Esc</td><td>重开 / 暂停</td></tr></table><details><summary>交付与检查范围</summary><p>可编辑工程保留分件、关节枢轴和握持点。当前正式动作由游戏中的姿势驱动器实现，该 Blender 工程不包含独立烘焙的 FBX 动画片段。B20 外观源保留。自动检查覆盖握持、脚底接触、30/60/120 FPS 三连斩、真实命中、暂停、死亡、返库、短战斗和完整关卡流程；通过这些检查不等于动作美感已获得用户认可。</p></details><footer>''' + html.escape(release['publishedAt']) + ''' · Nemesis_M01 · 图片未做美化或修图。</footer></main></html>'''
(out/'REVIEW.html').write_text(page, encoding='utf-8')
print(out/'REVIEW.html')
