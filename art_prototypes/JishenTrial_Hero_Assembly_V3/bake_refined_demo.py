"""Reuse the existing actual Cycles normal/PBR bakes. No synthetic texture substitutes."""
import bpy,pathlib
P=pathlib.Path(__file__).resolve().parent;W=P/'refinement';SRC=P.parent/'JishenTrial_V2_HighDetail'/'bake_demo.py'
bpy.ops.wm.open_mainfile(filepath=str(W/'DEMO_BAKE_WORKING.blend'))
code=SRC.read_text(encoding='utf8')
code=code.replace("OUT=pathlib.Path(__file__).resolve().parent;TEX=OUT/'textures'","OUT=WORK;TEX=OUT/'textures'")
code=code.replace("'JT_'+fam+'_'+channel,2048,2048","'JT_'+fam+'_'+channel,4096 if fam=='Armor' else 2048,4096 if fam=='Armor' else 2048")
code=code.replace("str(OUT/f)","str(SOURCE_DIR/f)")
code=code.replace('V2_HIGH_DETAIL_FINAL.blend','PHASE_07.blend').replace('V2_GAME_READY_DEMO.blend','JishenTrial_REFINED_DEMO.blend')
code=code.replace('JishenTrial_Hero_LOD0','JishenTrial_Refined_LOD0')
code=code.replace("'crown_height_m':3.25","'crown_height_m':3.309")
code=code.replace("'atlas_size':2048","'atlas_size':{'Armor':4096,'Mechanics':2048,'Weapon':2048}")
code=code.replace('3 x 2K PBR atlases','4K Armor + 2K Mechanics/Weapon PBR atlases')
exec(compile(code,str(SRC),'exec'),{'__name__':'__main__','__file__':str(SRC),'WORK':W,'SOURCE_DIR':SRC.parent})
