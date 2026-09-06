from pathlib import Path
p=Path('D:/project-mecha-design/MECH ROUGE/art_prototypes/Type_E01_20260906/build_stage03.py')
s=p.read_text(encoding='utf-8-sig')
changes={
"support=Vector((.14,0,-.093))":"support=Vector((.23,0,-.093))",
"grip=Vector((.08,-.54,2.18))":"grip=Vector((.08,-.51,2.18))",
"(6,-8,4.0),(0,-.24,1.43),3.45":"(.5,-8,3.6),(.05,-.22,1.42),3.48",
"(6,2.6,3.5),(0,-.18,1.42),3.60":"(6,2.6,3.5),(.20,-.35,1.42),3.95",
"(4.5,-6,3.8),(.03,-.33,2.28),1.52":"(.5,-6,3.3),(.12,-.44,2.29),1.85",
}
for old,new in changes.items():
    assert old in s,old
    s=s.replace(old,new,1)
p.write_text(s,encoding='utf8')
