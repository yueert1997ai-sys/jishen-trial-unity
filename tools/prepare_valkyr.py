"""Freeze the delivered mech FBX and extract its own GLB materials for Unity."""
import hashlib, json, pathlib, shutil, struct
ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCE = ROOT.parents[1] / 'art_prototypes/JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/exports'
DEST = ROOT / 'Assets/Art/ValkyrRaiken_V3'

def main():
    DEST.mkdir(parents=True, exist_ok=True)
    textures = DEST / 'Textures'
    textures.mkdir(exist_ok=True)
    records = []
    for name in ['VALKYR_RAIKEN_GAME.fbx', 'RAIKEN_MkII_GAME.fbx']:
        path = SOURCE / name
        shutil.copy2(path, DEST / name)
        records.append({'source': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    blob = (SOURCE / 'VALKYR_RAIKEN_GAME.glb').read_bytes()
    length = struct.unpack_from('<I', blob, 12)[0]
    doc = json.loads(blob[20:20+length])
    binary = blob[28+length:]
    images = []
    for index, image in enumerate(doc.get('images', [])):
        view = doc['bufferViews'][image['bufferView']]
        name = image.get('name', 'image_' + str(index)) + ('.png' if image['mimeType'] == 'image/png' else '.jpg')
        offset = view.get('byteOffset', 0)
        (textures / name).write_bytes(binary[offset:offset + view['byteLength']])
        images.append('Assets/Art/ValkyrRaiken_V3/Textures/' + name)
    def texture(info):
        return images[doc['textures'][info['index']]['source']] if info else ''
    mats = []
    for mat in doc['materials']:
        pbr = mat.get('pbrMetallicRoughness', {})
        mats.append({'name': mat['name'], 'baseColor': pbr.get('baseColorFactor', [1,1,1,1]),
            'metallic': pbr.get('metallicFactor', 0), 'roughness': pbr.get('roughnessFactor', .5),
            'albedo': texture(pbr.get('baseColorTexture')), 'normal': texture(mat.get('normalTexture')),
            'emission': mat.get('emissiveFactor', [0,0,0]),
            'emissionStrength': mat.get('extensions', {}).get('KHR_materials_emissive_strength', {}).get('emissiveStrength', 1)})
    (DEST / 'materials.json').write_text(json.dumps({'materials': mats}, indent=2), encoding='utf-8')
    evidence = ROOT / 'AuditEvidence/v3-model-update'
    evidence.mkdir(parents=True, exist_ok=True)
    (evidence / 'source-snapshot.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    print('Frozen FBX files:', len(records), 'materials:',len(mats), 'textures:',len(images))

if __name__ == '__main__': main()
