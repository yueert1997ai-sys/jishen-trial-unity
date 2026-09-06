"""Prepare only the isolated review project. Does not touch the game's Assets directory."""
from pathlib import Path
from PIL import Image,ImageOps
import shutil,json
OUT=Path(__file__).resolve().parent
dest=OUT/'UnityImportReview/Assets/Art/JishenTrial_Assembly_V3'
shutil.copy2(OUT/'exports/JishenTrial_Assembly_V3.fbx',dest/'JishenTrial_Assembly_V3.fbx')
for family in ('Armor','Mechanics','Weapon'):
    prefix='JT_V3_' if family=='Armor' else 'JT_'
    for ch in ('BaseColor','Normal'):
        shutil.copy2(OUT/'textures'/f'{prefix}{family}_{ch}.png',dest/'Textures'/f'{family}_{ch}.png')
    metal=Image.open(OUT/'textures'/f'{prefix}{family}_Metallic.png').convert('RGB').getchannel('R')
    rough=Image.open(OUT/'textures'/f'{prefix}{family}_Roughness.png').convert('RGB').getchannel('R')
    zero=Image.new('L',metal.size,0)
    Image.merge('RGBA',(metal,zero,zero,ImageOps.invert(rough))).save(dest/'Textures'/f'{family}_MetalSmooth.png')
(dest/'IMPORT_NOTES.txt').write_text('Static assembly review. No rig, animation controller or gameplay components.\n'
 'The side form of HEAD_V2 remains rejected by the user.\n'
 '5 shared Standard materials; Armor 4096x2048, Mechanics/Weapon 2048x2048.\n'
 'MetalSmooth texture R = metallic; A = 1 - roughness.\n'
 'Prefab path is deliberately different from the game auto-load path.\n'
 'No Assets/UserContent/PlayerMech/PlayerMech.prefab is supplied.\n',encoding='utf8')
print('UNITY_REVIEW_INPUTS_READY',dest)
