"""Executed in Blender's own Python console via Computer Use; no shell commands."""
import bpy,pathlib
ROOT=pathlib.Path('C:/Users/yue/Documents/MECH ROUGE/art_prototypes/JishenTrial_HEAD_V2')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'HEAD_V2.blend'))
t=bpy.data.texts.get('README.md') or bpy.data.texts.new('README.md')
t.clear();t.write((ROOT/'README.md').read_text(encoding='utf8'))
for a in bpy.context.screen.areas:
    if a.type=='IMAGE_EDITOR':
        a.spaces.active.image=bpy.data.images.get('REFERENCE_HEAD_DETAIL.png')
        region=next((r for r in a.regions if r.type=='WINDOW'),None)
        if region:
            with bpy.context.temp_override(area=a,region=region):
                bpy.ops.image.view_all(fit_view=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'HEAD_V2.blend'))
print('HEAD_V2_REVIEW_OPEN_IN_LOCAL_BLENDER - primary forms only')
