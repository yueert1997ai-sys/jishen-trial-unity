from pathlib import Path
import hashlib, json, shutil, html

root = Path(__file__).resolve().parents[2]
ev = root / 'AuditEvidence/combat-punch-r1/release'
build = root / 'Builds/Combat_Punch_R1'
out = root.parent.parent / 'reviews/Combat_Punch_R1'
(out / 'images').mkdir(parents=True, exist_ok=True)
sha = hashlib.sha256((build / 'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest()
modes = 'punch punchperf vfx heroes drones nemesis performance loopv2 foundation tactics impact melee beam regression fullplay natural fullrun'.split()
tests = {}
for mode in modes:
    directory = sorted(ev.glob(mode + '-*'))[-1]
    run = json.loads((directory / 'run.json').read_text(encoding='utf-8-sig'))
    assert run['exit'] == 0 and run['assembly_sha256'] == sha
    assert 'P0_CHECK code=0 errors=0' in (directory / 'quick-check.txt').read_text(encoding='utf-8-sig')
    tests[mode] = directory.name
death = ev / tests['punch']
frames = sorted(death.glob('death-[0-9]*.png'))
assert len(frames) == 39
for image in [*frames, death/'death-before.png', death/'five-mech-explosions.png']:
    shutil.copy2(image, out/'images'/image.name)
natural = (ev/tests['natural']/'natural-results.txt').read_text(encoding='utf-8-sig')
perf = json.loads((ev/tests['punchperf']/'death-performance.json').read_text(encoding='utf-8-sig'))
report = dict(assembly_sha256=sha, tests=tests, death_performance=perf, natural=natural)
(ev/'test-summary.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf8')
(out/'verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf8')
page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>机神试炼 · 击毁与遭遇节奏 R1</title>
<style>body{margin:0;background:#10121a;color:#e5e6ed;font:16px/1.7 system-ui,sans-serif}main{max-width:1120px;margin:auto;padding:40px 24px}h1{font-size:32px}h2{margin-top:40px}p{color:#bbc0d2}img{width:100%;border-radius:12px;background:#20232d}.cards{display:flex;gap:14px;flex-wrap:wrap}.cards div{flex:1;min-width:210px;background:#202430;padding:20px;border-radius:12px}.cards b{display:block;font-size:28px;color:#d5bcff}button{background:#bea0f5;color:#151020;border:0;border-radius:8px;padding:10px 22px;cursor:pointer}input{width:65%;vertical-align:middle}pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:13px}small{color:#a4aac0}</style>
<main><small>COMBAT PUNCH R1 · 实际游戏画面 · 2026-09-22</small><h1>更短的遭遇，更明确的机甲击毁</h1>
<div class="cards"><div><b>69 → 34</b>默认短关敌人数；完整流程每关 27～32 台。</div><div><b>约 0.5 秒</b>击杀后，下一组能完整放入剩余名额时开始增援；同时名额上限 5。</div><div><b>失控 → 殉爆 → 飞散</b>胸部主次爆、真实装甲部件、火花与烟气。</div></div>
<h2>单台击毁 · 逐帧回放</h2><p>从最终交付 Player 采集，30 帧/秒。可暂停拖动，查看后仰、胸部爆炸及头肩装甲飞散。</p>
<img id="frame" src="images/death-006.png" alt="胸部爆炸与装甲飞散"><p><button id="play">播放</button> <input id="seek" type="range" min="0" max="38" value="6" aria-label="回放帧"> <span id="time">0.20 秒</span></p>
<h2>五台同时击毁</h2><img src="images/five-mech-explosions.png" alt="五台机甲同时击毁检查"><p>碎片复用 24 个槽，死亡粒子最多 672 粒；返库清空，暂停冻结。画面中保留机体与爆炸的位置关系。</p>
<details><summary>验证与性能说明 · 17 组通过</summary><p>短关三种自动操作策略均完成；完整六关及 Boss 流程通过。自动输入不代表玩家已经认可手感。</p><pre>__NATURAL__</pre><p>RTX 4070 Ti，1600×900，独立运行的离屏同步渲染：五台同时击毁阶段平均 3.52 ms，P95 4.14 ms，最大 25.36 ms。包含首次触发，不能当作屏幕帧率保证；存活与死亡阶段的几何数量不同，不能据此认定爆炸没有开销。</p><pre>__TESTS__</pre><small>最终程序集 SHA-256：__SHA__</small></details>
<p>从原桌面「机神试炼」入口启动新版。此前机体和版本仍保留。</p></main>
<script>let i=6,timer=null;const frame=document.getElementById('frame'),seek=document.getElementById('seek'),play=document.getElementById('play'),time=document.getElementById('time');function show(){frame.src='images/death-'+String(i).padStart(3,'0')+'.png';seek.value=i;time.textContent=(i/30).toFixed(2)+' 秒'}play.onclick=()=>{if(timer){clearInterval(timer);timer=null;play.textContent='播放'}else{timer=setInterval(()=>{i=(i+1)%39;show()},1000/30);play.textContent='暂停'}};seek.oninput=()=>{i=Number(seek.value);show()};</script></html>'''
page = page.replace('__NATURAL__', html.escape(natural)).replace('__TESTS__', html.escape('\n'.join(k+' · PASS · '+v for k,v in tests.items()))).replace('__SHA__',sha)
(out/'REVIEW.html').write_text(page, encoding='utf8')
print(out/'REVIEW.html')
print(sha)
