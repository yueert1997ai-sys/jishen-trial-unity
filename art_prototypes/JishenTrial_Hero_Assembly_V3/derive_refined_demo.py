"""Reuse the current project's established LOD/UV workflow on the refined master."""
import bpy,pathlib
P=pathlib.Path(__file__).resolve().parent;W=P/'refinement';SRC=P.parent/'JishenTrial_V2_HighDetail'/'build_demo_geometry.py'
bpy.ops.wm.open_mainfile(filepath=str(W/'PHASE_07.blend'))
code=SRC.read_text(encoding='utf8')
code=code.replace("OUT=pathlib.Path(__file__).resolve().parent;scene=bpy.context.scene;T=time.time()","OUT=WORK;scene=bpy.context.scene;T=time.time()")
code=code.replace("str(OUT/'build_demo_geometry.py')","str(SOURCE_SCRIPT)")
# Drop only the documented tiny nonmanifold triangular flaps, prior to UV/bake.
needle="    bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()"
fix="""    flaps=[f for f in bm.faces if len(f.verts)==3 and sum(e.is_boundary for e in f.edges)==2 and any(len(e.link_faces)>2 for e in f.edges) and f.calc_area()<1e-7]
    if flaps:bmesh.ops.delete(bm,geom=flaps,context='FACES')
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()"""
code=code.replace(needle,fix)
exec(compile(code,str(SRC),'exec'),{'__name__':'__main__','__file__':str(SRC),'WORK':W,'SOURCE_SCRIPT':SRC})
