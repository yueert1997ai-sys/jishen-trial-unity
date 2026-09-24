"""Convert selected local PSA tracks using the actual reference-model hierarchy.

PSA BONENAMES exported by this UModel build has flattened parents. Never use
those parents for FK. This importer uses basepose_0000.gltf and validates bind FK.
Only the body/weapon joints needed by our runtime are retained.
"""
from pathlib import Path
import hashlib, json, mmap, struct
import numpy as np
from scipy.spatial.transform import Rotation as R

ROOT=Path(__file__).resolve().parents[2]
SOURCE=Path(r'E:\SteamLibrary\GameStudyExports\GB4\Converted')
OUT=ROOT/'Assets/Resources/GB4Motion'; OUT.mkdir(parents=True,exist_ok=True)
REPORT=ROOT/'AuditEvidence/gb4-motion-r1'; REPORT.mkdir(parents=True,exist_ok=True)
NAMES=['root','jointroot_000','J_hip_400','J_chest_001','J_head_002',
 'J_R_shoulder_200','J_R_upArm_201','J_R_elbow_202','J_R_foreArm_203','J_R_wrist_204','J_R_weapon_205',
 'J_L_shoulder_300','J_L_upArm_301','J_L_elbow_302','J_L_foreArm_303','J_L_wrist_304','J_L_weapon_305',
 'J_R_upLeg_420','J_R_knee_421','J_R_leg_422','J_R_foot_423','J_R_toe_424',
 'J_L_upLeg_430','J_L_knee_431','J_L_leg_432','J_L_foot_433','J_L_toe_434']
model=SOURCE/'Model/MS/BasePose/basepose_0000.gltf'
nodes=json.loads(model.read_text())['nodes']; ni={n['name']:i for i,n in enumerate(nodes)}
parent={c:i for i,n in enumerate(nodes) for c in n.get('children',[])}
parents={n:(nodes[parent[ni[n]]]['name'] if n!='root' else None) for n in NAMES}
assert all(p is None or p in NAMES for p in parents.values())

def fk(pos,rot):
    result={}
    def visit(n):
        if n in result:return result[n]
        q,v=rot[n],pos[n]
        if parents[n]:pq,pv=visit(parents[n]);q,v=pq*q,pv+pq.apply(v)
        result[n]=(q,v);return q,v
    for n in NAMES:visit(n)
    return np.array([np.r_[result[n][1],result[n][0].as_quat()] for n in NAMES],dtype='<f4')

def psa(path):
    with path.open('rb') as f:
        mm=mmap.mmap(f.fileno(),0,access=mmap.ACCESS_READ);chunks={};off=0
        while off<len(mm):
            name,_,size,count=struct.unpack_from('<20s3i',mm,off);name=name.split(b'\0')[0].decode()
            assert off+32+size*count<=len(mm)
            chunks[name]=(off+32,size,count);off+=32+size*count
        start,size,count=chunks['BONENAMES']; records={}
        for i in range(count):
            d=struct.unpack_from('<64s3i4f3ff3f',mm,start+i*size);n=d[0].split(b'\0')[0].decode(errors='replace')
            if n in NAMES:
                assert n not in records, 'Ambiguous core joint '+n
                records[n]=(i,d)
        assert len(records)==len(NAMES)
        start,size,number=chunks['ANIMINFO'];assert number==1
        a=struct.unpack_from('<64s64s4i3f3i',mm,start);frames=a[11];rate=a[8];assert a[2]==count
        def cvp(v):return np.array([-v[0],v[2],v[1]])*.01
        def cvq(q):return R.from_quat([-q[0],q[2],q[1],q[3]])
        bind=fk({n:cvp(d[8:11]) for n,(_,d) in records.items()}, {n:cvq(d[4:8]) for n,(_,d) in records.items()})
        start,size,keycount=chunks['ANIMKEYS'];assert size==32 and keycount==count*frames
        poses=[]
        for frame in range(frames):
            data={n:struct.unpack_from('<8f',mm,start+(frame*count+i)*size) for n,(i,_) in records.items()}
            # UModel PSA keys use ActorX animation handedness, while BONENAMES
            # carries the reference mesh convention. Root has an extra conjugation.
            poses.append(fk({n:cvp([v[0],-v[1],v[2]]) for n,v in data.items()},
                {n:cvq([-v[3],v[4],-v[5],v[6]] if n=='root' else [v[3],-v[4],v[5],v[6]]) for n,v in data.items()}))
        mm.close()
        poses=np.array(poses);assert np.isfinite(poses).all()
        # Gameplay owns planar travel/collision. Keep authored body sway and lift,
        # remove only the source actor's planar root translation.
        poses[:,:,:3]-=poses[:,0:1,:3]*np.array([1,0,1],dtype=np.float32)
    return dict(name=path.stem,rate=rate,frames=frames,poses=poses,bind=bind,source=path)

motion=SOURCE/'Motion/MS'; specs={}
for weapon,short in [('Saber','sbr'),('Rifle','rfl')]:
    for state,suffix in [('idle','wait_l'),('run','run_l'),('backrun','backRun_l'),('boost','fly_l')]:
        specs[f'{short}_{state}']=motion/f'Basic/Type_{weapon}/Standard/bsc_{short}_stn_{suffix}.psa'
for stage in range(1,4):
    for part in ['s','e']:specs[f'cut{stage}_{part}']=motion/f'Attack/Close/Saber/sbr_H0{stage}_{part}.psa'
clips={k:psa(p) for k,p in specs.items()}
bind=clips['sbr_idle']['bind']
gp={};gq={}
for n in NAMES:
    d=nodes[ni[n]];v=d.get('translation',[0,0,0]);q=d.get('rotation',[0,0,0,1]);gp[n]=np.array([-v[0],v[1],v[2]]);gq[n]=R.from_quat([q[0],-q[1],-q[2],q[3]])
gltfbind=fk(gp,gq)
positionError=float(np.max(np.abs(bind[:,:3]-gltfbind[:,:3])))
angleError=float(np.max((R.from_quat(bind[:,3:]).inv()*R.from_quat(gltfbind[:,3:])).magnitude()))
assert positionError<.0001 and angleError<.0001,(positionError,angleError)
with (OUT/'body.bytes').open('wb') as out:
    def string(s):b=s.encode();out.write(struct.pack('<i',len(b)));out.write(b)
    out.write(struct.pack('<4siii',b'GBM1',1,len(NAMES),len(clips)))
    for n in NAMES:string(n)
    out.write(bind.tobytes())
    for key,c in clips.items():
        string(key);out.write(struct.pack('<if',c['frames'],c['rate']));out.write(c['poses'].tobytes())
report={'jointNames':NAMES,'hierarchy':str(model),'referencePositionError':positionError,'referenceAngleErrorRadians':angleError,'clips':[]}
for key,c in clips.items():
    rec={'id':key,'source':str(c['source']),'sha256':hashlib.sha256(c['source'].read_bytes()).hexdigest(),'frames':c['frames'],'fps':c['rate'],'duration':(c['frames']-1)/c['rate']}
    report['clips'].append(rec);print(key,c['frames'],round(rec['duration'],3),flush=True)
report['outputSha256']=hashlib.sha256((OUT/'body.bytes').read_bytes()).hexdigest()
(REPORT/'import.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('Validated bind FK; compact motion bytes:',(OUT/'body.bytes').stat().st_size)
