import bpy,pathlib,sys
P=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(P/'RAIKEN_MkII_MASTER.blend'))
sc=bpy.context.scene
root=bpy.data.objects['RAIKEN_MkII_Grip_Root']
shots={
 'SIDE_ON':('SIDE',True,(2400,800),'01_SIDE_BEAM_ON.png'),
 'SIDE_OFF':('SIDE',False,(2400,800),'02_SIDE_BEAM_OFF.png'),
 'THREE_QUARTER':('THREE_QUARTER',True,(2400,1000),'03_THREE_QUARTER.png'),
 'HILT':('HILT',True,(1600,1300),'04_HILT_DETAIL.png'),
 'TOP':('TOP',False,(2400,600),'05_TOP.png'),
 'CROSS_SECTION':('CROSS_SECTION',True,(1200,1200),'06_CROSS_SECTION.png'),
}
names=next((a.split('=')[1].split(',') for a in sys.argv if a.startswith('shots=')),list(shots))
for name in names:
    camera,on,res,file=shots[name]
    root['Beam_On']=on;root.update_tag();bpy.context.view_layer.update()
    sc.camera=bpy.data.objects['RAIKEN_CAM_'+camera]
    sc.render.resolution_x,sc.render.resolution_y=res
    sc.render.filepath=str(P/'renders'/file)
    bpy.ops.render.render(write_still=True)
    print('SHOT_DONE',name,flush=True)
