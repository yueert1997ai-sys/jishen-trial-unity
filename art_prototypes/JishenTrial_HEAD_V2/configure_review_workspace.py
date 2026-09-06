"""Finalize only viewport/document metadata in the already open HEAD_V2.blend."""
import bpy,pathlib
R=pathlib.Path('C:/Users/yue/Documents/MECH ROUGE/art_prototypes/JishenTrial_HEAD_V2')
assert pathlib.Path(bpy.data.filepath).resolve()==(R/'HEAD_V2.blend').resolve()
t=bpy.data.texts.get('README.md') or bpy.data.texts.new('README.md')
t.clear();t.write((R/'README.md').read_text(encoding='utf8'))
area=bpy.context.area
if area.type=='CONSOLE':area.type='VIEW_3D'
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        s=a.spaces.active;s.clip_start=.02;s.clip_end=25
        s.overlay.show_floor=False;s.overlay.show_axis_x=False;s.overlay.show_axis_y=False;s.overlay.show_extras=False
        s.shading.show_cavity=False
        s.shading.type='SOLID';s.shading.color_type='MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'HEAD_V2.blend'))
(R/'logs'/'gui_review_opened.txt').write_text('HEAD_V2.blend opened in Blender GUI through Computer Use.\nReference shown in Image Editor.\nViewport depth range set to 0.02-25 m to avoid depth precision artifacts.\nOnly viewport and README text updated; model geometry/material nodes/cameras unchanged.\n',encoding='utf8')
