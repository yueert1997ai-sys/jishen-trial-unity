from pathlib import Path
import hashlib, html, json, shutil, statistics

root=Path(__file__).resolve().parents[2]
evidence=root/'AuditEvidence/drone-vfx-r1'
release=evidence/'release'
build=root/'Builds/Nemesis_DroneVfx_R1'
out=root.parents[1]/'art_prototypes/J01_NEMESIS_20260920/animation/DroneVfx_R1'
out.mkdir(parents=True,exist_ok=True)
images=out/'images';images.mkdir(exist_ok=True)
modes='vfx heroes drones nemesis performance loopv2 foundation tactics impact melee beam regression fullplay natural fullrun'.split()
assembly=hashlib.sha256((build/'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest()
tests={}
for mode in modes:
    path=sorted(p for p in release.glob(mode+'-*') if p.is_dir())[-1]
    run=json.loads((path/'run.json').read_text())
    report=(path/'quick-check.txt').read_text(encoding='utf-8-sig')
    assert run['exit']==0 and run['assembly_sha256']==assembly and 'P0_CHECK code=0 errors=0' in report, mode
    tests[mode]={'path':path.relative_to(root).as_posix(),'checks':report.count('PASS '),'assembly_sha256':assembly}
(release/'test-summary.json').write_text(json.dumps(tests,indent=2),encoding='utf8')
vfx=root/tests['vfx']['path'];drone=root/tests['drones']['path']
before=sorted((evidence/'baseline').glob('drones-*'))[-1]
shutil.copy2(before/'drones-autofire-production.png',images/'before.png')
shutil.copy2(drone/'drones-autofire-production.png',images/'after.png')
for p in vfx.glob('*.png'):shutil.copy2(p,images/p.name)
sequence=[p.name for p in sorted(images.glob('sequence-*.png'))]
boost=[p.name for p in sorted(images.glob('boost-*.png'))]
perf=json.loads((root/tests['performance']['path']/'performance.json').read_text())
paired=json.loads((evidence/'paired-performance.json').read_text()) if (evidence/'paired-performance.json').exists() else None
table=''
if paired:
    for phase in ['idle','boost-no-ghost','boost-ghost','boost-support']:
        labels={'idle':'待机','boost-no-ghost':'推进 · 关闭残影','boost-ghost':'推进 · 开启残影','boost-support':'推进 + 残影 + 浮游炮'}
        data=[]
        for version in ['baseline','release']:
            samples=[next(s for s in pair[version]['phases'] if s['phase']==phase) for pair in paired['pairs']]
            data.append((statistics.median(s['meanMs'] for s in samples),statistics.median(s['p95Ms'] for s in samples)))
        table+=f'<tr><td>{labels[phase]}</td><td>{data[0][0]:.2f} / {data[0][1]:.2f}</td><td>{data[1][0]:.2f} / {data[1][1]:.2f}</td></tr>'
summary={'version':build.name,'assembly_sha256':assembly,'tests':tests,'performance':perf,'paired_performance':paired}
(out/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf8')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>冥隼 · 浮游炮特效 R1</title><style>
:root{color-scheme:dark;font:16px/1.6 "Microsoft YaHei",sans-serif;color:#e5e1ec;background:#101018}*{box-sizing:border-box}body{margin:0}main{max-width:1340px;padding:42px 28px 72px;margin:auto}header{border-bottom:1px solid #373041;padding-bottom:24px;margin-bottom:26px}.eyebrow{letter-spacing:.2em;color:#c4a5ef;font-size:12px}h1{font-size:36px;line-height:1.25;margin:10px 0 14px;font-weight:600}p{color:#b9b4c2;margin:10px 0}button{font:inherit;cursor:pointer;border:1px solid #50435f;background:#24202e;color:#e6deee;border-radius:5px;padding:8px 18px}button.active{background:#604191;border-color:#b080eb;color:white}button:hover{border-color:#c29ae9}button:focus-visible,a:focus-visible,input:focus-visible{outline:2px solid #d8bcfc;outline-offset:3px}nav,.controls{display:flex;flex-wrap:wrap;gap:9px;align-items:center;margin:14px 0}figure{margin:0}img{display:block;width:100%;height:auto;background:#19191d;aspect-ratio:16/9;object-fit:contain;border:1px solid #302b39}figcaption{font-size:13px;color:#a9a2b1;padding:9px 0}.controls input{flex:1;min-width:100px;accent-color:#c596ff}.controls span{min-width:100px;color:#b5acbf;font-variant-numeric:tabular-nums}section{margin:32px 0}h2{font-size:22px;font-weight:600;margin:0 0 12px}a{color:#c7a0ee}details{border-top:1px solid #403746;padding-top:18px;margin-top:36px}summary{cursor:pointer}table{border-collapse:collapse;width:100%;font-size:14px;margin:18px 0}td,th{text-align:left;border-bottom:1px solid #35303f;padding:10px}.facts{display:grid;grid-template-columns:repeat(3,1fr);gap:18px;margin-top:24px}.fact{border-top:2px solid #755196;padding-top:14px}.fact strong{font-size:18px}.fact p{font-size:14px}.small{font-size:13px}.tag{font-size:12px;border:1px solid #554363;padding:3px 8px;color:#c7b4de;display:inline-block;margin-bottom:8px}@media(max-width:650px){main{padding:22px 14px}.facts{grid-template-columns:1fr}h1{font-size:29px}button{padding:7px 11px}}
</style><main><header><div class="eyebrow">J-01 NEMESIS / DRONE VFX R1</div><h1>暗紫交叉火力</h1><p>炮口收束 → 分层光束与流动细丝 → 命中喷溅 → 消散余辉。</p><span class="tag">来自最终游戏程序的实际画面 · 静音</span></header>
<section><nav aria-label="画面选择"><button class="active" id="volley">伴飞射击</button><button id="boost">空格推进中射击</button></nav><figure><img id="motion" src="images/drone-volley.png" alt="冥隼六枚浮游炮的暗紫光束射击"><figcaption>实机连续帧。20 帧/秒回放，可暂停并拖动查看蓄能、光束与余辉。</figcaption></figure><div class="controls"><button id="play">播放</button><input id="scrub" type="range" min="0" value="0" aria-label="回放进度"><span id="counter"></span></div></section>
<div class="facts"><div class="fact"><strong>先聚能，再放射</strong><p>炮口出现收束环与吸入粒子，开火时形成紫色光晕和短促闪光。</p></div><div class="fact"><strong>光束有厚度和流动</strong><p>亮芯、暗紫外层、柔和光晕，配合缠绕细丝和渐散余辉。</p></div><div class="fact"><strong>命中有落点</strong><p>接触点产生星芒、定向火花、扩散环和少量消散烟气。</p></div></div>
<section><h2>修改前后</h2><nav><button id="after" class="active">新版 R1</button><button id="before">此前 M02</button></nav><figure><img id="comparison" src="images/after.png" alt="浮游炮效果修改前后"><figcaption>同一相机与试射场，各取可见开火帧；开火时序不同，未做像素叠图。</figcaption></figure></section>
<details><summary>验证与性能记录</summary><p>最终版本的 15 组实际游戏检查全部通过。覆盖 30 / 120 帧射击次数与伤害、掩体与换目标、炮口连接、暂停、死亡、回收、换机、复用以及完整战斗流程。</p><p>三轮成对串行测试，中位数：平均 / P95（毫秒）。测试时没有并行构建或游戏检查。</p><table><thead><tr><th>状态</th><th>M02</th><th>新版 R1</th></tr></thead><tbody>PERFORMANCE_ROWS</tbody></table><p class="small">RTX 4070 Ti，1600×900，固定 60 Hz 指令，离屏渲染并逐帧同步 GPU。这是受控场景的帧耗时，不是屏幕 FPS 或最坏敌群的性能保证。早期样本有整体波动，因此保留它们并补做三轮前后配对测试。</p><p>六枚浮游炮复用固定特效，五套共享粒子系统最多 456 粒。没有新增逐炮动态灯光或机体副本。基础部署仍为 36 次射击、72 点总伤害。</p><p class="small">自动检查和这些画面不代替你对实际效果与手感的判断。<a href="verification.json">完整记录</a></p></details>
<footer><p>桌面原入口「机神试炼」· 默认冥隼 · E 部署浮游炮 · 空格展开推进</p></footer></main>
<script>
const sequences={volley:VOLLEY_DATA,boost:BOOST_DATA};let mode='volley',frame=0,playing=false;const motion=document.getElementById('motion'),scrub=document.getElementById('scrub'),play=document.getElementById('play');
function show(){const list=sequences[mode];scrub.max=list.length-1;scrub.value=frame;motion.src='images/'+list[frame];document.getElementById('counter').textContent=(frame+1)+' / '+list.length+' 帧'}
function select(next){mode=next;frame=0;for(const key of ['volley','boost'])document.getElementById(key).classList.toggle('active',key===mode);show();sequences[mode].forEach(src=>{const img=new Image();img.src='images/'+src})}
document.getElementById('volley').onclick=()=>select('volley');document.getElementById('boost').onclick=()=>select('boost');play.onclick=()=>{playing=!playing;play.textContent=playing?'暂停':'播放'};scrub.oninput=()=>{playing=false;play.textContent='播放';frame=Number(scrub.value);show()};
setInterval(()=>{if(playing&&!document.hidden){frame=(frame+1)%sequences[mode].length;show()}},50);
for(const id of ['before','after'])document.getElementById(id).onclick=()=>{document.getElementById('comparison').src='images/'+id+'.png';document.getElementById('before').classList.toggle('active',id==='before');document.getElementById('after').classList.toggle('active',id==='after')};select('volley');
</script></html>'''
page=page.replace('PERFORMANCE_ROWS',table).replace('VOLLEY_DATA',json.dumps(sequence)).replace('BOOST_DATA',json.dumps(boost))
(out/'REVIEW.html').write_text(page,encoding='utf8')
print(out/'REVIEW.html')
print('Verified',len(tests),'suites,',sum(v['checks'] for v in tests.values()),'checks; assembly',assembly)
