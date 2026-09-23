"""Collect only evidence from the final Player. Images are unmodified Player captures."""
from pathlib import Path
import hashlib, json, shutil, html

game=Path(__file__).resolve().parents[2]
evidence=game/'AuditEvidence/nemesis-m02/release'
art=game.parents[1]/'art_prototypes/J01_NEMESIS_20260920'
out=art/'animation/M02'
out.mkdir(parents=True,exist_ok=True)
modes=['heroes','drones','nemesis','performance','loopv2','foundation','tactics','impact','melee','beam','regression','fullplay','natural','fullrun']
assembly=hashlib.sha256((game/'Builds/Nemesis_M02/MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest()
checks={}
for mode in modes:
    directory=sorted(evidence.glob(mode+'-*'))[-1]
    report=(directory/'quick-check.txt').read_text(encoding='utf-8-sig')
    run=json.loads((directory/'run.json').read_text(encoding='utf-8-sig'))
    assert 'P0_CHECK code=0 errors=0' in report and run['exit']==0, mode+' did not pass'
    assert run['assembly_sha256']==assembly, mode+' is not from the final assembly'
    checks[mode]=directory

baseline=json.loads((game/'AuditEvidence/nemesis-m02/baseline/performance-20260922-003400/performance.json').read_text())
current=json.loads((checks['performance']/'performance.json').read_text())
labels=['待机','推进 · 关闭残影','推进 · 开启残影','推进 · 残影与支援']
rows=[]
for label,a,b in zip(labels,baseline['phases'],current['phases']):
    rows.append(f'<tr><td>{label}</td><td>{a["meanMs"]:.2f}</td><td>{b["meanMs"]:.2f}</td><td>{a["p95Ms"]:.2f} → {b["p95Ms"]:.2f}</td></tr>')
gallery=[
    ('heroes','hero-chooser-production.png','机库切换','Backspace 返回机库，点击“切换机体”。启动默认冥隼。'),
    ('heroes','valkyr-hangar-production.png','瓦尔基里','原机体、原双手持枪与原武器配置保留。'),
    ('heroes','nemesis-hangar-production.png','冥隼','近景完整装甲，步枪、光束剑与火箭炮保留。'),
    ('drones','drones-autofire-production.png','六枚浮游炮','E 离体伴飞、自动索敌与开火，结束后归位。实际试场截图。'),
    ('nemesis','dash-wings-rear.png','推进与残影','空格展开翼片，保留四份短暂暗紫残影。此图使用检查近景镜头。'),
    ('natural','natural-Mixed-32s.png','实际战斗镜头','最终构建的普通输入回放；画面未修饰。'),
]
figures=[]
for mode,name,title,description in gallery:
    destination=out/(mode+'-'+name);shutil.copy2(checks[mode]/name,destination)
    figures.append(f'<figure><img loading="lazy" src="{destination.name}" alt="{title}"><figcaption><strong>{title}</strong><p>{description}</p></figcaption></figure>')

hashes={}
for name,path in [('B20',art/'body_rebuild/delivery/B20/J01_NEMESIS_BODY_B20.blend'),('M02_blend',out/'J01_NEMESIS_M02.blend'),('profile',game/'Launcher/UserData/profile.json')]:
    hashes[name]=hashlib.sha256(path.read_bytes()).hexdigest()
assert hashes['B20']=='f96ae5690dbda520fd940f72be7734d063f8f0363d8f354ee5ff8b495e8c94ca'
assert hashes['profile']=='7db4bf90ba758a4cf034dc2081969bc9de413a41483f59c156d76be7cdbf195d'
verification=dict(assembly_sha256=assembly,files=hashes,tests={k:str(v) for k,v in checks.items()},performance_before=baseline,performance_after=current)
(out/'VERIFICATION.json').write_text(json.dumps(verification,ensure_ascii=False,indent=2),encoding='utf-8')
(evidence/'test-summary.json').write_text(json.dumps(verification,ensure_ascii=False,indent=2),encoding='utf-8')

page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>冥隼 M02 · 游戏内复核</title><style>
:root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#11131b;color:#e5e8f0;font:16px/1.65 "Microsoft YaHei",system-ui,sans-serif}
main{max-width:1200px;margin:auto;padding:38px 24px 80px}h1{font-size:38px;margin:0}h2{margin:48px 0 18px}p{margin:8px 0;color:#b8bfd1}.tag{color:#ad8ef1;letter-spacing:.16em;font-size:12px}.lead{font-size:19px;max-width:820px}
.gallery{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0;background:#1b1e2a;border:1px solid #2d3143;border-radius:8px;overflow:hidden}figure:first-child{grid-column:1/-1}img{display:block;width:100%;height:auto}figcaption{padding:18px 20px}figcaption strong{font-size:21px}figcaption p{font-size:14px}
.metric{display:flex;gap:30px;padding:22px 0}.metric strong{font-size:30px;display:block;color:#c7b5f1}.metric p{margin:0}table{border-collapse:collapse;width:100%;margin:20px 0}th,td{padding:12px 14px;text-align:left;border-bottom:1px solid #363849}th{color:#bbb0d3}a{color:#bca3f7}aside{padding:20px;background:#191c27;border-left:3px solid #a786df;margin-top:22px}
@media(max-width:700px){h1{font-size:28px}.gallery{grid-template-columns:1fr}.metric{display:block}.metric>div{margin-bottom:18px}table{font-size:12px}td,th{padding:8px}}</style><main>
<div class="tag">J-01 NEMESIS · M02</div><h1>机库切换与自动浮游炮</h1>
<p class="lead">默认冥隼，瓦尔基里可在机库切换。E 启动六枚浮游炮伴飞与自动射击，空格继续展开双翼并留下暗紫残影。</p>
<aside>沿用桌面“机神试炼”入口。短战斗中按 Backspace 返回机库，点击左上角“切换机体”。正在运行的旧版本需要关闭后重新启动。</aside>
<h2>实际游戏截图</h2><div class="gallery">'''+''.join(figures)+'''</div>
<h2>相同场景的帧耗时</h2><p>RTX 4070 Ti，1600 × 900。模拟与离屏渲染同步计时；前后分别运行。下表单位为毫秒，越低越好。</p>
<table><thead><tr><th>状态</th><th>M01 平均</th><th>M02 平均</th><th>P95：M01 → M02</th></tr></thead><tbody>'''+''.join(rows)+'''</tbody></table>
<p>这是固定试场对照，不等同于屏幕 FPS 或最坏敌群帧率。最后一行对比各版实际支援技能，旧版为导弹，新版为伴飞浮游炮；它不是相同技能的纯性能对比。</p>
<div class="metric"><div><strong>645,062</strong><p>近景机体三角面 · 保留细节</p></div><div><strong>29,512</strong><p>战斗远景三角面</p></div><div><strong>6,091</strong><p>每份残影三角面</p></div></div>
<h2>复核与文件</h2><p>14 项最终 Player 检查通过，包含机库往返切换、近身与远距自动射击、掩体、暂停、死亡、归位、动作与完整流程。B20 源文件及永久收藏未改动。数值检查通过不代表用户已经认可手感和视觉效果。</p>
<p><a href="J01_NEMESIS_M02.blend">可编辑 Blender 准备源</a> · <a href="VERIFICATION.json">验证记录</a></p>
<p>动作在 Unity 中实时执行；此 Blender 源保留分件和枢轴，未附带烘焙动画片段。</p></main></html>'''
(out/'REVIEW.html').write_text(page,encoding='utf-8')

doc=game/'docs/NEMESIS_M02.md'
text=doc.read_text(encoding='utf-8').split('\n## 最终验证记录')[0]
text+='\n## 最终验证记录\n\n'+f'最终 Assembly-CSharp SHA-256：`{assembly}`。14 项实际 Player 检查均通过，准确路径见 `AuditEvidence/nemesis-m02/release/test-summary.json`。\n\n'
text+='| 状态 | M01 平均 ms | M02 平均 ms | M01 / M02 P95 ms |\n| --- | ---: | ---: | ---: |\n'
for label,a,b in zip(labels,baseline['phases'],current['phases']):
    text+=f'| {label} | {a["meanMs"]:.2f} | {b["meanMs"]:.2f} | {a["p95Ms"]:.2f} / {b["p95Ms"]:.2f} |\n'
text+='\n支援行的技能已变化：旧版为导弹，新版为浮游炮。主要同场景对照使用推进加残影一行。每阶段最大值与全部原始记录保留在 JSON，未把 GC 计数为零当作零分配证明。\n\n'
for mode in ('natural','fullrun'):
    text+=f'{mode}：\n\n```text\n'+(checks[mode]/'natural-results.txt').read_text(encoding='utf-8-sig')+'\n```\n\n'
text+='实际最终 Player 图集位于美术工程 `animation/M02/REVIEW.html`。发布入口与静音启动校验另以 Launcher 记录为准。\n'
doc.write_text(text,encoding='utf-8')
print(json.dumps({'review':str(out/'REVIEW.html'),'tests':len(checks),'assembly':assembly},ensure_ascii=False))
